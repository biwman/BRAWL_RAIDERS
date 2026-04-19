using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    const int DefaultPlayerHp = 50;
    const int DefaultPlayerShield = 50;

    public int maxHP = 50;
    public int maxShield = 50;

    int currentHP;
    int currentShield;
    Slider hpBar;
    bool messageShowing;

    public int CurrentHP => currentHP;
    public int CurrentShield => currentShield;
    public int MaxShield => maxShield;
    public bool IsWreck { get; private set; }
    public bool IsBotControlled => GetComponent<EnemyBot>() != null;
    public bool IsAstronautControlled => GetComponent<AstronautSurvivor>() != null;

    void Start()
    {
        if (!IsAstronautControlled)
        {
            maxHP = DefaultPlayerHp;
            maxShield = DefaultPlayerShield;
        }

        currentHP = maxHP;
        currentShield = maxShield;

        if (photonView.IsMine && !IsBotControlled)
        {
            PhotonNetwork.LocalPlayer.TagObject = gameObject;

            if (GetComponent<HealthBarUI>() == null)
            {
                gameObject.AddComponent<HealthBarUI>();
            }

            if (GetComponent<ShieldBarUI>() == null)
            {
                gameObject.AddComponent<ShieldBarUI>();
            }

            GameObject barObj = GameObject.Find("HP_Bar");
            if (barObj != null)
            {
                hpBar = barObj.GetComponent<Slider>();
                if (hpBar != null)
                {
                    hpBar.maxValue = maxHP;
                    hpBar.value = currentHP;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (photonView != null &&
            photonView.IsMine &&
            PhotonNetwork.LocalPlayer != null &&
            ReferenceEquals(PhotonNetwork.LocalPlayer.TagObject, gameObject))
        {
            PhotonNetwork.LocalPlayer.TagObject = null;
        }
    }

    [PunRPC]
    public void TakeDamage(int dmg, int attackerViewID)
    {
        if (!PhotonNetwork.IsMasterClient || IsWreck)
            return;

        int remainingDamage = Mathf.Max(0, dmg);
        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - remainingDamage);
        }

        photonView.RPC(nameof(SyncVitals), RpcTarget.All, currentHP, currentShield);

        if (currentHP <= 0)
        {
            HandleDeath(attackerViewID);
        }
    }

    [PunRPC]
    void SyncVitals(int newHP, int newShield)
    {
        currentHP = newHP;
        currentShield = newShield;

        if (photonView.IsMine && hpBar != null)
        {
            hpBar.maxValue = maxHP;
            hpBar.value = currentHP;
        }
    }

    public void ConfigureBaseStats(int hp, int shield)
    {
        maxHP = Mathf.Max(1, hp);
        maxShield = Mathf.Max(0, shield);
        currentHP = maxHP;
        currentShield = maxShield;

        if (photonView != null)
        {
            photonView.RPC(nameof(SyncVitals), RpcTarget.All, currentHP, currentShield);
        }
    }

    void HandleDeath(int attackerViewID)
    {
        photonView.RPC(nameof(PlayDeathExplosion), RpcTarget.All);
        if (!IsBotControlled)
        {
            photonView.RPC(nameof(ShowDeathMessage), RpcTarget.All);
        }

        if (IsBotControlled)
        {
            photonView.RPC(nameof(BecomeBotWreck), RpcTarget.All);
            return;
        }

        if (IsAstronautControlled)
        {
            if (!IsBotControlled)
            {
                int currentRoundXp = GetCurrentRoundXp();
                RoundResultsTracker.RecordOutcome(photonView.Owner, currentRoundXp, "dead");
            }
            photonView.RPC(nameof(DestroySelf), photonView.Owner);
            return;
        }

        string wreckLoot = PlayerProfileService.SerializeShipInventorySlots(PlayerProfileService.GetPlayerShipInventorySlots(photonView.Owner));
        int shipSkinIndex = RoomSettings.GetPlayerShipSkin(photonView.Owner, 0);
        Vector3 astronautSpawnPosition = transform.position + (transform.right * 0.85f);
        photonView.RPC(nameof(ClearLocalShipInventoryForWreck), photonView.Owner);
        photonView.RPC(nameof(SpawnAstronautAfterDestruction), photonView.Owner, astronautSpawnPosition.x, astronautSpawnPosition.y, transform.eulerAngles.z);
        photonView.RPC(nameof(BecomeWreck), RpcTarget.All, wreckLoot, shipSkinIndex);
    }

    int GetCurrentRoundXp()
    {
        int propScore = RoomSettings.GetPlayerRoundXp(photonView.Owner);
        if (propScore > 0)
            return propScore;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            return collector.totalScore;

        return 0;
    }

    [PunRPC]
    public void OnEvacuated(int amount)
    {
        if (!photonView.IsMine)
            return;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
        {
            collector.AddScore(amount);
        }
    }

    [PunRPC]
    public void DestroySelf()
    {
        if (!photonView.IsMine)
            return;

        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public async void ClearLocalShipInventoryForWreck()
    {
        if (!photonView.IsMine)
            return;

        try
        {
            await PlayerProfileService.Instance.ReplaceShipInventoryAsync(new string[PlayerInventoryData.ShipSlotCount]);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to clear ship inventory for wreck: " + ex);
        }
    }

    [PunRPC]
    void SpawnAstronautAfterDestruction(float x, float y, float rotationZ)
    {
        if (!photonView.IsMine)
            return;

        PhotonNetwork.LocalPlayer.TagObject = null;
        GameObject astronaut = PhotonNetwork.Instantiate(
            "Player",
            new Vector3(x, y, 0f),
            Quaternion.Euler(0f, 0f, rotationZ),
            0,
            new object[] { AstronautSurvivor.AstronautInstantiationMarker });

        if (astronaut != null)
        {
            PhotonNetwork.LocalPlayer.TagObject = astronaut;
        }
    }

    [PunRPC]
    void BecomeWreck(string serializedLoot, int shipSkinIndex)
    {
        IsWreck = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.enabled = false;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            collector.enabled = false;

        HealthBarUI healthBarUi = GetComponent<HealthBarUI>();
        if (healthBarUi != null)
            Destroy(healthBarUi);

        ShieldBarUI shieldBarUi = GetComponent<ShieldBarUI>();
        if (shieldBarUi != null)
            Destroy(shieldBarUi);

        BoosterBarUI boosterBarUi = GetComponent<BoosterBarUI>();
        if (boosterBarUi != null)
            Destroy(boosterBarUi);

        AmmoUI ammoUi = GetComponent<AmmoUI>();
        if (ammoUi != null)
            Destroy(ammoUi);

        ReloadButtonUI reloadButtonUi = GetComponent<ReloadButtonUI>();
        if (reloadButtonUi != null)
            Destroy(reloadButtonUi);

        ShipInventoryHudUI cargoHudUi = GetComponent<ShipInventoryHudUI>();
        if (cargoHudUi != null)
            Destroy(cargoHudUi);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 2.4f;
            body.linearDamping = 0.32f;
            body.angularDamping = 0.42f;

            Vector2 driftDirection = Random.insideUnitCircle.normalized;
            if (driftDirection.sqrMagnitude < 0.001f)
                driftDirection = Vector2.left;

            body.linearVelocity = driftDirection * 0.32f;
            body.angularVelocity = Random.Range(-12f, 12f);
        }

        ShipWreck wreck = GetComponent<ShipWreck>();
        if (wreck == null)
            wreck = gameObject.AddComponent<ShipWreck>();

        wreck.InitializeFromLootJson(serializedLoot, shipSkinIndex);
    }

    [PunRPC]
    void BecomeBotWreck()
    {
        IsWreck = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.enabled = false;

        EnemyBot bot = GetComponent<EnemyBot>();
        if (bot != null)
            bot.enabled = false;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            collector.enabled = false;

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = true;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 1.5f;
            body.linearDamping = 0.28f;
            body.angularDamping = 0.36f;

            Vector2 driftDirection = Random.insideUnitCircle.normalized;
            if (driftDirection.sqrMagnitude < 0.001f)
                driftDirection = Vector2.left;

            body.linearVelocity = driftDirection * 0.28f;
            body.angularVelocity = Random.Range(-18f, 18f);
        }

        ShipWreck wreck = GetComponent<ShipWreck>();
        if (wreck == null)
            wreck = gameObject.AddComponent<ShipWreck>();

        string serializedLoot = PlayerProfileService.SerializeShipInventorySlots(new[] { InventoryItemCatalog.DroidScrapId });
        wreck.InitializeFromLootJson(serializedLoot, -1);
    }

    [PunRPC]
    void ShowDeathMessage()
    {
        if (messageShowing)
            return;

        GameObject obj = FindObjectEvenIfDisabled("DeathMessage");
        if (obj != null)
        {
            messageShowing = true;
            obj.SetActive(true);
            StartCoroutine(HideDeathMessage(obj));
        }
    }

    IEnumerator HideDeathMessage(GameObject obj)
    {
        yield return new WaitForSeconds(3f);

        if (obj != null)
            obj.SetActive(false);

        messageShowing = false;
    }

    [PunRPC]
    void PlayDeathExplosion()
    {
        AudioManager.Instance.PlayExplosionAt(transform.position);
    }

    [PunRPC]
    public void OnTimeUp()
    {
        if (!photonView.IsMine)
            return;

        ShowTimeUpMessage();
        StartCoroutine(DieAfterDelay());
    }

    void ShowTimeUpMessage()
    {
        GameObject obj = GameObject.Find("TimeUpMessage");
        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator DestroyBotSafely()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.enabled = false;

        EnemyBot bot = GetComponent<EnemyBot>();
        if (bot != null)
            bot.enabled = false;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            collector.enabled = false;

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        yield return null;

        if (PhotonNetwork.IsConnected && photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
        else if (!PhotonNetwork.IsConnected)
            Destroy(gameObject);
    }

    GameObject FindObjectEvenIfDisabled(string name)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject go in all)
        {
            if (go.name == name)
                return go;
        }

        return null;
    }
}

