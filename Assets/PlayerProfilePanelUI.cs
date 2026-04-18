using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerProfilePanelUI : MonoBehaviour
{
    static PlayerProfilePanelUI instance;

    GameObject panelObject;
    TMP_InputField nicknameInput;
    TMP_Text accountText;
    TMP_Text statusText;
    TMP_Text gamesPlayedText;
    TMP_Text totalXpText;
    TMP_Text inventoryHintText;
    Button saveAndRunButton;
    Button[] skinButtons;
    Button[] shipInventoryButtons;
    Button[] playerInventoryButtons;
    TMP_Text[] shipInventoryTexts;
    TMP_Text[] playerInventoryTexts;
    Image[] shipInventoryIcons;
    Image[] playerInventoryIcons;
    int selectedSkin;
    bool inventoryActionInProgress;

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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(720f, 930f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.12f, 0.16f, 0.95f);
        background.type = Image.Type.Sliced;

        CreateText(panelObject.transform, "ProfileTitle", "PLAYER PROFILE", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(540f, 40f), 34f, TextAlignmentOptions.Center);
        accountText = CreateText(panelObject.transform, "AccountText", "Connecting...", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(540f, 24f), 16f, TextAlignmentOptions.Center);
        gamesPlayedText = CreateText(panelObject.transform, "GamesPlayedText", "Games Played: 0", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(540f, 24f), 18f, TextAlignmentOptions.Center);
        totalXpText = CreateText(panelObject.transform, "TotalXpText", "Total XP: 0", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(540f, 24f), 18f, TextAlignmentOptions.Center);

        CreateText(panelObject.transform, "NicknameLabel", "NICKNAME", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -154f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Center);

        GameObject inputObject = new GameObject("NicknameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(panelObject.transform, false);

        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 1f);
        inputRect.anchorMax = new Vector2(0.5f, 1f);
        inputRect.pivot = new Vector2(0.5f, 1f);
        inputRect.anchoredPosition = new Vector2(0f, -184f);
        inputRect.sizeDelta = new Vector2(300f, 42f);

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

        CreateText(panelObject.transform, "SkinLabel", "SHIP SKIN", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Center);

        skinButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int capturedIndex = i;
            skinButtons[i] = CreateButton(panelObject.transform, "ShipSkinButton" + i, "SHIP " + (i + 1), new Vector2(-110f + (110f * i), -258f), new Vector2(96f, 40f), () =>
            {
                selectedSkin = capturedIndex;
                UpdateSkinButtonVisuals();
            });
        }

        for (int i = 0; i < 3; i++)
        {
            RectTransform skinRect = skinButtons[i].GetComponent<RectTransform>();
            skinRect.anchoredPosition += new Vector2(0f, -22f);
        }

        inventoryHintText = CreateText(panelObject.transform, "InventoryHintText", "Click an occupied slot to move that item to the other inventory.", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -336f), new Vector2(620f, 24f), 16f, TextAlignmentOptions.Center);
        inventoryHintText.fontStyle = FontStyles.Normal;

        CreateText(panelObject.transform, "ShipInventoryLabel", "SHIP INVENTORY (10)", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -372f), new Vector2(260f, 24f), 18f, TextAlignmentOptions.Center);
        CreateInventoryGrid(panelObject.transform, false, new Vector2(-150f, -406f), 2, 5, out shipInventoryButtons, out shipInventoryTexts, out shipInventoryIcons);

        CreateText(panelObject.transform, "PlayerInventoryLabel", "PLAYER INVENTORY (50)", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -538f), new Vector2(280f, 24f), 18f, TextAlignmentOptions.Center);
        CreateInventoryGrid(panelObject.transform, true, new Vector2(-303f, -572f), 5, 10, out playerInventoryButtons, out playerInventoryTexts, out playerInventoryIcons);

        saveAndRunButton = CreateButton(panelObject.transform, "SaveAndRunButton", "SAVE & RUN", new Vector2(0f, -854f), new Vector2(180f, 46f), OnSaveAndRunClicked);
        statusText = CreateText(panelObject.transform, "ProfileStatusText", string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(320f, 24f), 16f, TextAlignmentOptions.Center);
    }

    void CreateInventoryGrid(Transform parent, bool isPlayerInventory, Vector2 startPosition, int rows, int columns, out Button[] buttons, out TMP_Text[] labels, out Image[] icons)
    {
        buttons = new Button[rows * columns];
        labels = new TMP_Text[rows * columns];
        icons = new Image[rows * columns];

        const float slotSize = 54f;
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

        GameObject iconObject = new GameObject(objectName + "Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(38f, 38f);

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

        selectedSkin = Mathf.Clamp(profile.ShipSkinIndex, 0, 2);

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

        UpdateSkinButtonVisuals();
        RefreshInventoryView(profile.Inventory);
    }

    void UpdateSkinButtonVisuals()
    {
        if (skinButtons == null)
            return;

        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (skinButtons[i] == null)
                continue;

            Image image = skinButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i == selectedSkin
                    ? new Color(0.19f, 0.61f, 0.5f, 0.98f)
                    : new Color(0.16f, 0.2f, 0.27f, 0.95f);
            }
        }
    }

    void RefreshVisibility()
    {
        if (panelObject == null)
            return;

        bool show = !PhotonNetwork.InRoom;
        panelObject.SetActive(show);

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

    async void OnInventorySlotClicked(bool isPlayerInventory, int slotIndex)
    {
        if (inventoryActionInProgress || !panelObject.activeSelf)
            return;

        inventoryActionInProgress = true;
        SetInteractable(false);

        try
        {
            bool moved = await PlayerProfileService.Instance.MoveInventoryItemAsync(!isPlayerInventory, slotIndex);
            if (!moved)
            {
                SetStatus(isPlayerInventory
                    ? "No free ship slot for this item."
                    : "No free player slot for this item.");
            }
            else
            {
                SetStatus("Inventory updated.");
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
            RefreshView();
        }
    }

    void RefreshInventoryView(PlayerInventoryData inventory)
    {
        PlayerInventoryData normalized = inventory != null ? inventory.Clone() : PlayerInventoryData.Default();
        normalized.Normalize();

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
            Image image = buttons[i] != null ? buttons[i].GetComponent<Image>() : null;
            Image icon = icons[i];
            Sprite itemSprite = occupied ? InventoryItemCatalog.GetIcon(itemId) : null;

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
                    ? (isShipInventory ? new Color(0.18f, 0.39f, 0.58f, 0.98f) : new Color(0.18f, 0.5f, 0.38f, 0.98f))
                    : new Color(0.12f, 0.16f, 0.21f, 0.96f);
            }
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
}
