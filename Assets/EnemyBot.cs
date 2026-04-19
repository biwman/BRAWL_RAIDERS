using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBot : MonoBehaviourPun
{
    public const string BotInstantiationMarker = "enemy_bot";
    const float DroidTargetSize = 1.04f;

    static Sprite cachedBotSprite;

    Rigidbody2D rb;
    PhotonView view;
    PlayerHealth health;
    EnemyBotBehaviorBase behavior;
    SpriteRenderer cachedRenderer;

    public static bool IsBotObject(GameObject target)
    {
        return target != null && target.GetComponent<EnemyBot>() != null;
    }

    public static bool IsBotView(PhotonView targetView)
    {
        return targetView != null && targetView.GetComponent<EnemyBot>() != null;
    }

    public static bool IsBotInstantiationData(object[] data)
    {
        return data != null &&
               data.Length > 0 &&
               data[0] is string marker &&
               marker == BotInstantiationMarker;
    }

    public void InitializeFromPhotonData()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();

        DisablePlayerOnlySystems();
        EnsureBehavior();
        ApplyBotVisuals();
        ConfigurePhysics();
    }

    void Awake()
    {
        InitializeFromPhotonData();
    }

    void Start()
    {
        InitializeFromPhotonData();
    }

    void Update()
    {
        EnsureStableVisuals();

        if (!view.IsMine || !IsGameStarted() || health == null || health.IsWreck)
            return;

        if (behavior == null)
            EnsureBehavior();
    }

    void FixedUpdate()
    {
        if (!view.IsMine || !IsGameStarted() || health == null || health.IsWreck)
            return;

        if (behavior == null)
            EnsureBehavior();

        behavior?.TickBehavior();
    }

    void ConfigurePhysics()
    {
        if (rb == null)
            return;

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void EnsureBehavior()
    {
        behavior = GetComponent<EnemyBotBehaviorBase>();
        if (behavior == null)
            behavior = gameObject.AddComponent<EnemyDroneBehavior>();

        behavior.Initialize(this);
    }

    void DisablePlayerOnlySystems()
    {
        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            collector.enabled = false;

        ShipInventoryHudUI cargoHud = GetComponent<ShipInventoryHudUI>();
        if (cargoHud != null)
            cargoHud.enabled = false;

        AmmoUI ammoUi = GetComponent<AmmoUI>();
        if (ammoUi != null)
            ammoUi.enabled = false;

        BoosterBarUI boosterUi = GetComponent<BoosterBarUI>();
        if (boosterUi != null)
            boosterUi.enabled = false;

        ReloadButtonUI reloadUi = GetComponent<ReloadButtonUI>();
        if (reloadUi != null)
            reloadUi.enabled = false;
    }

    void ApplyBotVisuals()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        cachedRenderer = renderer;
        Sprite sprite = LoadBotSprite();
        if (sprite == null)
            return;

        renderer.sprite = sprite;
        renderer.color = Color.white;
        FitRendererToTargetSize(renderer, DroidTargetSize);
    }

    void EnsureStableVisuals()
    {
        if (health != null && health.IsWreck)
            return;

        if (cachedRenderer == null)
            cachedRenderer = GetComponent<SpriteRenderer>();

        if (cachedRenderer == null)
            return;

        Sprite desiredSprite = LoadBotSprite();
        if (desiredSprite != null && cachedRenderer.sprite != desiredSprite)
            cachedRenderer.sprite = desiredSprite;

        cachedRenderer.color = Color.white;
        FitRendererToTargetSize(cachedRenderer, DroidTargetSize);
    }

    Sprite LoadBotSprite()
    {
        if (cachedBotSprite != null)
            return cachedBotSprite;

        string filePath = System.IO.Path.Combine(Application.dataPath, "droid1.png");
        if (!System.IO.File.Exists(filePath))
            return null;

        byte[] bytes = System.IO.File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        cachedBotSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedBotSprite;
    }

    bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
            value is bool started)
        {
            return started;
        }

        return false;
    }

    void FitRendererToTargetSize(SpriteRenderer renderer, float targetSize)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Bounds spriteBounds = renderer.sprite.bounds;
        float largestDimension = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);
        if (largestDimension <= 0.0001f)
            return;

        float scale = targetSize / largestDimension;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}

public abstract class EnemyBotBehaviorBase : MonoBehaviour
{
    protected EnemyBot bot;

    public virtual void Initialize(EnemyBot owner)
    {
        bot = owner;
    }

    public abstract void TickBehavior();
}

[RequireComponent(typeof(EnemyBot))]
public class EnemyDroneBehavior : EnemyBotBehaviorBase
{
    const float DetectionRadius = 10f;
    const float DisengageRadius = 20f;
    const float MoveSpeed = 1.1f;
    const float OrbitDistance = 5.5f;
    const float PreferredDistance = 7.5f;
    const float ShootDistance = 12f;
    const float RepathInterval = 0.35f;
    const float TargetRefreshInterval = 0.45f;
    const float TurnResponsiveness = 300f;
    const float IdleDriftTurnSpeed = 18f;

