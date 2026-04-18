using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBot : MonoBehaviourPun
{
    public const string BotInstantiationMarker = "enemy_bot";

    const float MoveSpeed = 4.3f;
    const float OrbitDistance = 5.5f;
    const float PreferredDistance = 7.5f;
    const float ShootDistance = 10.5f;
    const float RepathInterval = 0.35f;
    const float TargetRefreshInterval = 0.5f;
    const float TurnResponsiveness = 420f;

    static Sprite cachedBotSprite;

    Rigidbody2D rb;
    PhotonView view;
    PlayerShooting shooting;
    PlayerHealth health;
    float nextTargetRefreshTime;
    float nextRepathTime;
    Vector2 currentMoveDirection = Vector2.up;
    Transform currentTarget;

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
        shooting = GetComponent<PlayerShooting>();
        health = GetComponent<PlayerHealth>();

        DisablePlayerOnlySystems();
        ApplyBotVisuals();
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
        if (!view.IsMine || !IsGameStarted())
            return;

        if (health != null && health.IsWreck)
            return;

        if (Time.time >= nextTargetRefreshTime)
        {
            nextTargetRefreshTime = Time.time + TargetRefreshInterval;
            currentTarget = FindClosestHumanTarget();
        }
    }

    void FixedUpdate()
    {
        if (!view.IsMine || !IsGameStarted())
            return;

        if (health != null && health.IsWreck)
            return;

        if (rb == null)
            return;

        if (currentTarget == null)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.08f);
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            currentMoveDirection = CalculateMoveDirection(currentTarget.position);
        }

        Vector2 desiredVelocity = currentMoveDirection * MoveSpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, 0.18f);

        Vector2 aimDirection = ((Vector2)currentTarget.position - rb.position);
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
            float nextAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, TurnResponsiveness * Time.fixedDeltaTime);
            rb.MoveRotation(nextAngle);
        }

        TryShootAtTarget(aimDirection);
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

        Sprite sprite = LoadBotSprite();
        if (sprite == null)
            return;

        renderer.sprite = sprite;
        renderer.color = Color.white;
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

    Transform FindClosestHumanTarget()
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
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate.transform;
            }
        }

        return bestTarget;
    }

    Vector2 CalculateMoveDirection(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - rb.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return Vector2.zero;

        Vector2 towardTarget = toTarget / distance;
        Vector2 orbitDirection = new Vector2(-towardTarget.y, towardTarget.x);

        float orbitSeed = Mathf.Sin(Time.time * 0.7f + view.ViewID * 0.33f);
        if (orbitSeed < 0f)
            orbitDirection *= -1f;

        Vector2 result;
        if (distance > PreferredDistance)
        {
            result = (towardTarget * 0.82f) + (orbitDirection * 0.34f);
        }
        else if (distance < OrbitDistance)
        {
            result = (-towardTarget * 0.7f) + (orbitDirection * 0.55f);
        }
        else
        {
            result = (orbitDirection * 0.88f) + (towardTarget * 0.22f);
        }

        return result.normalized;
    }

    void TryShootAtTarget(Vector2 aimDirection)
    {
        if (shooting == null || aimDirection.sqrMagnitude <= 0.001f)
            return;

        if (aimDirection.magnitude > ShootDistance)
            return;

        Vector2 normalizedAim = aimDirection.normalized;
        float facingDot = Vector2.Dot(transform.up, normalizedAim);
        if (facingDot < 0.92f)
            return;

        shooting.TryFireBot(normalizedAim);
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
}