[RequireComponent(typeof(PlayerHealth))]
public class ShieldBarUI : MonoBehaviourPun
{
    const string ShieldBarName = "Shield_Bar";
    const string ShieldLabelName = "ShieldLabel";
    const string ShieldValueName = "ShieldValue";

    PlayerHealth health;
    Slider shieldBar;
    RectTransform shieldRect;
    Image fillImage;
    Image backgroundImage;
    Image handleImage;
    TMPro.TextMeshProUGUI labelText;
    TMPro.TextMeshProUGUI valueText;
    bool isVisible = true;

    void Start()
    {
        health = GetComponent<PlayerHealth>();

        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        CreateShieldBar();
        RefreshBar();
    }

    void Update()
    {
        UpdateVisibility();
        RefreshBar();
    }

    void OnDestroy()
    {
        if (shieldBar != null)
            Destroy(shieldBar.gameObject);
    }

    void CreateShieldBar()
    {
        GameObject existingBar = GameObject.Find(ShieldBarName);
        if (existingBar != null)
            Destroy(existingBar);

        GameObject hpBarObject = GameObject.Find("HP_Bar");
        if (hpBarObject == null)
            return;

        GameObject clone = Object.Instantiate(hpBarObject, hpBarObject.transform.parent);
        clone.name = ShieldBarName;

        RectTransform hpRect = hpBarObject.GetComponent<RectTransform>();
        shieldRect = clone.GetComponent<RectTransform>();
        shieldRect.sizeDelta = new Vector2(560f, 44f);
        shieldRect.anchoredPosition = hpRect.anchoredPosition + new Vector2(0f, -55f);

        shieldBar = clone.GetComponent<Slider>();
        shieldBar.minValue = 0f;
        shieldBar.maxValue = health != null ? health.MaxShield : 50f;
        shieldBar.wholeNumbers = false;

        backgroundImage = FindImage(clone.transform, "Background");
        fillImage = FindImage(clone.transform, "Fill");
        handleImage = FindImage(clone.transform, "Handle");
        DestroyIfExists(clone.transform, "HealthLabel");
        DestroyIfExists(clone.transform, "HealthValue");
        DestroyIfExists(clone.transform, ShieldLabelName);
        DestroyIfExists(clone.transform, ShieldValueName);

        if (backgroundImage != null)
            backgroundImage.color = new Color(0.05f, 0.08f, 0.16f, 0.95f);

        if (fillImage != null)
            fillImage.color = new Color(0.24f, 0.62f, 1f, 1f);

        labelText = CreateText(clone.transform, ShieldLabelName, new Vector2(12f, 0f), TMPro.TextAlignmentOptions.Left, "SHIELD");
        valueText = CreateText(clone.transform, ShieldValueName, new Vector2(-14f, 0f), TMPro.TextAlignmentOptions.Right, string.Empty);
    }

