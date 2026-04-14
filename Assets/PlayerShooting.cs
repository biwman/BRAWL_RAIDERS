using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    public Joystick shootJoystick;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireRate = 0.3f;
    public static bool gameStarted = false;
    private float nextFireTime = 0f;

    void Start()
    {
        if (!photonView.IsMine) return;

        Debug.Log("PlayerShooting START");
    }

    void Update()
    {
        if (!IsGameStarted()) return;
        if (!photonView.IsMine) return;

        // znajdź joystick (retry aż znajdzie)
        if (shootJoystick == null)
        {
            GameObject sj = GameObject.Find("ShootJoystickBG");

            if (sj != null)
            {
                shootJoystick = sj.GetComponent<Joystick>();
                Debug.Log("ShootJoystick znaleziony");
            }
        }

        // jeśli nadal brak joysticka → nie strzelamy
        if (shootJoystick == null) return;

        Vector2 direction = shootJoystick.IsPressed ? shootJoystick.inputVector : Vector2.zero;

        // dead zone
        if (direction.magnitude < 0.2f) return;

        if (Time.time >= nextFireTime)
        {
            Debug.Log("STRZAŁ");

            Shoot(direction.normalized);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("bulletPrefab NULL");
            return;
        }

        Vector3 spawnPos = transform.position + transform.up * 0.5f;

        GameObject bullet = PhotonNetwork.Instantiate(
            bulletPrefab.name,
            spawnPos,
            Quaternion.identity
        );
        PhotonView pv = GetComponent<PhotonView>();
        bullet.GetComponent<Bullet>().ownerViewID = pv.ViewID;

        if (bullet == null)
        {
            Debug.LogError("Bullet się nie stworzył");
            return;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }
        else
        {
            Debug.LogError("Bullet NIE ma Rigidbody2D");
        }

        // ignoruj kolizję z graczem
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();

        if (bulletCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, playerCollider);
        }
    }
    bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        object value;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out value))
        {
            return (bool)value;
        }

        return false;
    }
}
