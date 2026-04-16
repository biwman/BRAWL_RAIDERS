using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

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
    public bool CanManualReload => photonView.IsMine && IsGameStarted() && !isReloading && currentAmmo > 0 && currentAmmo < maxAmmo;
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

        if (GetComponent<ReloadButtonUI>() == null)
        {
            gameObject.AddComponent<ReloadButtonUI>();
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

        photonView.RPC(nameof(PlayLaserSfx), RpcTarget.All);
    }

    void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        if (currentAmmo <= 0)
        {
            StartReload(false);
        }
    }

    void StartReload(bool playSound)
    {
        if (isReloading)
            return;

        isReloading = true;
        reloadFinishTime = Time.time + reloadDuration;

        if (playSound)
        {
            photonView.RPC(nameof(PlayReloadSfx), RpcTarget.All);
        }
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

    public void TriggerManualReload()
    {
        if (!CanManualReload)
            return;

        StartReload(true);
    }

    [PunRPC]
    void PlayLaserSfx()
    {
        AudioManager.Instance.PlayLaserAt(transform.position);
    }

    [PunRPC]
    void PlayReloadSfx()
    {
        AudioManager.Instance.PlayReloadAt(transform.position);
    }
}

[RequireComponent(typeof(PlayerShooting))]
public class ReloadButtonUI : MonoBehaviourPun
{
    const string ReloadButtonName = "ReloadButton";

    PlayerShooting shooting;
    GameObject buttonObject;
    Button reloadButton;
    Image backgroundImage;
    TextMeshProUGUI buttonText;

    void Start()
    {
        shooting = GetComponent<PlayerShooting>();

        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        CreateButton();
        RefreshState();
    }

    void Update()
    {
        RefreshState();
    }

    void OnDestroy()
    {
        if (buttonObject != null)
        {
            Destroy(buttonObject);
        }
    }

    void CreateButton()
    {
        GameObject existing = GameObject.Find(ReloadButtonName);
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject canvas = GameObject.Find("Canvas");
        GameObject shootJoystickObject = GameObject.Find("ShootJoystickBG");
        if (canvas == null || shootJoystickObject == null)
            return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform joystickRect = shootJoystickObject.GetComponent<RectTransform>();
        if (canvasRect == null || joystickRect == null)
            return;

        buttonObject = new GameObject(ReloadButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = joystickRect.anchorMin;
        rect.anchorMax = joystickRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = joystickRect.anchoredPosition + new Vector2(0f, 208f);
        rect.sizeDelta = new Vector2(176f, 62f);

        backgroundImage = buttonObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.23f, 0.56f, 0.9f, 0.96f);
        backgroundImage.type = Image.Type.Sliced;

        reloadButton = buttonObject.GetComponent<Button>();
        reloadButton.transition = Selectable.Transition.ColorTint;
        reloadButton.targetGraphic = backgroundImage;
        reloadButton.onClick.AddListener(HandleReloadClicked);

        GameObject textObject = new GameObject("ReloadButtonText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        buttonText = textObject.GetComponent<TextMeshProUGUI>();
        buttonText.text = "RELOAD";
        buttonText.fontSize = 26f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.textWrappingMode = TextWrappingModes.NoWrap;
        buttonText.margin = new Vector4(12f, 6f, 12f, 6f);
        buttonText.color = Color.white;

        TMP_Text referenceText = FindAnyObjectByType<TMP_Text>();
        if (referenceText != null)
        {
            buttonText.font = referenceText.font;
            buttonText.fontSharedMaterial = referenceText.fontSharedMaterial;
        }
    }

    void RefreshState()
    {
        if (shooting == null || reloadButton == null || backgroundImage == null || buttonText == null)
            return;

        bool canReload = shooting.CanManualReload;
        reloadButton.interactable = canReload;
        backgroundImage.color = canReload
            ? new Color(0.23f, 0.56f, 0.9f, 0.96f)
            : new Color(0.14f, 0.18f, 0.24f, 0.78f);
        buttonText.color = canReload
            ? Color.white
            : new Color(0.82f, 0.86f, 0.91f, 0.82f);
    }

    void HandleReloadClicked()
    {
        if (shooting == null)
            return;

        shooting.TriggerManualReload();
    }
}