    void RefreshBar()
    {
        if (health == null || shieldBar == null)
            return;

        shieldBar.maxValue = health.MaxShield;
        shieldBar.value = health.CurrentShield;

        if (valueText != null)
            valueText.text = health.CurrentShield + " / " + health.MaxShield;

        float normalized = shieldBar.maxValue > 0f ? shieldBar.value / shieldBar.maxValue : 0f;
        if (fillImage != null)
            fillImage.color = Color.Lerp(new Color(0.08f, 0.18f, 0.55f, 1f), new Color(0.36f, 0.78f, 1f, 1f), normalized);

        if (handleImage != null)
        {
            handleImage.color = normalized >= 0.999f
                ? new Color(0.84f, 0.94f, 1f, 1f)
                : new Color(0.56f, 0.66f, 0.84f, 1f);
        }
    }

    void UpdateVisibility()
    {
        if (shieldBar == null)
            return;

        bool shouldBeVisible = health != null &&
                               health.MaxShield > 0 &&
                               PhotonNetwork.CurrentRoom != null &&
                               PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
                               value is bool started &&
                               started;
        if (isVisible == shouldBeVisible)
            return;

        isVisible = shouldBeVisible;
        shieldBar.gameObject.SetActive(shouldBeVisible);
    }

    TMPro.TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 anchoredPosition, TMPro.TextAlignmentOptions alignment, string initialText)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;

        TMPro.TextMeshProUGUI text = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = 20f;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        TMPro.TMP_Text referenceText = Object.FindAnyObjectByType<TMPro.TMP_Text>();
        if (referenceText != null)
        {
            text.font = referenceText.font;
            text.fontSharedMaterial = referenceText.fontSharedMaterial;
        }

        return text;
    }

    void DestroyIfExists(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            Destroy(existing.gameObject);
    }

    Image FindImage(Transform root, string objectName)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name == objectName)
                return image;
        }

        return null;
    }
}

