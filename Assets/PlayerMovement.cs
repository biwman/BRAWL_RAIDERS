using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float speed = 5f;
    public Joystick joystick;
    public Joystick shootJoystick;

    private Vector2 moveInput;
    private Vector2 shootInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = joystick.inputVector;
        shootInput = shootJoystick.inputVector;

        // DEAD ZONE
        if (moveInput.magnitude < 0.2f)
        {
            moveInput = Vector2.zero;
        }

        if (shootInput.magnitude < 0.3f)
        {
            shootInput = Vector2.zero;
        }

        // OBRÓT (może zostać w Update)
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
        // RUCH tylko tutaj!
        rb.linearVelocity = moveInput * speed;
    }
}