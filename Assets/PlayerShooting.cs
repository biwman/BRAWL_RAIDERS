using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Joystick shootJoystick;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireRate = 0.3f;
    private float nextFireTime = 0f;


    void Update()
    {
        Vector2 direction = shootJoystick.inputVector;

        if (direction.magnitude > 0.2f)
        {
            if (Time.time >= nextFireTime)
            {
                Shoot(direction);
                nextFireTime = Time.time + fireRate;
            }
            ;
        }
    }

    void Shoot(Vector2 direction)
    {
        Vector3 spawnPos = transform.position + transform.up * 0.5f;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * bulletSpeed;

        // ignoruj kolizję z graczem
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();

        Physics2D.IgnoreCollision(bulletCollider, playerCollider);
    }
}