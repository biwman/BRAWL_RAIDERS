using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerProfilePanelUI : MonoBehaviour
{
    static readonly string[] GameplayHudObjectNames =
    {
        "JoystickBG",
        "ShootJoystickBG",
        "CollectButton",
        "ShipInventoryButton",
        "ShipInventoryPanel",
        "ReloadButton",
        "TimerText",
        "HP_Bar",
        "Shield_Bar",
        "Booster_Bar",
        "ScoreText",
        "ExtractionMessage"
    };

    static PlayerProfilePanelUI instance;
    readonly Dictionary<string, GameObject> gameplayHudObjectsByName = new Dictionary<string, GameObject>();

    GameObject panelObject;
    TMP_InputField nicknameInput;
    TMP_Text accountText;
    TMP_Text statusText;
    TMP_Text gamesPlayedText;
    TMP_Text totalXpText;
    TMP_Text astronsText;
    TMP_Text inventoryHintText;
    Button saveAndRunButton;
    Button[] shipTypeButtons;
    Button[] skinButtons;
    Button[] shipInventoryButtons;
    Button[] playerInventoryButtons;
    TMP_Text[] shipInventoryTexts;
    TMP_Text[] playerInventoryTexts;
    Image[] shipInventoryIcons;
    Image[] playerInventoryIcons;
    TMP_Text shipTypeLabelText;
    TMP_Text shipSkinLabelText;
    TMP_Text shipInventoryLabelText;
    TMP_Text playerInventoryLabelText;
    TMP_Text shipPreviewTitleText;
    TMP_Text[] equipmentSlotPreviewTexts;
    Image shipPreviewImage;
    GameObject itemPreviewPanelObject;
    Image itemPreviewIcon;
    TMP_Text itemPreviewNameText;
    TMP_Text itemPreviewPriceText;
    GameObject splashScreenObject;
    Image splashScreenImage;
    float splashHideTime;
    static bool splashShownOnce;
    int selectedSkin;
    bool inventoryActionInProgress;
    Coroutine holdSellRoutine;
    bool suppressNextInventoryClick;
    bool dragInProgress;
    GameObject dragVisualObject;
    Image dragVisualIcon;
    TMP_Text dragVisualLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("PlayerProfilePanelUI");
        instance = root.AddComponent<PlayerProfilePanelUI>();
        DontDestroyOnLoad(root);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerProfileService.Instance.ProfileChanged += OnProfileChanged;
    }

    async void Start()
    {
        await PlayerProfileService.Instance.EnsureInitializedAsync();
        EnsurePanel();
        RefreshView();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (PlayerProfileService.HasInstance)
            PlayerProfileService.Instance.ProfileChanged -= OnProfileChanged;

        if (instance == this)
            instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePanel();
        RefreshView();
    }

    void OnProfileChanged(PlayerProfileData profile)
    {
        RefreshView();
        if (!NetworkManager.SessionRequested)
        {
            SetInteractable(true);
        }
        RefreshLobbyUi();
    }

    void Update()
    {
        EnsurePanel();
        RefreshVisibility();
        UpdateSkinButtonVisuals();
    }

    void EnsurePanel()
    {
        GameObject canvasObject = GameObject.Find("Canvas");
        if (canvasObject == null)
            return;

        if (panelObject != null && panelObject.scene.IsValid())
        {
            if (panelObject.transform.parent != canvasObject.transform)
                panelObject.transform.SetParent(canvasObject.transform, false);

            return;
        }

        CreatePanel(canvasObject.transform);
        RefreshView();
    }

    void CreatePanel(Transform parent)
    {
        panelObject = new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.08f, 0.12f, 1f);
        background.type = Image.Type.Sliced;

        CreateText(panelObject.transform, "ProfileTitle", "PLAYER PROFILE", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -28f), new Vector2(360f, 40f), 34f, TextAlignmentOptions.Left);
        accountText = CreateText(panelObject.transform, "AccountText", "Connecting...", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -68f), new Vector2(360f, 24f), 16f, TextAlignmentOptions.Left);
        gamesPlayedText = CreateText(panelObject.transform, "GamesPlayedText", "Games Played: 0", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -100f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Left);
        totalXpText = CreateText(panelObject.transform, "TotalXpText", "Total XP: 0", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, -100f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Left);
        astronsText = CreateText(panelObject.transform, "AstronsText", "Astrons: 0", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(650f, -100f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Left);

        CreateText(panelObject.transform, "NicknameLabel", "NICKNAME", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -146f), new Vector2(140f, 24f), 18f, TextAlignmentOptions.Left);

        GameObject inputObject = new GameObject("NicknameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(panelObject.transform, false);

        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 1f);
        inputRect.anchorMax = new Vector2(0f, 1f);
        inputRect.pivot = new Vector2(0f, 1f);
        inputRect.anchoredPosition = new Vector2(210f, -174f);
        inputRect.sizeDelta = new Vector2(320f, 42f);

        Image inputBackground = inputObject.GetComponent<Image>();
        inputBackground.color = new Color(0.15f, 0.2f, 0.27f, 0.98f);

        GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(inputObject.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 8f);
        viewportRect.offsetMax = new Vector2(-12f, -8f);

        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", "Nickname", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Left);
        placeholder.color = new Color(0.74f, 0.79f, 0.86f, 0.5f);

        TMP_Text inputText = CreateText(viewport.transform, "Text", string.Empty, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Left);
        inputText.color = new Color(0.96f, 0.98f, 1f, 1f);

        nicknameInput = inputObject.GetComponent<TMP_InputField>();
        nicknameInput.targetGraphic = inputBackground;
        nicknameInput.textViewport = viewportRect;
        nicknameInput.textComponent = inputText;
        nicknameInput.placeholder = placeholder;
        nicknameInput.lineType = TMP_InputField.LineType.SingleLine;
        nicknameInput.contentType = TMP_InputField.ContentType.Standard;
        nicknameInput.characterLimit = 18;

        CreateSplashScreen(panelObject.transform);

        shipTypeLabelText = CreateText(panelObject.transform, "ShipTypeLabel", "SHIP", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-304f, -214f), new Vector2(260f, 24f), 18f, TextAlignmentOptions.Left);

        shipTypeButtons = new Button[2];
        shipTypeButtons[0] = CreateButton(panelObject.transform, "ExplorerShipButton", "EXPLORER", new Vector2(404f, -242f), new Vector2(156f, 40f), () =>
        {
            SetSelectedShipType(ShipType.Explorer);
        });
        shipTypeButtons[1] = CreateButton(panelObject.transform, "ViperShipButton", "VIPER", new Vector2(580f, -242f), new Vector2(156f, 40f), () =>
        {
            SetSelectedShipType(ShipType.Viper);
        });

        shipSkinLabelText = CreateText(panelObject.transform, "SkinLabel", "SHIP SKIN", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-304f, -294f), new Vector2(300f, 24f), 18f, TextAlignmentOptions.Left);

        skinButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int capturedIndex = i;
            skinButtons[i] = CreateButton(panelObject.transform, "ShipSkinButton" + i, "SKIN", new Vector2(346f + (146f * i), -322f), new Vector2(126f, 56f), () =>
            {
                ApplySkinChoiceByButtonIndex(capturedIndex);
            });
        }

        shipPreviewTitleText = CreateText(panelObject.transform, "ShipPreviewTitle", "SHIP LOADOUT", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-304f, -506f), new Vector2(360f, 24f), 18f, TextAlignmentOptions.Left);
        CreateShipPreview(panelObject.transform);

        inventoryHintText = CreateText(panelObject.transform, "InventoryHintText", "Tap to preview. Drag between inventories. Hold 2s to sell for Astrons.", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(680f, 24f), 16f, TextAlignmentOptions.Center);
        inventoryHintText.fontStyle = FontStyles.Normal;

        shipInventoryLabelText = CreateText(panelObject.transform, "ShipInventoryLabel", "SHIP INVENTORY", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-776f, -320f), new Vector2(320f, 24f), 18f, TextAlignmentOptions.Center);
        CreateInventoryGrid(panelObject.transform, false, new Vector2(-900f, -352f), 2, 5, out shipInventoryButtons, out shipInventoryTexts, out shipInventoryIcons);

        playerInventoryLabelText = CreateText(panelObject.transform, "PlayerInventoryLabel", "PLAYER INVENTORY (50)", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-621f, -536f), new Vector2(380f, 24f), 18f, TextAlignmentOptions.Center);
        CreateInventoryGrid(panelObject.transform, true, new Vector2(-900f, -568f), 5, 10, out playerInventoryButtons, out playerInventoryTexts, out playerInventoryIcons);

        CreateItemPreview(panelObject.transform);

        saveAndRunButton = CreateButton(panelObject.transform, "SaveAndRunButton", "SAVE & RUN", new Vector2(820f, -72f), new Vector2(210f, 54f), OnSaveAndRunClicked);
        statusText = CreateText(panelObject.transform, "ProfileStatusText", string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(320f, 24f), 16f, TextAlignmentOptions.Center);
    }

    void CreateShipPreview(Transform parent)
    {
        GameObject previewRoot = new GameObject("ShipPreviewRoot", typeof(RectTransform), typeof(Image));
        previewRoot.transform.SetParent(parent, false);

        RectTransform rootRect = previewRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-56f, -570f);
        rootRect.sizeDelta = new Vector2(440f, 208f);

        Image rootImage = previewRoot.GetComponent<Image>();
        rootImage.color = new Color(0.12f, 0.16f, 0.2f, 0.7f);

        GameObject imageObject = new GameObject("ShipPreviewImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(previewRoot.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, 0f);
        imageRect.sizeDelta = new Vector2(200f, 120f);
        shipPreviewImage = imageObject.GetComponent<Image>();
        shipPreviewImage.preserveAspect = true;

        equipmentSlotPreviewTexts = new TMP_Text[6];
        equipmentSlotPreviewTexts[0] = CreateEquipmentSlotBadge(previewRoot.transform, "MainGunA", new Vector2(-154f, -48f), "MAIN GUN");
        equipmentSlotPreviewTexts[1] = CreateEquipmentSlotBadge(previewRoot.transform, "MainGunB", new Vector2(154f, -48f), "MAIN GUN");
        equipmentSlotPreviewTexts[2] = CreateEquipmentSlotBadge(previewRoot.transform, "ShieldSlot", new Vector2(0f, -150f), "SHIELD");
        equipmentSlotPreviewTexts[3] = CreateEquipmentSlotBadge(previewRoot.transform, "EngineA", new Vector2(-124f, 10f), "ENGINE");
        equipmentSlotPreviewTexts[4] = CreateEquipmentSlotBadge(previewRoot.transform, "EngineB", new Vector2(124f, 10f), "ENGINE");
        equipmentSlotPreviewTexts[5] = CreateEquipmentSlotBadge(previewRoot.transform, "GadgetSlot", new Vector2(0f, 56f), "GADGET");
    }

    void CreateSplashScreen(Transform parent)
    {
        splashScreenObject = new GameObject("StartupSplashScreen", typeof(RectTransform), typeof(Image));
        splashScreenObject.transform.SetParent(parent, false);

        RectTransform rect = splashScreenObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image splashBackground = splashScreenObject.GetComponent<Image>();
        splashBackground.color = Color.black;
        splashBackground.raycastTarget = false;

        GameObject logoObject = new GameObject("StartupSplashLogo", typeof(RectTransform), typeof(Image));
        logoObject.transform.SetParent(splashScreenObject.transform, false);
        RectTransform logoRect = logoObject.GetComponent<RectTransform>();
        logoRect.anchorMin = Vector2.zero;
        logoRect.anchorMax = Vector2.one;
        logoRect.offsetMin = Vector2.zero;
        logoRect.offsetMax = Vector2.zero;

        splashScreenImage = logoObject.GetComponent<Image>();
        splashScreenImage.color = Color.white;
        splashScreenImage.preserveAspect = true;
        splashScreenImage.raycastTarget = false;
        splashScreenImage.sprite = LoadStandaloneSprite("STAR_RAIDERS_ekran.png");

        if (!splashShownOnce)
        {
            splashHideTime = Time.unscaledTime + 3f;
            splashShownOnce = true;
        }
        else
        {
            splashHideTime = -1f;
            splashScreenObject.SetActive(false);
        }
    }

    void CreateItemPreview(Transform parent)
    {
        itemPreviewPanelObject = new GameObject("ItemPreviewPanel", typeof(RectTransform), typeof(Image));
        itemPreviewPanelObject.transform.SetParent(parent, false);

        RectTransform rect = itemPreviewPanelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(220f, 228f);

        Image background = itemPreviewPanelObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.12f, 0.16f, 0.92f);

        GameObject iconObject = new GameObject("ItemPreviewIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(itemPreviewPanelObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -18f);
        iconRect.sizeDelta = new Vector2(104f, 104f);
        itemPreviewIcon = iconObject.GetComponent<Image>();
        itemPreviewIcon.preserveAspect = true;

        itemPreviewNameText = CreateText(itemPreviewPanelObject.transform, "ItemPreviewNameText", "SELECT ITEM", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(190f, 26f), 21f, TextAlignmentOptions.Center);
        itemPreviewPriceText = CreateText(itemPreviewPanelObject.transform, "ItemPreviewPriceText", "Value: 0 Astrons", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(190f, 24f), 18f, TextAlignmentOptions.Center);
        itemPreviewPriceText.fontStyle = FontStyles.Normal;
        itemPreviewPanelObject.SetActive(false);
    }

    TMP_Text CreateEquipmentSlotBadge(Transform parent, string name, Vector2 anchoredPosition, string label)
    {
        GameObject slotObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        slotObject.transform.SetParent(parent, false);
        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(100f, 28f);

        Image bg = slotObject.GetComponent<Image>();
        bg.color = new Color(0.17f, 0.22f, 0.28f, 0.88f);

        TMP_Text text = CreateText(slotObject.transform, name + "Text", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 12f, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    void CreateInventoryGrid(Transform parent, bool isPlayerInventory, Vector2 startPosition, int rows, int columns, out Button[] buttons, out TMP_Text[] labels, out Image[] icons)
    {
        buttons = new Button[rows * columns];
        labels = new TMP_Text[rows * columns];
        icons = new Image[rows * columns];

        const float slotSize = 60f;
        const float slotSpacing = 8f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                Vector2 position = new Vector2(
                    startPosition.x + col * (slotSize + slotSpacing),
                    startPosition.y - row * (slotSize + slotSpacing));

                buttons[index] = CreateInventorySlot(parent, (isPlayerInventory ? "PlayerSlot" : "ShipSlot") + index, position, new Vector2(slotSize, slotSize), isPlayerInventory, index, out labels[index], out icons[index]);
            }
        }
    }

    Button CreateInventorySlot(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, bool isPlayerInventory, int slotIndex, out TMP_Text label, out Image icon)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.21f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => OnInventorySlotClicked(isPlayerInventory, slotIndex));

        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
        trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => OnInventorySlotHoldStart(isPlayerInventory, slotIndex));
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => OnInventorySlotHoldEnd());
        trigger.triggers.Add(up);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => OnInventorySlotHoldEnd());
        trigger.triggers.Add(exit);

        ProfileInventorySlotDragHandler dragHandler = buttonObject.AddComponent<ProfileInventorySlotDragHandler>();
        dragHandler.owner = this;
        dragHandler.isPlayerInventory = isPlayerInventory;
        dragHandler.slotIndex = slotIndex;

        GameObject iconObject = new GameObject(objectName + "Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(42f, 42f);

        icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;

        label = CreateText(buttonObject.transform, objectName + "Text", string.Empty, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 13f, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.margin = new Vector4(3f, 3f, 3f, 3f);

        return button;
    }

    Button CreateButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size, Action onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.2f, 0.27f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => onClick?.Invoke());

        TMP_Text text = CreateText(buttonObject.transform, objectName + "Text", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18f, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        if (objectName.StartsWith("ShipSkinButton", StringComparison.Ordinal))
        {
            text.fontSize = 15f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(6f, 4f, 6f, 4f);
        }

        return button;
    }

    TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.offsetMin = sizeDelta == Vector2.zero ? Vector2.zero : rect.offsetMin;
        rect.offsetMax = sizeDelta == Vector2.zero ? Vector2.zero : rect.offsetMax;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.97f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontStyle = FontStyles.Bold;

        TMP_Text reference = FindAnyObjectByType<TextMeshProUGUI>();
        if (reference != null)
        {
            text.font = reference.font;
            text.fontSharedMaterial = reference.fontSharedMaterial;
        }

        return text;
    }

    async void OnSaveAndRunClicked()
    {
        if (nicknameInput == null)
            return;

        SetStatus("Saving profile...");
        SetInteractable(false);

        try
        {
            await PlayerProfileService.Instance.SaveProfileAsync(nicknameInput.text, selectedSkin);
            SetStatus("Connecting...");
            NetworkManager.RequestSessionStart();
        }
        catch (Exception ex)
        {
            Debug.LogError("Profile save failed: " + ex);
            SetStatus("Save failed");
            SetInteractable(true);
        }
    }

    void RefreshView()
    {
        PlayerProfileData profile = PlayerProfileService.Instance.CurrentProfile;
        if (profile == null)
            return;

        selectedSkin = Mathf.Clamp(profile.ShipSkinIndex, 0, 3);

        if (nicknameInput != null && !nicknameInput.isFocused)
        {
            nicknameInput.text = profile.Nickname;
        }

        if (gamesPlayedText != null)
        {
            gamesPlayedText.text = "Games Played: " + profile.GamesPlayed;
        }

        if (totalXpText != null)
        {
            totalXpText.text = "Total XP: " + profile.TotalXp;
        }

        if (astronsText != null)
        {
            astronsText.text = "Astrons: " + profile.Astrons;
        }

        if (accountText != null)
        {
            string playerId = PlayerProfileService.Instance.PlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                accountText.text = PlayerProfileService.Instance.IsInitialized ? "Cloud linked" : "Connecting...";
            }
            else
            {
                string suffix = playerId.Length <= 8 ? playerId : playerId.Substring(playerId.Length - 8);
                accountText.text = "ID: " + suffix.ToUpperInvariant();
            }
        }

        UpdateShipTypeButtonVisuals();
        UpdateSkinButtonsForSelectedShip();
        UpdateSkinButtonVisuals();
        RefreshShipPreview();
        RefreshInventoryView(profile.Inventory);
    }

    ShipType GetSelectedShipType()
    {
        return ShipCatalog.GetShipTypeFromSkinIndex(selectedSkin);
    }

    async void SetSelectedShipType(ShipType shipType)
    {
        int[] allowedSkins = ShipCatalog.GetSkinsForShipType(shipType);
        int targetSkin = System.Array.IndexOf(allowedSkins, selectedSkin) >= 0 ? selectedSkin : allowedSkins[0];
        if (inventoryActionInProgress)
            return;

        inventoryActionInProgress = true;
        SetInteractable(false);
        SetStatus("Switching ship...");

        try
        {
            bool changed = await PlayerProfileService.Instance.TryChangeShipSkinAsync(targetSkin);
            if (!changed)
            {
                SetStatus("No room in player inventory for extra cargo.");
                RefreshView();
                return;
            }

            selectedSkin = targetSkin;
            RefreshView();
            SetStatus("Ship changed.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Ship switch failed: " + ex);
            SetStatus("Ship change failed.");
            RefreshView();
        }
        finally
        {
            inventoryActionInProgress = false;
            SetInteractable(true);
        }
    }

    void ApplySkinChoiceByButtonIndex(int buttonIndex)
    {
        int[] allowedSkins = ShipCatalog.GetSkinsForShipType(GetSelectedShipType());
        if (buttonIndex < 0 || buttonIndex >= allowedSkins.Length)
            return;

        selectedSkin = allowedSkins[buttonIndex];
        UpdateShipTypeButtonVisuals();
        UpdateSkinButtonsForSelectedShip();
        UpdateSkinButtonVisuals();
        RefreshShipPreview();
    }

    void UpdateShipTypeButtonVisuals()
    {
        if (shipTypeButtons == null)
            return;

        ShipType selectedType = GetSelectedShipType();
        for (int i = 0; i < shipTypeButtons.Length; i++)
        {
            if (shipTypeButtons[i] == null)
                continue;

            Image image = shipTypeButtons[i].GetComponent<Image>();
            if (image != null)
            {
                bool isSelected = (ShipType)i == selectedType;
                image.color = isSelected
                    ? new Color(0.19f, 0.61f, 0.5f, 0.98f)
                    : new Color(0.16f, 0.2f, 0.27f, 0.95f);
            }
        }
    }

    void UpdateSkinButtonsForSelectedShip()
    {
        if (skinButtons == null)
            return;

        ShipType shipType = GetSelectedShipType();
        int[] allowedSkins = ShipCatalog.GetSkinsForShipType(shipType);

        if (shipTypeLabelText != null)
            shipTypeLabelText.text = "SHIP: " + ShipCatalog.GetShipTypeDisplayName(shipType).ToUpperInvariant();

        if (shipSkinLabelText != null)
            shipSkinLabelText.text = shipType == ShipType.Viper ? "SHIP SKIN (VIPER)" : "SHIP SKIN (EXPLORER)";

        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (skinButtons[i] == null)
                continue;

            bool active = i < allowedSkins.Length;
            skinButtons[i].gameObject.SetActive(active);
            if (!active)
                continue;

            TMP_Text text = skinButtons[i].GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = ShipCatalog.GetSkinDisplayName(allowedSkins[i]).ToUpperInvariant();
        }
    }

    void UpdateSkinButtonVisuals()
    {
        if (skinButtons == null)
            return;

        int[] allowedSkins = ShipCatalog.GetSkinsForShipType(GetSelectedShipType());

        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (skinButtons[i] == null)
                continue;

            if (i >= allowedSkins.Length)
                continue;

            Image image = skinButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = allowedSkins[i] == selectedSkin
                    ? new Color(0.19f, 0.61f, 0.5f, 0.98f)
                    : new Color(0.16f, 0.2f, 0.27f, 0.95f);
            }
        }
    }

    void RefreshShipPreview()
    {
        if (shipPreviewTitleText != null)
        {
            int capacity = ShipCatalog.GetShipInventoryCapacity(selectedSkin);
            shipPreviewTitleText.text = ShipCatalog.GetShipTypeDisplayName(GetSelectedShipType()).ToUpperInvariant() + " LOADOUT  |  CARGO " + capacity;
        }

        if (shipPreviewImage != null)
        {
            shipPreviewImage.sprite = LoadShipPreviewSprite(selectedSkin);
            shipPreviewImage.color = shipPreviewImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        RefreshEquipmentSlotPreview();
    }

    void RefreshEquipmentSlotPreview()
    {
        if (equipmentSlotPreviewTexts == null || equipmentSlotPreviewTexts.Length < 6)
            return;

        int mainGunSlots = ShipCatalog.GetMainGunSlots(selectedSkin);
        int shieldSlots = ShipCatalog.GetShieldSlots(selectedSkin);
        int engineSlots = ShipCatalog.GetEngineSlots(selectedSkin);
        int gadgetSlots = ShipCatalog.GetGadgetSlots(selectedSkin);

        SetEquipmentSlotState(equipmentSlotPreviewTexts[0], mainGunSlots >= 1, "MAIN GUN");
        SetEquipmentSlotState(equipmentSlotPreviewTexts[1], mainGunSlots >= 2, "MAIN GUN");
        SetEquipmentSlotState(equipmentSlotPreviewTexts[2], shieldSlots >= 1, "SHIELD");
        SetEquipmentSlotState(equipmentSlotPreviewTexts[3], engineSlots >= 1, "ENGINE");
        SetEquipmentSlotState(equipmentSlotPreviewTexts[4], engineSlots >= 2, "ENGINE");
        SetEquipmentSlotState(equipmentSlotPreviewTexts[5], gadgetSlots >= 1, "GADGET");
    }

    void SetEquipmentSlotState(TMP_Text text, bool enabled, string label)
    {
        if (text == null)
            return;

        text.text = enabled ? label : "NO SLOT";
        text.color = enabled ? Color.white : new Color(0.58f, 0.62f, 0.68f, 0.82f);
        Image bg = text.transform.parent != null ? text.transform.parent.GetComponent<Image>() : null;
        if (bg != null)
            bg.color = enabled ? new Color(0.17f, 0.22f, 0.28f, 0.88f) : new Color(0.1f, 0.12f, 0.16f, 0.55f);
    }

    Sprite LoadShipPreviewSprite(int skinIndex)
    {
        string resourcesPath = skinIndex switch
        {
            1 => "Visuals/Ships/ship2_resource",
            2 => "Visuals/Ships/ship3_resource",
            3 => "ship4_resource",
            _ => "Visuals/Ships/ship1_resource"
        };

        string editorPath = skinIndex switch
        {
            1 => "Assets/Resources/Visuals/Ships/ship2_resource.png",
            2 => "Assets/Resources/Visuals/Ships/ship3_resource.png",
            3 => "Assets/Resources/ship4_resource.png",
            _ => "Assets/Resources/Visuals/Ships/ship1_resource.png"
        };

        string editorFallbackPath = skinIndex switch
        {
            1 => "Assets/ship2.png",
            2 => "Assets/ship3.png",
            3 => "Assets/ship4.png",
            _ => "Assets/ship1.png"
        };

        return LoadSpriteFromResourcesOrEditor(resourcesPath, editorPath, editorFallbackPath);
    }

    Sprite LoadStandaloneSprite(string fileName)
    {
        string resourcesPath = fileName switch
        {
            "STAR_RAIDERS_ekran.png" => "STAR_RAIDERS_ekran_resource",
            "ship1.png" => "Visuals/Ships/ship1_resource",
            "ship2.png" => "Visuals/Ships/ship2_resource",
            "ship3.png" => "Visuals/Ships/ship3_resource",
            "ship4.png" => "ship4_resource",
            _ => null
        };

        string editorResourcePath = fileName switch
        {
            "STAR_RAIDERS_ekran.png" => "Assets/Resources/STAR_RAIDERS_ekran_resource.png",
            "ship1.png" => "Assets/Resources/Visuals/Ships/ship1_resource.png",
            "ship2.png" => "Assets/Resources/Visuals/Ships/ship2_resource.png",
            "ship3.png" => "Assets/Resources/Visuals/Ships/ship3_resource.png",
            "ship4.png" => "Assets/Resources/ship4_resource.png",
            _ => null
        };

        string editorFallbackPath = string.IsNullOrWhiteSpace(fileName) ? null : "Assets/" + fileName;
        return LoadSpriteFromResourcesOrEditor(resourcesPath, editorResourcePath, editorFallbackPath);
    }

    Sprite LoadSpriteFromResourcesOrEditor(string resourcesPath, string editorPreferredPath, string editorFallbackPath = null)
    {
        Sprite sprite = LoadSpriteFromResources(resourcesPath);
        if (sprite != null)
            return sprite;

#if UNITY_EDITOR
        sprite = LoadEditorSprite(editorPreferredPath);
        if (sprite != null)
            return sprite;

        if (!string.IsNullOrWhiteSpace(editorFallbackPath))
            return LoadEditorSprite(editorFallbackPath);
#endif

        return null;
    }

    Sprite LoadSpriteFromResources(string resourcesPath)
    {
        if (string.IsNullOrWhiteSpace(resourcesPath))
            return null;

        Sprite sprite = Resources.Load<Sprite>(resourcesPath);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
        sprite = GetLargestSprite(sprites);
        if (sprite != null)
            return sprite;

        Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
        if (texture == null)
            return null;

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        float pixelsPerUnit = Mathf.Max(100f, Mathf.Max(texture.width, texture.height));
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    Sprite GetLargestSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
            return null;

        Sprite best = null;
        float bestArea = 0f;
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite candidate = sprites[i];
            if (candidate == null)
                continue;

            float area = candidate.rect.width * candidate.rect.height;
            if (best == null || area > bestArea)
            {
                best = candidate;
                bestArea = area;
            }
        }

        return best;
    }