[RequireComponent(typeof(PhotonView))]
public class AstronautSurvivor : MonoBehaviourPun
{
    public const string AstronautInstantiationMarker = "astronaut_survivor";
    const float AstronautTargetSize = 0.56f;

    static Sprite cachedAstronautSprite;
    bool initialized;
    SpriteRenderer cachedRenderer;

    public static bool IsAstronautInstantiationData(object[] data)
    {
        return data != null &&
               data.Length > 0 &&
               data[0] is string marker &&
               marker == AstronautInstantiationMarker;
    }

    public void InitializeFromPhotonData()
    {
        if (initialized)
            return;

        initialized = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        cachedRenderer = renderer;
        if (renderer != null)
        {
            Sprite astronautSprite = LoadAstronautSprite();
            if (astronautSprite != null)
            {
                renderer.sprite = astronautSprite;
                renderer.color = Color.white;
                FitRendererToTargetSize(renderer, AstronautTargetSize);
            }
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.speed = Mathf.Max(1.2f, movement.speed / 3f);
            movement.boosterDuration = 9999f;
        }

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.enabled = false;

        ShipInventoryHudUI cargoHud = GetComponent<ShipInventoryHudUI>();
        if (cargoHud != null)
            cargoHud.enabled = false;

        AmmoUI ammoUi = GetComponent<AmmoUI>();
        if (ammoUi != null)
            ammoUi.enabled = false;

        ReloadButtonUI reloadUi = GetComponent<ReloadButtonUI>();
        if (reloadUi != null)
            reloadUi.enabled = false;

        BoosterBarUI boosterUi = GetComponent<BoosterBarUI>();
        if (boosterUi != null)
            boosterUi.enabled = false;

        EngineThrusterVFX thruster = GetComponent<EngineThrusterVFX>();
        if (thruster != null)
            thruster.RefreshMode();

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
            health.ConfigureBaseStats(20, 0);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.mass = 0.03f;
            body.linearDamping = 0.3f;
            body.angularDamping = 0.8f;
        }

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null && renderer != null)
        {
            Vector2 spriteSize = renderer.bounds.size;
            SetWorldBoxSize(boxCollider, new Vector2(spriteSize.x * 0.42f, spriteSize.y * 0.58f));
        }

        CircleCollider2D triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider != null)
            triggerCollider.enabled = false;

    }

    void LateUpdate()
    {
        if (cachedRenderer != null)
            cachedRenderer.color = Color.white;
    }

    Sprite LoadAstronautSprite()
    {
        if (cachedAstronautSprite != null)
            return cachedAstronautSprite;

        string filePath = System.IO.Path.Combine(Application.dataPath, "kosmonauta.png");
        if (!System.IO.File.Exists(filePath))
            return null;

        byte[] bytes = System.IO.File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        cachedAstronautSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedAstronautSprite;
    }

    void SetWorldBoxSize(BoxCollider2D collider2D, Vector2 worldSize)
    {
        Vector3 scale = collider2D.transform.lossyScale;
        float safeX = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
        float safeY = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;
        collider2D.size = new Vector2(worldSize.x / safeX, worldSize.y / safeY);
    }

    void FitRendererToTargetSize(SpriteRenderer renderer, float targetSize)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Bounds spriteBounds = renderer.sprite.bounds;
        float largestDimension = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);
        if (largestDimension <= 0.0001f)
            return;

        float scale = targetSize / largestDimension;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
