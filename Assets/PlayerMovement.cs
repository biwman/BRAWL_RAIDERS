using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviourPun
{
    public static bool gameStarted = false;

    private Rigidbody2D rb;
    public float speed = 5f;
    public float depletedSpeedMultiplier = 0.7f;
    public float boosterDuration = 5f;
    public float maxSpeedThreshold = 0.9f;
    public float fullSpeedSnapThreshold = 0.92f;
    public float boosterRecoveryThreshold = 0.2f;

    public Joystick joystick;
    public Joystick shootJoystick;

    private Vector2 moveInput;
    private Vector2 shootInput;
    private Vector2 effectiveMoveInput;
    private Vector2 lastFacingDirection = Vector2.up;
    private float boosterCharge = 1f;
    private bool boosterExhausted = false;
    private float targetRotationAngle = 0f;
    private float boosterRecoveryDelayTimer = 0f;
    private AudioSource engineAudioSource;
    private Vector3 lastAudioPosition;
    const float AccelerationResponsiveness = 18f;
    const float LowSpeedBrakeResponsiveness = 7.4f;
    const float HighSpeedBrakeResponsiveness = 1.15f;

    public float BoosterNormalized => boosterCharge;
    public bool IsBoosterDepleted => boosterExhausted;
    float CurrentDepletedSpeedMultiplier => 1f - (RoomSettings.GetBoosterSlowdownPercent() / 100f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.angularVelocity = 0f;
        }

        if (GetComponent<HideInNebulaTarget>() == null)
        {
            gameObject.AddComponent<HideInNebulaTarget>();
        }

        if (GetComponent<EngineThrusterVFX>() == null)
        {
            gameObject.AddComponent<EngineThrusterVFX>();
        }

        targetRotationAngle = transform.eulerAngles.z;

        SetupEngineAudio();
        lastAudioPosition = transform.position;

        if (GetComponent<EnemyBot>() != null)
            return;

        if (!photonView.IsMine)
            return;

        CameraFollow cam = FindObjectOfType<CameraFollow>();
        if (cam != null)
        {
            cam.target = transform;
        }

        ResolveJoysticks();

        if (GetComponent<BoosterBarUI>() == null)
        {
            gameObject.AddComponent<BoosterBarUI>();
        }

        if (GetComponent<ShipInventoryHudUI>() == null)
        {
            gameObject.AddComponent<ShipInventoryHudUI>();
        }
    }

    void Update()
    {
        if (GetComponent<EnemyBot>() != null)
        {
            UpdateEngineAudio();
            return;
        }

        if (photonView.IsMine)
        {
            if (!IsGameStarted())
            {
                moveInput = Vector2.zero;
                shootInput = Vector2.zero;
                effectiveMoveInput = Vector2.zero;
                if (rb != null)
                {
                    rb.angularVelocity = 0f;
                }
                UpdateEngineAudio();
                return;
            }

            ResolveJoysticks();

            moveInput = joystick != null && joystick.IsPressed ? joystick.inputVector : Vector2.zero;
            shootInput = shootJoystick != null && shootJoystick.IsPressed ? shootJoystick.inputVector : Vector2.zero;

            if (moveInput.magnitude < 0.2f)
                moveInput = Vector2.zero;

            if (shootInput.magnitude < 0.3f)
                shootInput = Vector2.zero;

            effectiveMoveInput = GetEffectiveMoveInput(moveInput);

            UpdateBooster(Time.deltaTime);
            UpdateFacingDirection();
        }

        UpdateEngineAudio();
    }

    void FixedUpdate()
    {
        if (GetComponent<EnemyBot>() != null)
            return;

        if (!photonView.IsMine)
            return;

        if (!IsGameStarted())
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            return;
        }

        float currentSpeed = IsBoosterDepleted ? speed * CurrentDepletedSpeedMultiplier : speed;
        if (rb != null)
        {
            ApplyVelocity(currentSpeed);
            ClampExcessCollisionBoost(currentSpeed);
            rb.angularVelocity = 0f;
            rb.MoveRotation(targetRotationAngle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!photonView.IsMine)
            return;

        Debug.Log("DOTKNALEM: " + other.name);
    }

    void ClampExcessCollisionBoost(float currentSpeed)
    {
        if (rb == null)
            return;

        float expectedTopSpeed = currentSpeed * Mathf.Max(1f, GetMaxInputSpeedBoostMultiplier());
        float hardCap = expectedTopSpeed * 1.55f;
        float currentMagnitude = rb.linearVelocity.magnitude;

        if (currentMagnitude <= hardCap || currentMagnitude <= 0.001f)
            return;

        rb.linearVelocity = rb.linearVelocity.normalized * hardCap;
    }

    bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value))
        {
            return (bool)value;
        }

        return false;
    }

    void UpdateBooster(float deltaTime)
    {
        bool usingBooster = effectiveMoveInput.magnitude >= maxSpeedThreshold && !boosterExhausted;

        if (usingBooster)
        {
            boosterCharge -= deltaTime / boosterDuration;
            boosterRecoveryDelayTimer = RoomSettings.GetBoosterRecoveryDelay();
        }
        else if (!boosterExhausted || boosterRecoveryDelayTimer <= 0f)
        {
            boosterCharge += deltaTime / boosterDuration;
        }
        else
        {
            boosterRecoveryDelayTimer -= deltaTime;
        }

        boosterCharge = Mathf.Clamp01(boosterCharge);

        if (!boosterExhausted && boosterCharge <= 0.001f)
        {
            boosterExhausted = true;
            boosterRecoveryDelayTimer = RoomSettings.GetBoosterRecoveryDelay();
        }
        else if (boosterExhausted && boosterCharge >= boosterRecoveryThreshold)
        {
            boosterExhausted = false;
        }
    }

    Vector2 GetEffectiveMoveInput(Vector2 rawInput)
    {
        if (rawInput == Vector2.zero)
            return Vector2.zero;

        if (rawInput.magnitude >= fullSpeedSnapThreshold)
            return rawInput.normalized * GetMaxInputSpeedBoostMultiplier();

        return rawInput;
    }

    float GetMaxInputSpeedBoostMultiplier()
    {
        return 1f + (RoomSettings.GetMaxInputBoostPercent() / 100f);
    }

    void ApplyVelocity(float currentSpeed)
    {
        Vector2 targetVelocity = effectiveMoveInput * currentSpeed;

        if (!RoomSettings.IsShipDriftEnabled())
        {
            rb.linearVelocity = targetVelocity;
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;
        float currentMagnitude = currentVelocity.magnitude;
        float targetMagnitude = targetVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentMagnitude / Mathf.Max(currentSpeed, 0.001f));

        bool braking = targetMagnitude + 0.01f < currentMagnitude || effectiveMoveInput == Vector2.zero;

        if (braking)
        {
            float driftWeight = speedRatio * speedRatio;
            float brakeResponsiveness = Mathf.Lerp(LowSpeedBrakeResponsiveness, HighSpeedBrakeResponsiveness, driftWeight);
            float releaseDriftMultiplier = effectiveMoveInput == Vector2.zero ? 0.86f : 1f;
            float maxDelta = brakeResponsiveness * speed * releaseDriftMultiplier * Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, maxDelta);
            return;
        }

        float accelerationDelta = AccelerationResponsiveness * speed * Time.fixedDeltaTime;
        rb.linearVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelerationDelta);
    }

    void UpdateFacingDirection()
    {
        Vector2 desiredDirection = Vector2.zero;

        if (shootInput.sqrMagnitude > 0.09f)
        {
            desiredDirection = shootInput.normalized;
        }
        else if (moveInput.sqrMagnitude > 0.09f)
        {
            desiredDirection = moveInput.normalized;
        }

        if (desiredDirection == Vector2.zero)
            return;

        lastFacingDirection = desiredDirection;

        float angle = Mathf.Atan2(lastFacingDirection.y, lastFacingDirection.x) * Mathf.Rad2Deg;
        targetRotationAngle = angle - 90f;

        if (rb == null)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, targetRotationAngle);
        }
    }

    void ResolveJoysticks()
    {
        if (joystick == null)
        {
            GameObject movementJoystick = GameObject.Find("JoystickBG");
            if (movementJoystick != null)
            {
                joystick = movementJoystick.GetComponent<Joystick>();
            }
        }

        if (shootJoystick == null)
        {
            GameObject shootingJoystick = GameObject.Find("ShootJoystickBG");
            if (shootingJoystick != null)
            {
                shootJoystick = shootingJoystick.GetComponent<Joystick>();
            }
        }
    }

    void SetupEngineAudio()
    {
        AudioClip engineClip = AudioManager.Instance.EngineClip;
        if (engineClip == null)
            return;

        engineAudioSource = GetComponent<AudioSource>();
        if (engineAudioSource == null)
        {
            engineAudioSource = gameObject.AddComponent<AudioSource>();
        }

        engineAudioSource.clip = engineClip;
        engineAudioSource.loop = true;
        engineAudioSource.playOnAwake = false;
        AudioManager.Instance.ConfigureSpatialSource(engineAudioSource, 0f);
        engineAudioSource.volume = 0f;
        engineAudioSource.pitch = 0.85f;
    }

    void UpdateEngineAudio()
    {
        if (engineAudioSource == null)
            return;

        if (!IsGameStarted())
        {
            if (engineAudioSource.isPlaying)
                engineAudioSource.Stop();

            return;
        }

        float speedReference = photonView.IsMine && IsBoosterDepleted ? speed * CurrentDepletedSpeedMultiplier : speed;
        if (speedReference <= 0.001f)
            speedReference = speed;

        float normalizedSpeed = GetAudioSpeedRatio(speedReference);

        if (!engineAudioSource.isPlaying)
            engineAudioSource.Play();

        engineAudioSource.volume = Mathf.Lerp(0.12f, 0.42f, normalizedSpeed);
        engineAudioSource.pitch = Mathf.Lerp(0.88f, 1.24f, normalizedSpeed);
    }

    float GetAudioSpeedRatio(float speedReference)
    {
        if (photonView.IsMine)
        {
            if (rb != null && speedReference > 0.001f)
            {
                return Mathf.Clamp01(rb.linearVelocity.magnitude / speedReference);
            }

            return 0f;
        }

        float delta = Time.unscaledDeltaTime > 0.0001f
            ? Vector3.Distance(transform.position, lastAudioPosition) / Time.unscaledDeltaTime
            : 0f;
        lastAudioPosition = transform.position;
        return Mathf.Clamp01(delta / speedReference);
    }
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EngineThrusterVFX : MonoBehaviour
{
    const string ThrusterRootName = "EngineVFX";
    const string TrailObjectName = "EngineTrail";

    Rigidbody2D rb;
    SpriteRenderer shipRenderer;
    TrailRenderer trailRenderer;
    float referenceSpeed = 5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shipRenderer = GetComponent<SpriteRenderer>();
        PlayerMovement movement = GetComponent<PlayerMovement>();
        referenceSpeed = Mathf.Max(1f, movement != null ? movement.speed : 5f);

        CreateThrusterObjects();
        UpdateVisuals(0f);
    }

    void Update()
    {
        if (rb == null)
            return;

        float speedNormalized = Mathf.InverseLerp(0.02f, referenceSpeed, rb.linearVelocity.magnitude);
        UpdateVisuals(speedNormalized);
    }

    void CreateThrusterObjects()
    {
        Transform existing = transform.Find(ThrusterRootName);
        GameObject rootObject = existing != null ? existing.gameObject : new GameObject(ThrusterRootName);
        rootObject.transform.SetParent(transform, false);

        for (int i = rootObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(rootObject.transform.GetChild(i).gameObject);
        }

        float shipHeight = shipRenderer != null ? shipRenderer.bounds.size.y : 1f;
        rootObject.transform.localPosition = new Vector3(0f, -shipHeight * 0.46f, 0f);
        rootObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        GameObject trailObject = new GameObject(TrailObjectName);
        trailObject.transform.SetParent(rootObject.transform, false);
        trailObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        trailRenderer = trailObject.AddComponent<TrailRenderer>();
        ConfigureTrail(trailRenderer);
    }

    void ConfigureTrail(TrailRenderer trail)
    {
        trail.time = 0.42f;
        trail.minVertexDistance = 0.01f;
        trail.widthMultiplier = 0.08f;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.numCapVertices = 12;
        trail.numCornerVertices = 8;
        trail.material = CreateSpritesMaterial();
        trail.generateLightingData = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.64f, 0.97f, 1f), 0.14f),
                new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0.45f),
                new GradientColorKey(new Color(0.03f, 0.18f, 0.86f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.66f, 0.2f),
                new GradientAlphaKey(0.26f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.12f, 0.82f),
            new Keyframe(0.6f, 0.3f),
            new Keyframe(1f, 0f));

        if (shipRenderer != null)
        {
            trail.sortingLayerID = shipRenderer.sortingLayerID;
            trail.sortingOrder = shipRenderer.sortingOrder - 2;
        }
    }

    void UpdateVisuals(float speedNormalized)
    {
        float clamped = Mathf.Clamp01(speedNormalized);
        float intensity = Mathf.Lerp(0.18f, 1f, clamped);

        if (trailRenderer != null)
        {
            trailRenderer.time = Mathf.Lerp(0.22f, 0.82f, intensity);
            trailRenderer.widthMultiplier = Mathf.Lerp(0.03f, 0.16f, intensity);
            trailRenderer.emitting = clamped > 0.04f;
        }
    }

    Material CreateSpritesMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.name = "EngineThrusterVFXMaterial";
        return material;
    }
}
