using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovingSpaceObject : MonoBehaviour
{
    public enum SpaceObjectType
    {
        Obstacle,
        Treasure
    }

    const float ObstacleBaseSpeed = 0.22f;
    const float TreasureBaseSpeed = 0.3f;
    const float VelocityDamping = 0.12f;
    const float CruiseAcceleration = 2.1f;
    const float MaxSpeedMultiplier = 3.1f;
    const float MinCruiseSpeedFactor = 0.78f;
    const float MinAngularSpeed = 12f;
    const float MaxAngularSpeed = 38f;
    const float MaxCollisionAngularSpeed = 55f;
    const float MaxBoundaryAngularSpeed = 48f;
    const float CollisionSpinFlipThreshold = 0.85f;
    const float CollisionSpinFlipChance = 0.45f;
    const float SnapshotInterval = 0.045f;
    const float RemoteSmoothing = 16f;
    const float ImpulseRequestCooldown = 0.05f;

    static readonly Dictionary<string, MovingSpaceObject> ObjectsById = new Dictionary<string, MovingSpaceObject>();
    static PhysicsMaterial2D sharedBouncyMaterial;

    string stableId;
    SpaceObjectType objectType;
    Rigidbody2D rb;
    Vector2 cruiseDirection;
    float speedMultiplier = 1f;
    float baseAngularSpeed;
    float nextSnapshotTime;
    float nextImpulseRequestTime;
    bool isAuthority;
    bool movingEnabled;
    Vector2 networkPosition;
    Vector2 networkVelocity;
    float networkRotation;
    float networkAngularVelocity;
    bool hasNetworkState;
    bool configured;

    public string StableId => stableId;

    public static PhysicsMaterial2D GetSharedBouncyMaterial()
    {
        if (sharedBouncyMaterial == null)
        {
            sharedBouncyMaterial = new PhysicsMaterial2D("MovingSpaceObjectBouncy")
            {
                friction = 0f,
                bounciness = 1f
            };
        }

        return sharedBouncyMaterial;
    }

    public static MovingSpaceObject Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        ObjectsById.TryGetValue(id, out MovingSpaceObject value);
        return value;
    }

    public void Configure(string id, SpaceObjectType type)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!string.IsNullOrWhiteSpace(stableId))
        {
            ObjectsById.Remove(stableId);
        }

        stableId = id;
        objectType = type;
        EnsureRigidBody();
        ConfigureMotionFromId(id);
        ApplySimulationMode();

        ObjectsById[stableId] = this;
        configured = true;
    }

    void Awake()
    {
        EnsureRigidBody();
    }

    void FixedUpdate()
    {
        if (!configured)
            return;

        ApplySimulationMode();

        if (isAuthority)
        {
            SimulateAuthorityMotion();
            BroadcastSnapshotIfNeeded();
        }
        else
        {
            FollowAuthoritySnapshot();
        }
    }

    void OnDestroy()
    {
        if (!string.IsNullOrWhiteSpace(stableId))
        {
            ObjectsById.Remove(stableId);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!RoomSettings.AreMovingObjectsEnabled())
            return;

        if (isAuthority)
        {
            TryFlipSpinOnCollision(collision);
            return;
        }

        if (Time.time < nextImpulseRequestTime)
            return;

        PlayerMovement player = collision.collider.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.photonView.IsMine)
            return;

        if (player.GetComponent<AstronautSurvivor>() != null)
            return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 playerVelocity = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
        if (playerVelocity.sqrMagnitude < 0.01f)
            return;

        int weightFactor = objectType == SpaceObjectType.Obstacle
            ? RoomSettings.GetObstacleWeightFactor()
            : RoomSettings.GetTreasureWeightFactor();
        weightFactor = Mathf.Max(1, weightFactor);

        Vector2 impulse = playerVelocity / weightFactor;
        nextImpulseRequestTime = Time.time + ImpulseRequestCooldown;
        SpaceObjectMotionSync.RequestImpulse(stableId, impulse);
    }

    public void ApplyImpulse(Vector2 impulse)
    {
        if (rb == null)
            return;

        ApplySimulationMode();
        if (!isAuthority)
            return;

        EnsureDynamicBody();
        rb.linearVelocity += impulse;
        if (Mathf.Abs(impulse.x) + Mathf.Abs(impulse.y) > 0.05f)
        {
            float torqueDirection = Mathf.Sign(Vector3.Cross(cruiseDirection, impulse).z);
            if (Mathf.Abs(torqueDirection) < 0.1f)
                torqueDirection = Random.value < 0.5f ? -1f : 1f;

            float addedAngular = torqueDirection * Mathf.Lerp(4f, 12f, Mathf.Clamp01(impulse.magnitude / 3f));
            rb.angularVelocity = Mathf.Clamp(rb.angularVelocity + addedAngular, -MaxCollisionAngularSpeed, MaxCollisionAngularSpeed);
        }
    }

    public void ApplyNetworkState(Vector2 position, Vector2 velocity, float rotation, float angularVelocity)
    {
        if (isAuthority)
            return;

        networkPosition = position;
        networkVelocity = velocity;
        networkRotation = rotation;
        networkAngularVelocity = angularVelocity;
        hasNetworkState = true;

        if (rb != null && Vector2.Distance(rb.position, position) > 2f)
        {
            rb.position = position;
            rb.rotation = rotation;
        }
    }

    void EnsureRigidBody()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.useFullKinematicContacts = true;

        Collider2D[] colliders = GetComponents<Collider2D>();
        PhysicsMaterial2D material = GetSharedBouncyMaterial();
        foreach (Collider2D currentCollider in colliders)
        {
            if (currentCollider != null)
            {
                currentCollider.sharedMaterial = material;
            }
        }
    }

    void ConfigureMotionFromId(string id)
    {
        int hash = id.GetHashCode();
        float seedA = Mathf.Abs(hash * 0.00017f) + 4.7f;
        float seedB = Mathf.Abs(hash * 0.00031f) + 9.9f;
        float angle = Mathf.PerlinNoise(seedA, seedB) * Mathf.PI * 2f;
        cruiseDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        speedMultiplier = Mathf.Lerp(0.45f, 1.85f, Mathf.PerlinNoise(seedB, seedA + 3.2f));
        float angularSeed = Mathf.PerlinNoise(seedA + 7.1f, seedB + 4.6f);
        baseAngularSpeed = Mathf.Lerp(MinAngularSpeed, MaxAngularSpeed, angularSeed);
        if (Mathf.PerlinNoise(seedB + 1.8f, seedA + 5.5f) < 0.5f)
            baseAngularSpeed *= -1f;
        if (cruiseDirection.sqrMagnitude < 0.0001f)
        {
            cruiseDirection = Vector2.right;
        }
    }

    void ApplySimulationMode()
    {
        movingEnabled = RoomSettings.AreMovingObjectsEnabled();
        isAuthority = !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

        if (rb == null)
            EnsureRigidBody();

        if (isAuthority && movingEnabled)
        {
            EnsureDynamicBody();
        }
        else
        {
            if (movingEnabled)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    void EnsureDynamicBody()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = Mathf.Max(1f, GetMassFactor());
        rb.linearDamping = VelocityDamping;
        rb.angularDamping = 0.18f;
    }

    void SimulateAuthorityMotion()
    {
        if (!movingEnabled || rb == null)
            return;

        float baseSpeed = GetBaseSpeed();
        float minCruiseSpeed = baseSpeed * MinCruiseSpeedFactor;
        float maxSpeed = baseSpeed * MaxSpeedMultiplier;

        if (rb.linearVelocity.sqrMagnitude > 0.0025f)
        {
            cruiseDirection = rb.linearVelocity.normalized;
        }
        else
        {
            rb.linearVelocity = cruiseDirection * baseSpeed;
        }

        if (rb.linearVelocity.magnitude < minCruiseSpeed)
        {
            rb.AddForce(cruiseDirection * CruiseAcceleration, ForceMode2D.Force);
        }

        if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        if (Mathf.Abs(rb.angularVelocity) < Mathf.Abs(baseAngularSpeed) * 0.82f)
        {
            float angularStep = Mathf.Sign(baseAngularSpeed) * 22f * Time.fixedDeltaTime;
            rb.angularVelocity += angularStep;
        }

        KeepInsideMapBounds();
    }

    void BroadcastSnapshotIfNeeded()
    {
        if (!PhotonNetwork.IsConnected || Time.time < nextSnapshotTime || rb == null)
            return;

        nextSnapshotTime = Time.time + SnapshotInterval;
        SpaceObjectMotionSync.BroadcastState(stableId, rb.position, rb.linearVelocity, rb.rotation, rb.angularVelocity);
    }

    void FollowAuthoritySnapshot()
    {
        if (!hasNetworkState || rb == null)
            return;

        Vector2 predictedPosition = networkPosition + networkVelocity * SnapshotInterval;
        float smoothing = 1f - Mathf.Exp(-RemoteSmoothing * Time.fixedDeltaTime);

        if (Vector2.Distance(rb.position, predictedPosition) > 1.6f)
        {
            rb.position = predictedPosition;
            rb.rotation = networkRotation;
        }

        Vector2 nextPosition = Vector2.Lerp(rb.position, predictedPosition, smoothing);
        float nextRotation = Mathf.LerpAngle(rb.rotation, networkRotation, smoothing);
        rb.MovePosition(nextPosition);
        rb.MoveRotation(nextRotation);
    }

    void TryFlipSpinOnCollision(Collision2D collision)
    {
        if (rb == null || collision == null)
            return;

        float relativeSpeed = collision.relativeVelocity.magnitude;
        if (relativeSpeed < CollisionSpinFlipThreshold || Random.value > CollisionSpinFlipChance)
            return;

        float currentAngular = Mathf.Abs(rb.angularVelocity) > 0.1f ? rb.angularVelocity : baseAngularSpeed;
        float flippedAngular = -currentAngular * Mathf.Lerp(0.7f, 0.95f, Mathf.Clamp01(relativeSpeed / 4f));
        flippedAngular = Mathf.Clamp(flippedAngular, -MaxCollisionAngularSpeed, MaxCollisionAngularSpeed);
        rb.angularVelocity = flippedAngular;
        baseAngularSpeed = Mathf.Sign(flippedAngular) * Mathf.Min(Mathf.Abs(baseAngularSpeed), MaxAngularSpeed);
    }

    float GetBaseSpeed()
    {
        float baseSpeed = objectType == SpaceObjectType.Obstacle ? ObstacleBaseSpeed : TreasureBaseSpeed;
        return baseSpeed * speedMultiplier;
    }

    float GetMassFactor()
    {
        return objectType == SpaceObjectType.Obstacle
            ? RoomSettings.GetObstacleWeightFactor()
            : RoomSettings.GetTreasureWeightFactor();
    }

    void KeepInsideMapBounds()
    {
        if (rb == null)
            return;

        Vector2 mapSize = RoomSettings.GetMapDimensions();
        float halfX = mapSize.x * 0.5f;
        float halfY = mapSize.y * 0.5f;
        float boundsRadius = GetBoundsRadius();

        Vector2 position = rb.position;
        Vector2 velocity = rb.linearVelocity;
        bool reflected = false;

        if (position.x > halfX - boundsRadius)
        {
            position.x = halfX - boundsRadius;
            velocity.x = -Mathf.Abs(velocity.x);
            reflected = true;
        }
        else if (position.x < -halfX + boundsRadius)
        {
            position.x = -halfX + boundsRadius;
            velocity.x = Mathf.Abs(velocity.x);
            reflected = true;
        }

        if (position.y > halfY - boundsRadius)
        {
            position.y = halfY - boundsRadius;
            velocity.y = -Mathf.Abs(velocity.y);
            reflected = true;
        }
        else if (position.y < -halfY + boundsRadius)
        {
            position.y = -halfY + boundsRadius;
            velocity.y = Mathf.Abs(velocity.y);
            reflected = true;
        }

        if (!reflected)
            return;

        rb.position = position;
        rb.linearVelocity = velocity;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            cruiseDirection = velocity.normalized;
        }

        if (Random.value < CollisionSpinFlipChance)
        {
            rb.angularVelocity = Mathf.Clamp(-rb.angularVelocity * 0.6f, -MaxBoundaryAngularSpeed, MaxBoundaryAngularSpeed);
            baseAngularSpeed *= -1f;
        }
    }

    float GetBoundsRadius()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        float maxExtent = 0.5f;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D current = colliders[i];
            if (current == null || !current.enabled)
                continue;

            Bounds bounds = current.bounds;
            maxExtent = Mathf.Max(maxExtent, bounds.extents.x, bounds.extents.y);
        }

        return maxExtent;
    }
}