#if UNITY_EDITOR
    Sprite LoadEditorSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite loadedSprite)
                return loadedSprite;
        }

        return null;
    }
#endif

    void RefreshVisibility()
    {
        if (panelObject == null)
            return;

        bool splashShowing = splashScreenObject != null && splashHideTime > 0f && Time.unscaledTime < splashHideTime;
        if (splashScreenObject != null)
        {
            splashScreenObject.SetActive(splashShowing);
            if (splashShowing)
                splashScreenObject.transform.SetAsLastSibling();
        }

        bool show = !PhotonNetwork.InRoom;
        panelObject.SetActive(show);
        SetGameplayHudVisible(!show);

        if (show)
        {
            SetInteractable(!NetworkManager.SessionRequested);
            if (statusText != null && statusText.text == "Connecting..." && !NetworkManager.SessionRequested)
            {
                statusText.text = string.Empty;
            }
        }
    }

    void SetStatus(string value)
    {
        if (statusText != null)
        {
            statusText.text = value;
        }
    }

    void SetInteractable(bool interactable)
    {
        if (nicknameInput != null)
            nicknameInput.interactable = interactable;

        if (saveAndRunButton != null)
            saveAndRunButton.interactable = interactable;

        if (shipTypeButtons != null)
        {
            for (int i = 0; i < shipTypeButtons.Length; i++)
            {
                if (shipTypeButtons[i] != null)
                    shipTypeButtons[i].interactable = interactable;
            }
        }

        if (skinButtons == null)
            return;

        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (skinButtons[i] != null)
                skinButtons[i].interactable = interactable;
        }

        SetInventoryInteractable(interactable && !inventoryActionInProgress);
    }

    void SetInventoryInteractable(bool interactable)
    {
        SetInventoryButtonState(playerInventoryButtons, interactable);
        SetInventoryButtonState(shipInventoryButtons, interactable);
    }

    void SetInventoryButtonState(Button[] buttons, bool interactable)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].interactable = interactable;
        }
    }

    void OnInventorySlotClicked(bool isPlayerInventory, int slotIndex)
    {
        if (suppressNextInventoryClick)
        {
            suppressNextInventoryClick = false;
            return;
        }

        if (inventoryActionInProgress || dragInProgress || !panelObject.activeSelf)
            return;

        if (TryGetInventoryItemId(isPlayerInventory, slotIndex, out string itemId))
        {
            ShowItemPreview(itemId);
            SetStatus(isPlayerInventory ? "Player item selected." : "Ship item selected.");
        }
        else
        {
            HideItemPreview();
            SetStatus(string.Empty);
        }
    }

    public void BeginSlotDrag(bool isPlayerInventory, int slotIndex, PointerEventData eventData)
    {
        if (inventoryActionInProgress || !panelObject.activeSelf)
            return;

        if (!TryGetInventoryItemId(isPlayerInventory, slotIndex, out string itemId))
            return;

        dragInProgress = true;
        suppressNextInventoryClick = true;
        OnInventorySlotHoldEnd();
        ShowItemPreview(itemId);
        EnsureDragVisual();
        UpdateDragVisualContent(itemId);
        UpdateDragVisualPosition(eventData);
        dragVisualObject.SetActive(true);
    }

    public void UpdateSlotDrag(bool isPlayerInventory, int slotIndex, PointerEventData eventData)
    {
        if (!dragInProgress || dragVisualObject == null)
            return;

        UpdateDragVisualPosition(eventData);
    }

    public async void EndSlotDrag(bool isPlayerInventory, int slotIndex, PointerEventData eventData)
    {
        if (!dragInProgress)
            return;

        dragInProgress = false;
        if (dragVisualObject != null)
            dragVisualObject.SetActive(false);

        bool? targetIsPlayerInventory = ResolveInventoryDropTarget(eventData != null ? eventData.pointerEnter : null);
        if (!targetIsPlayerInventory.HasValue || targetIsPlayerInventory.Value == isPlayerInventory)
            return;

        inventoryActionInProgress = true;
        SetInteractable(false);

        try
        {
            bool moved = await PlayerProfileService.Instance.MoveInventoryItemAsync(!isPlayerInventory, slotIndex);
            if (moved)
            {
                SetStatus(targetIsPlayerInventory.Value ? "Moved item to player inventory." : "Moved item to ship inventory.");
                RefreshView();
            }
            else
            {
                SetStatus(targetIsPlayerInventory.Value ? "No free player slot for this item." : "No free ship slot for this item.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Inventory move failed: " + ex);
            SetStatus("Inventory update failed.");
        }
        finally
        {
            inventoryActionInProgress = false;
            SetInteractable(true);
        }
    }

    void OnInventorySlotHoldStart(bool isPlayerInventory, int slotIndex)
    {
        if (inventoryActionInProgress || dragInProgress || !panelObject.activeSelf)
            return;

        if (holdSellRoutine != null)
            StopCoroutine(holdSellRoutine);

        holdSellRoutine = StartCoroutine(HoldSellRoutine(isPlayerInventory, slotIndex));
    }

    void OnInventorySlotHoldEnd()
    {
        if (holdSellRoutine != null)
        {
            StopCoroutine(holdSellRoutine);
            holdSellRoutine = null;
        }
    }

    bool TryGetInventoryItemId(bool isPlayerInventory, int slotIndex, out string itemId)
    {
        itemId = null;
        PlayerProfileData profile = PlayerProfileService.Instance.CurrentProfile;
        if (profile == null || profile.Inventory == null)
            return false;

        string[] slots = isPlayerInventory ? profile.Inventory.PlayerSlots : profile.Inventory.ShipSlots;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            return false;

        itemId = slots[slotIndex];
        return !string.IsNullOrWhiteSpace(itemId);
    }

    void ShowItemPreview(string itemId)
    {
        if (itemPreviewPanelObject == null || string.IsNullOrWhiteSpace(itemId))
            return;

        itemPreviewPanelObject.SetActive(true);
        itemPreviewIcon.sprite = InventoryItemCatalog.GetIcon(itemId);
        itemPreviewIcon.enabled = itemPreviewIcon.sprite != null;
        itemPreviewNameText.text = InventoryItemCatalog.GetDisplayName(itemId).ToUpperInvariant();
        itemPreviewPriceText.text = "Value: " + InventoryItemCatalog.GetSellValueAstrons(itemId) + " Astrons";

        Image bg = itemPreviewPanelObject.GetComponent<Image>();
        if (bg != null)
        {
            Color rarityColor = InventoryItemCatalog.GetRarityColor(itemId);
            bg.color = new Color(
                Mathf.Clamp01(rarityColor.r * 0.55f),
                Mathf.Clamp01(rarityColor.g * 0.55f),
                Mathf.Clamp01(rarityColor.b * 0.55f),
                0.95f);
        }
    }

    void HideItemPreview()
    {
        if (itemPreviewPanelObject != null)
            itemPreviewPanelObject.SetActive(false);
    }

    void EnsureDragVisual()
    {
        if (dragVisualObject != null)
            return;

        dragVisualObject = new GameObject("ProfileDragVisual", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        dragVisualObject.transform.SetParent(panelObject.transform, false);

        RectTransform rect = dragVisualObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(72f, 72f);

        Image bg = dragVisualObject.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.14f, 0.18f, 0.92f);

        CanvasGroup group = dragVisualObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0.94f;

        GameObject iconObject = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(dragVisualObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(48f, 48f);
        dragVisualIcon = iconObject.GetComponent<Image>();
        dragVisualIcon.preserveAspect = true;

        dragVisualLabel = CreateText(dragVisualObject.transform, "DragLabel", string.Empty, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 13f, TextAlignmentOptions.Center);
        dragVisualLabel.fontStyle = FontStyles.Bold;
        dragVisualLabel.color = Color.white;

        dragVisualObject.SetActive(false);
    }

    void UpdateDragVisualContent(string itemId)
    {
        if (dragVisualObject == null)
            return;

        Image bg = dragVisualObject.GetComponent<Image>();
        if (bg != null)
            bg.color = InventoryItemCatalog.GetRarityColor(itemId);

        Sprite icon = InventoryItemCatalog.GetIcon(itemId);
        dragVisualIcon.sprite = icon;
        dragVisualIcon.enabled = icon != null;
        dragVisualLabel.text = icon == null ? InventoryItemCatalog.GetShortLabel(itemId) : string.Empty;
    }

    void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragVisualObject == null || panelObject == null || eventData == null)
            return;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        RectTransform dragRect = dragVisualObject.GetComponent<RectTransform>();
        if (panelRect == null || dragRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            dragRect.anchoredPosition = localPoint;
    }

    bool? ResolveInventoryDropTarget(GameObject hoveredObject)
    {
        Transform current = hoveredObject != null ? hoveredObject.transform : null;
        while (current != null)
        {
            ProfileInventorySlotDragHandler slot = current.GetComponent<ProfileInventorySlotDragHandler>();
            if (slot != null)
                return slot.isPlayerInventory;

            current = current.parent;
        }

        return null;
    }

    System.Collections.IEnumerator HoldSellRoutine(bool isPlayerInventory, int slotIndex)
    {
        yield return new WaitForSeconds(2f);
        holdSellRoutine = null;
        TrySellInventoryItem(isPlayerInventory, slotIndex);
    }

    async void TrySellInventoryItem(bool isPlayerInventory, int slotIndex)
    {
        if (inventoryActionInProgress)
            return;

        string[] slots = isPlayerInventory
            ? PlayerProfileService.Instance.CurrentProfile.Inventory.PlayerSlots
            : PlayerProfileService.Instance.CurrentProfile.Inventory.ShipSlots;
        string itemId = slots != null && slotIndex >= 0 && slotIndex < slots.Length ? slots[slotIndex] : null;
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        inventoryActionInProgress = true;
        suppressNextInventoryClick = true;
        SetInteractable(false);

        try
        {
            int value = InventoryItemCatalog.GetSellValueAstrons(itemId);
            bool sold = await PlayerProfileService.Instance.SellInventoryItemAsync(!isPlayerInventory, slotIndex);
            if (sold)
                SetStatus("Sold for " + value + " Astrons.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Inventory sell failed: " + ex);
            SetStatus("Sell failed.");
        }
        finally
        {
            inventoryActionInProgress = false;
            SetInteractable(true);
            RefreshView();
        }
    }

    void RefreshInventoryView(PlayerInventoryData inventory)
    {
        PlayerInventoryData normalized = inventory != null ? inventory.Clone() : PlayerInventoryData.Default();
        normalized.Normalize();

        if (shipInventoryLabelText != null)
            shipInventoryLabelText.text = "SHIP INVENTORY (" + ShipCatalog.GetShipInventoryCapacity(selectedSkin) + ")";

        if (playerInventoryLabelText != null)
            playerInventoryLabelText.text = "PLAYER INVENTORY (" + PlayerInventoryData.PlayerSlotCount + ")";

        RefreshInventoryButtons(shipInventoryButtons, shipInventoryTexts, shipInventoryIcons, normalized.ShipSlots, true);
        RefreshInventoryButtons(playerInventoryButtons, playerInventoryTexts, playerInventoryIcons, normalized.PlayerSlots, false);
    }

    void RefreshInventoryButtons(Button[] buttons, TMP_Text[] labels, Image[] icons, string[] slots, bool isShipInventory)
    {
        if (buttons == null || labels == null || icons == null || slots == null)
            return;

        for (int i = 0; i < buttons.Length && i < slots.Length; i++)
        {
            string itemId = slots[i];
            bool occupied = !string.IsNullOrWhiteSpace(itemId);
            int shipCapacity = ShipCatalog.GetShipInventoryCapacity(selectedSkin);
            bool withinShipCapacity = !isShipInventory || i < shipCapacity;
            Image image = buttons[i] != null ? buttons[i].GetComponent<Image>() : null;
            Image icon = icons[i];
            Sprite itemSprite = occupied ? InventoryItemCatalog.GetIcon(itemId) : null;

            if (buttons[i] != null)
                buttons[i].gameObject.SetActive(withinShipCapacity);

            if (!withinShipCapacity)
                continue;

            if (labels[i] != null)
            {
                bool useTextLabel = occupied && itemSprite == null;
                labels[i].text = useTextLabel ? InventoryItemCatalog.GetShortLabel(itemId) : string.Empty;
                labels[i].color = useTextLabel ? new Color(0.97f, 0.99f, 1f, 1f) : new Color(0f, 0f, 0f, 0f);
            }

            if (icon != null)
            {
                icon.sprite = itemSprite;
                icon.enabled = occupied && itemSprite != null;
            }

            if (image != null)
            {
                image.color = occupied
                    ? InventoryItemCatalog.GetRarityColor(itemId)
                    : new Color(0.12f, 0.16f, 0.21f, 0.96f);
            }

            if (buttons[i] != null)
                buttons[i].interactable = !inventoryActionInProgress;
        }
    }

    void RefreshLobbyUi()
    {
        LobbyManager lobby = FindAnyObjectByType<LobbyManager>();
        if (lobby != null)
        {
            lobby.ForceRefreshUi();
        }
    }

    void SetGameplayHudVisible(bool visible)
    {
        for (int i = 0; i < GameplayHudObjectNames.Length; i++)
        {
            string objectName = GameplayHudObjectNames[i];
            if (!gameplayHudObjectsByName.TryGetValue(objectName, out GameObject target) || target == null)
            {
                target = GameObject.Find(objectName);
                if (target != null)
                    gameplayHudObjectsByName[objectName] = target;
            }

            if (target != null)
                target.SetActive(visible);
        }
    }
}

public class ProfileInventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerProfilePanelUI owner;
    public bool isPlayerInventory;
    public int slotIndex;

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginSlotDrag(isPlayerInventory, slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.UpdateSlotDrag(isPlayerInventory, slotIndex, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndSlotDrag(isPlayerInventory, slotIndex, eventData);
    }
}
