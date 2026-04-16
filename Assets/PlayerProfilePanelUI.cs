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
    Button saveAndRunButton;
    Button[] skinButtons;
    int selectedSkin;

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
        rect.sizeDelta = new Vector2(560f, 430f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.12f, 0.16f, 0.95f);
        background.type = Image.Type.Sliced;

        CreateText(panelObject.transform, "ProfileTitle", "PLAYER PROFILE", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(420f, 40f), 34f, TextAlignmentOptions.Center);
        accountText = CreateText(panelObject.transform, "AccountText", "Connecting...", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(420f, 24f), 16f, TextAlignmentOptions.Center);
        gamesPlayedText = CreateText(panelObject.transform, "GamesPlayedText", "Games Played: 0", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(420f, 24f), 18f, TextAlignmentOptions.Center);

        CreateText(panelObject.transform, "NicknameLabel", "NICKNAME", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Center);

        GameObject inputObject = new GameObject("NicknameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(panelObject.transform, false);

        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 1f);
        inputRect.anchorMax = new Vector2(0.5f, 1f);
        inputRect.pivot = new Vector2(0.5f, 1f);
        inputRect.anchoredPosition = new Vector2(0f, -162f);
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

        CreateText(panelObject.transform, "SkinLabel", "SHIP SKIN", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -224f), new Vector2(220f, 24f), 18f, TextAlignmentOptions.Center);

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

        saveAndRunButton = CreateButton(panelObject.transform, "SaveAndRunButton", "SAVE & RUN", new Vector2(0f, -332f), new Vector2(180f, 46f), OnSaveAndRunClicked);
        statusText = CreateText(panelObject.transform, "ProfileStatusText", string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(320f, 24f), 16f, TextAlignmentOptions.Center);
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
