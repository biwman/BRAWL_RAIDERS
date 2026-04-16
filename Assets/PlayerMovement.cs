using UnityEngine;
using Photon.Pun;

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

        targetRotationAngle = transform.eulerAngles.z;

        SetupEngineAudio();
        lastAudioPosition = transform.position;

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
    }

    void Update()
    {
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
            rb.linearVelocity = effectiveMoveInput * currentSpeed;
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
            return rawInput.normalized;

        return rawInput;
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
