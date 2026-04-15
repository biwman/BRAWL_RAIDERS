using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    const string BoosterSlowdownKey = "boosterSlowdownPercent";

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

    public float BoosterNormalized => boosterCharge;
    public bool IsBoosterDepleted => boosterExhausted;
    float CurrentDepletedSpeedMultiplier => 1f - GetBoosterSlowdownFraction();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.angularVelocity = 0f;
        }

        targetRotationAngle = transform.eulerAngles.z;

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
        if (!photonView.IsMine)
            return;

        if (!IsGameStarted())
        {
            moveInput = Vector2.zero;
            shootInput = Vector2.zero;
            effectiveMoveInput = Vector2.zero;
            if (rb != null)
            {
                rb.angularVelocity = 0f;
            }
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
        if (effectiveMoveInput.magnitude >= maxSpeedThreshold && !boosterExhausted)
        {
            boosterCharge -= deltaTime / boosterDuration;
        }
        else
        {
            boosterCharge += deltaTime / boosterDuration;
        }

        boosterCharge = Mathf.Clamp01(boosterCharge);

        if (!boosterExhausted && boosterCharge <= 0.001f)
        {
            boosterExhausted = true;
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

    float GetBoosterSlowdownFraction()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BoosterSlowdownKey, out object value))
        {
            if (value is int intValue)
                return Mathf.Clamp01(intValue / 100f);

            if (value is float floatValue)
                return Mathf.Clamp01(floatValue / 100f);
        }

        return 1f - depletedSpeedMultiplier;
    }
}
