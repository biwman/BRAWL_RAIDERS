using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    public Joystick shootJoystick;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireRate = 0.3f;

    private float nextFireTime = 0f;

    void Start()
    {
        if (!photonView.IsMine) return;

        if (shootJoystick == null)
        {
            GameObject sj = GameObject.Find("ShootJoystick");
            if (sj != null)
                shootJoystick = sj.GetComponent<Joystick>();
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        Vector2 direction = shootJoystick != null ? shootJoystick.inputVector : Vector2.zero;

        if (direction.magnitude > 0.2f)
        {
            if (Time.time >= nextFireTime)
            {
                Shoot(direction);
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void Shoot(Vector2 direction)
    {
        Vector3 spawnPos = transform.position + transform.up * 0.5f;

        // 🔥 KLUCZOWE: zamiast Instantiate
        GameObject bullet = PhotonNetwork.Instantiate(
            bulletPrefab.name,
            spawnPos,
            Quaternion.identity
        );

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * bulletSpeed;

        // ignoruj kolizję z graczem
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();

        if (bulletCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(bulletCollider, playerCollider);
    }
}