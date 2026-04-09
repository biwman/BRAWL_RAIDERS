using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    public static bool gameStarted = false;

    private Rigidbody2D rb;
    public float speed = 5f;

    public Joystick joystick;
    public Joystick shootJoystick;

    private Vector2 moveInput;
    private Vector2 shootInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // ❌ zdalni gracze NIC nie robią
        if (!photonView.IsMine) return;

        // 🔥 kamera podąża za graczem
        CameraFollow cam = FindObjectOfType<CameraFollow>();
        if (cam != null)
        {
            cam.target = transform;
        }

        // 🎮 joystick ruchu
        if (joystick == null)
        {
            joystick = FindObjectOfType<Joystick>();
        }

        // 🎯 joystick strzału
        if (shootJoystick == null)
        {
            GameObject sj = GameObject.Find("ShootJoystickBG");
            if (sj != null)
            {
                shootJoystick = sj.GetComponent<Joystick>();
            }
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (!gameStarted) return;

        moveInput = joystick != null ? joystick.inputVector : Vector2.zero;
        shootInput = shootJoystick != null ? shootJoystick.inputVector : Vector2.zero;

        // DEAD ZONE
        if (moveInput.magnitude < 0.2f)
            moveInput = Vector2.zero;

        if (shootInput.magnitude < 0.3f)
            shootInput = Vector2.zero;

        // ROTACJA
        if (shootInput != Vector2.zero)
        {
            float angle = Mathf.Atan2(shootInput.y, shootInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        else if (moveInput != Vector2.zero)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // 🔥 blokada ruchu przed startem gry
        if (!gameStarted)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!photonView.IsMine) return;

        Debug.Log("DOTKNALEM: " + other.name);
    }
}