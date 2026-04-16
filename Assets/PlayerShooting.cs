using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    public Joystick shootJoystick;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float fireRate = 0.3f;
    public int maxAmmo = 10;
    public float reloadDuration = 4f;
    public static bool gameStarted = false;

    float nextFireTime = 0f;
    int currentAmmo;
    bool isReloading;
    float reloadFinishTime;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsReloading => isReloading;
    public float ReloadProgress
    {
        get
        {
            if (!isReloading || reloadDuration <= 0f)
                return 0f;

            float remaining = Mathf.Max(0f, reloadFinishTime - Time.time);
            return 1f - Mathf.Clamp01(remaining / reloadDuration);
        }
    }

    void Start()
    {
        maxAmmo = GetConfiguredMaxAmmo();
        currentAmmo = maxAmmo;

        if (!photonView.IsMine)
            return;

        if (GetComponent<AmmoUI>() == null)
        {
            gameObject.AddComponent<AmmoUI>();
        }

        Debug.Log("PlayerShooting START");
    }

    void Update()
    {
        if (!IsGameStarted())
            return;

        if (!photonView.IsMine)
            return;

        SyncAmmoSetting();

        UpdateReload();

        if (shootJoystick == null)
        {
            GameObject shootJoystickObject = GameObject.Find("ShootJoystickBG");
            if (shootJoystickObject != null)
            {
                shootJoystick = shootJoystickObject.GetComponent<Joystick>();
                Debug.Log("ShootJoystick found");
            }
        }

        if (shootJoystick == null)
            return;

        if (isReloading || currentAmmo <= 0)
            return;

        Vector2 direction = shootJoystick.IsPressed ? shootJoystick.inputVector : Vector2.zero;
        if (direction.magnitude < 0.2f)
            return;

        if (Time.time >= nextFireTime)
        {
            Shoot(direction.normalized);
            ConsumeAmmo();
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

        if (bullet == null)
        {
            Debug.LogError("Bullet failed to spawn");
            return;
        }

        PhotonView playerView = GetComponent<PhotonView>();
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null && playerView != null)
        {
            bulletComponent.ownerViewID = playerView.ViewID;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }
        else
        {
            Debug.LogError("Bullet is missing Rigidbody2D");
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        if (bulletCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, playerCollider);
        }

        AudioManager.Instance.PlayLaser();
    }

    void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }

    void StartReload()
    {
        if (isReloading)
            return;

        isReloading = true;
        reloadFinishTime = Time.time + reloadDuration;
    }

    void UpdateReload()
    {
        if (!isReloading)
            return;

        if (Time.time < reloadFinishTime)
            return;

        isReloading = false;
        currentAmmo = maxAmmo;
    }

    void SyncAmmoSetting()
    {
        int configuredAmmo = GetConfiguredMaxAmmo();
        if (configuredAmmo == maxAmmo)
            return;

        int previousMaxAmmo = maxAmmo;
        maxAmmo = configuredAmmo;

        if (isReloading)
            return;

        if (currentAmmo == previousMaxAmmo)
        {
            currentAmmo = maxAmmo;
        }
        else
        {
            currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        }
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

    int GetConfiguredMaxAmmo()
    {
        return RoomSettings.GetAmmoCount();
    }
}