    Rigidbody2D rb;
    PhotonView view;
    PlayerShooting shooting;
    PlayerHealth health;
    float nextTargetRefreshTime;
    float nextRepathTime;
    Vector2 currentMoveDirection = Vector2.up;
    Transform currentTarget;

    public override void Initialize(EnemyBot owner)
    {
        base.Initialize(owner);
        rb = owner.GetComponent<Rigidbody2D>();
        view = owner.GetComponent<PhotonView>();
        shooting = owner.GetComponent<PlayerShooting>();
        health = owner.GetComponent<PlayerHealth>();

        if (shooting != null)
        {
            shooting.fireRate = 0.15f;
            shooting.reloadDuration = 10f;
        }
    }

    public override void TickBehavior()
    {
        if (bot == null || view == null || !view.IsMine || rb == null)
            return;

        if (health != null && health.IsWreck)
            return;

        if (Time.time >= nextTargetRefreshTime)
        {
            nextTargetRefreshTime = Time.time + TargetRefreshInterval;
            currentTarget = ResolveTarget();
        }

        if (currentTarget == null)
        {
            ApplyIdleDrift();
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            currentMoveDirection = CalculateMoveDirection(currentTarget.position);
        }

        Vector2 desiredVelocity = currentMoveDirection * MoveSpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, 0.14f);

        Vector2 aimDirection = (Vector2)currentTarget.position - rb.position;
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg + 90f;
            float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, TurnResponsiveness * Time.fixedDeltaTime);
            rb.MoveRotation(nextAngle);
        }

        TryShootAtTarget(aimDirection);
    }

    void ApplyIdleDrift()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.05f)
        {
            Vector2 fallback = currentMoveDirection.sqrMagnitude > 0.001f ? currentMoveDirection : Vector2.up;
            rb.linearVelocity = fallback.normalized * (MoveSpeed * 0.36f);
        }

        float spin = Mathf.Sin(Time.time * 0.45f + view.ViewID * 0.23f) * IdleDriftTurnSpeed;
        rb.MoveRotation(rb.rotation + spin * Time.fixedDeltaTime);
    }

    Transform ResolveTarget()
    {
        if (currentTarget != null)
        {
            PlayerHealth currentHealth = currentTarget.GetComponent<PlayerHealth>();
            if (IsValidVisibleTarget(currentHealth, DisengageRadius))
                return currentTarget;
        }

        return FindClosestVisibleHumanTarget(DetectionRadius);
    }

    Transform FindClosestVisibleHumanTarget(float maxDistance)
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            PlayerHealth candidate = players[i];
            if (candidate == null || candidate == health || candidate.IsWreck || candidate.IsBotControlled)
                continue;

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance > maxDistance || distance >= bestDistance)
                continue;

            if (!IsValidVisibleTarget(candidate, maxDistance))
                continue;

            bestDistance = distance;
            bestTarget = candidate.transform;
        }

        return bestTarget;
    }

    bool IsValidVisibleTarget(PlayerHealth candidate, float maxDistance)
    {
        if (candidate == null || candidate == health || candidate.IsWreck || candidate.IsBotControlled)
            return false;

        HideInNebulaTarget nebulaState = candidate.GetComponent<HideInNebulaTarget>();
        if (nebulaState != null && nebulaState.IsHiddenForOthers)
            return false;

        float distance = Vector2.Distance(transform.position, candidate.transform.position);
        return distance <= maxDistance;
    }

    Vector2 CalculateMoveDirection(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - rb.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return currentMoveDirection;

        Vector2 towardTarget = toTarget / distance;
        Vector2 orbitDirection = new Vector2(-towardTarget.y, towardTarget.x);
        if (Mathf.Sin(Time.time * 0.6f + view.ViewID * 0.27f) < 0f)
            orbitDirection *= -1f;

        Vector2 result;
        if (distance > PreferredDistance)
            result = towardTarget * 0.84f + orbitDirection * 0.28f;
        else if (distance < OrbitDistance)
            result = -towardTarget * 0.72f + orbitDirection * 0.52f;
        else
            result = orbitDirection * 0.85f + towardTarget * 0.18f;

        return result.normalized;
    }

    void TryShootAtTarget(Vector2 aimDirection)
    {
        if (shooting == null || aimDirection.sqrMagnitude <= 0.001f)
            return;

        if (aimDirection.magnitude > ShootDistance)
            return;

        Vector2 normalizedAim = aimDirection.normalized;
        float facingDot = Vector2.Dot(-transform.up, normalizedAim);
        if (facingDot < 0.9f)
            return;

        shooting.TryFireBot(normalizedAim);
    }
}
