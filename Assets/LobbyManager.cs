using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Text;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    const string RoundDurationKey = "roundDuration";
    const string ObstacleDensityKey = "obstacleDensity";
    const string TreasureDensityKey = "treasureDensity";
    const string ExtractionCountKey = "extractionCount";
    const string BoosterSlowdownKey = "boosterSlowdownPercent";
    const string AmmoCountKey = "ammoCount";

    static readonly float[] RoundDurationOptions = { 60f, 90f, 120f, 150f, 180f, 210f, 240f };
    static readonly string[] DensityOptions = { "low", "medium", "high" };
    static readonly int[] ExtractionCountOptions = { 1, 2, 3, 4 };
    static readonly int[] BoosterSlowdownOptions = { 30, 40, 50, 60, 70, 80, 90, 100 };
    static readonly int[] AmmoCountOptions = { 5, 10, 15, 20, 25, 30 };

    public Button readyButton;
    public TMP_Text readyText;
    public TMP_Text playerStatusListText;
    public TMP_Text roundSettingText;
    public TMP_Text obstacleSettingText;
    public TMP_Text treasureSettingText;
    public TMP_Text extractionSettingText;
    public TMP_Text boosterSettingText;
    public TMP_Text ammoSettingText;
    public Button roundSettingButton;
    public Button obstacleSettingButton;
    public Button treasureSettingButton;
    public Button extractionSettingButton;
    public Button boosterSettingButton;
    public Button ammoSettingButton;

    bool isReady = false;

    void Start()
    {
        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        ShowLobby();
        EnsurePlayerStatusListExists();
        EnsureHostSettingsUiExists();
        EnsureDefaultRoomSettings();

        if (readyText != null)
        {
            readyText.text = "NOT READY";
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(ToggleReady);
            readyButton.onClick.AddListener(ToggleReady);
        }

        if (PhotonNetwork.InRoom)
        {
            SetReady(false);
        }

        RefreshPlayerStatusList();
        RefreshHostSettingsUi();

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
            value is bool started && started)
        {
            Debug.Log("GAME ALREADY STARTED (Start)");
            HideLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        ShowLobby();
        EnsurePlayerStatusListExists();
        EnsureHostSettingsUiExists();
        EnsureDefaultRoomSettings();
        SetReady(false);
        RefreshPlayerStatusList();
        RefreshHostSettingsUi();
    }

    void ToggleReady()
    {
        isReady = !isReady;
        SetReady(isReady);
    }

    void SetReady(bool ready)
    {
        isReady = ready;

        Hashtable props = new Hashtable();
        props["ready"] = ready;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (readyText != null)
        {
            readyText.text = ready ? "READY" : "NOT READY";
        }

        RefreshPlayerStatusList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("ready"))
        {
            RefreshPlayerStatusList();
            CheckAllReady();
        }

        if (changedProps.ContainsKey(RoundDurationKey) ||
            changedProps.ContainsKey(ObstacleDensityKey) ||
            changedProps.ContainsKey(TreasureDensityKey) ||
            changedProps.ContainsKey(ExtractionCountKey) ||
            changedProps.ContainsKey(BoosterSlowdownKey) ||
            changedProps.ContainsKey(AmmoCountKey))
        {
            RefreshHostSettingsUi();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerStatusList();
        RefreshHostSettingsUi();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerStatusList();
        RefreshHostSettingsUi();
    }

    void CheckAllReady()
    {
        Debug.Log("SPRAWDZAM READY");

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("ready", out object readyValue))
            {
                return;
            }

            if (!(bool)readyValue)
            {
                return;
            }
        }

        Debug.Log("WSZYSCY GOTOWI");

        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        Debug.Log("START GRY");
        GameTimer.StartGame();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoundDurationKey) ||
            propertiesThatChanged.ContainsKey(ObstacleDensityKey) ||
            propertiesThatChanged.ContainsKey(TreasureDensityKey) ||
            propertiesThatChanged.ContainsKey(ExtractionCountKey) ||
            propertiesThatChanged.ContainsKey(BoosterSlowdownKey) ||
            propertiesThatChanged.ContainsKey(AmmoCountKey))
        {
            RefreshHostSettingsUi();
        }

        if (!propertiesThatChanged.ContainsKey("gameStarted"))
            return;

        bool started = false;
        if (propertiesThatChanged["gameStarted"] is bool startedValue)
        {
            started = startedValue;
        }

        if (started)
        {
            Debug.Log("GAME STARTED (ROOM PROP)");
            HideLobby();
        }
        else
        {
            Debug.Log("GAME RESET TO LOBBY");
            PlayerMovement.gameStarted = false;
            PlayerShooting.gameStarted = false;
            ShowLobby();
            SetReady(false);
            RefreshPlayerStatusList();
            RefreshHostSettingsUi();
        }
    }

    void HideLobby()
    {
        PlayerMovement.gameStarted = true;
        PlayerShooting.gameStarted = true;

        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg != null)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void ShowLobby()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg != null)
        {
            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    void EnsurePlayerStatusListExists()
    {
        if (playerStatusListText != null && playerStatusListText.gameObject.scene.IsValid())
            return;

        Transform existing = transform.Find("RoomPlayersText");
        if (existing != null)
        {
            playerStatusListText = existing.GetComponent<TMP_Text>();
            if (playerStatusListText != null)
                return;
        }

        GameObject textObject = new GameObject("RoomPlayersText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -26f);
        rect.sizeDelta = new Vector2(390f, 145f);

        playerStatusListText = textObject.GetComponent<TextMeshProUGUI>();
        playerStatusListText.fontSize = 22f;
        playerStatusListText.fontStyle = FontStyles.Bold;
        playerStatusListText.alignment = TextAlignmentOptions.TopLeft;
        playerStatusListText.enableWordWrapping = false;
        playerStatusListText.color = new Color(0.94f, 0.97f, 1f, 1f);
        playerStatusListText.text = string.Empty;
    }

    void EnsureHostSettingsUiExists()
    {
        roundSettingButton = EnsureSettingButton(ref roundSettingText, roundSettingButton, "RoundSettingButton", "RoundSettingText", new Vector2(-120f, -208f), CycleRoundDuration);
        obstacleSettingButton = EnsureSettingButton(ref obstacleSettingText, obstacleSettingButton, "ObstacleSettingButton", "ObstacleSettingText", new Vector2(120f, -208f), CycleObstacleDensity);
        treasureSettingButton = EnsureSettingButton(ref treasureSettingText, treasureSettingButton, "TreasureSettingButton", "TreasureSettingText", new Vector2(-120f, -262f), CycleTreasureDensity);
        extractionSettingButton = EnsureSettingButton(ref extractionSettingText, extractionSettingButton, "ExtractionSettingButton", "ExtractionSettingText", new Vector2(120f, -262f), CycleExtractionCount);
        boosterSettingButton = EnsureSettingButton(ref boosterSettingText, boosterSettingButton, "BoosterSettingButton", "BoosterSettingText", new Vector2(-120f, -316f), CycleBoosterSlowdown);
        ammoSettingButton = EnsureSettingButton(ref ammoSettingText, ammoSettingButton, "AmmoSettingButton", "AmmoSettingText", new Vector2(120f, -316f), CycleAmmoCount);
    }

    void RefreshPlayerStatusList()
    {
        EnsurePlayerStatusListExists();

        if (playerStatusListText == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            playerStatusListText.text = "Joining room...";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("PLAYERS");

        foreach (Player player in PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber))
        {
            bool ready = player.CustomProperties.TryGetValue("ready", out object readyValue) && readyValue is bool readyBool && readyBool;

            builder.Append(GetDisplayName(player));
            if (player == PhotonNetwork.LocalPlayer)
                builder.Append(" (YOU)");

            builder.Append("  -  ");
            builder.Append(ready ? "READY" : "NOT READY");
            builder.AppendLine();
        }

        playerStatusListText.text = builder.ToString().TrimEnd();
    }

    void EnsureDefaultRoomSettings()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable props = new Hashtable();
        bool changed = false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoundDurationKey))
        {
            props[RoundDurationKey] = 180f;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ObstacleDensityKey))
        {
            props[ObstacleDensityKey] = "medium";
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TreasureDensityKey))
        {
            props[TreasureDensityKey] = "medium";
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ExtractionCountKey))
        {
            props[ExtractionCountKey] = 3;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(BoosterSlowdownKey))
        {
            props[BoosterSlowdownKey] = 30;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(AmmoCountKey))
        {
            props[AmmoCountKey] = 10;
            changed = true;
        }

        if (changed)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    Button EnsureSettingButton(ref TMP_Text textField, Button existingButton, string buttonName, string textName, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
    {
        Button button = existingButton;

        if (button == null || !button.gameObject.scene.IsValid())
        {
            Transform existing = transform.Find(buttonName);
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
        }

        if (button == null)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            button = buttonObject.GetComponent<Button>();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(210f, 42f);
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.16f, 0.2f, 0.27f, 0.95f);
            image.type = Image.Type.Sliced;
        }

        if (textField == null || !textField.gameObject.scene.IsValid())
        {
            Transform existingText = button.transform.Find(textName);
            if (existingText != null)
            {
                textField = existingText.GetComponent<TMP_Text>();
            }
        }

        if (textField == null)
        {
            GameObject textObject = new GameObject(textName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(button.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            textField = textObject.GetComponent<TextMeshProUGUI>();
            textField.fontSize = 20f;
            textField.fontStyle = FontStyles.Bold;
            textField.alignment = TextAlignmentOptions.Center;
            textField.color = Color.white;
            textField.enableWordWrapping = false;
        }

        return button;
    }

    void CycleRoundDuration()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        float current = GetRoundDuration();
        int index = System.Array.FindIndex(RoundDurationOptions, option => Mathf.Abs(option - current) < 0.01f);
        if (index < 0)
            index = 0;

        int nextIndex = (index + 1) % RoundDurationOptions.Length;

        Hashtable props = new Hashtable();
        props[RoundDurationKey] = RoundDurationOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleObstacleDensity()
    {
        CycleDensitySetting(ObstacleDensityKey, GetObstacleDensity());
    }

    void CycleTreasureDensity()
    {
        CycleDensitySetting(TreasureDensityKey, GetTreasureDensity());
    }

    void CycleExtractionCount()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int current = GetExtractionCount();
        int index = System.Array.IndexOf(ExtractionCountOptions, current);
        if (index < 0)
            index = 2;

        int nextIndex = (index + 1) % ExtractionCountOptions.Length;

        Hashtable props = new Hashtable();
        props[ExtractionCountKey] = ExtractionCountOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleBoosterSlowdown()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int current = GetBoosterSlowdownPercent();
        int index = System.Array.IndexOf(BoosterSlowdownOptions, current);
        if (index < 0)
            index = 0;

        int nextIndex = (index + 1) % BoosterSlowdownOptions.Length;

        Hashtable props = new Hashtable();
        props[BoosterSlowdownKey] = BoosterSlowdownOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleAmmoCount()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int current = GetAmmoCount();
        int index = System.Array.IndexOf(AmmoCountOptions, current);
        if (index < 0)
            index = 1;

        int nextIndex = (index + 1) % AmmoCountOptions.Length;

        Hashtable props = new Hashtable();
        props[AmmoCountKey] = AmmoCountOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleDensitySetting(string key, string current)
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int index = System.Array.IndexOf(DensityOptions, current);
        if (index < 0)
            index = 0;

        int nextIndex = (index + 1) % DensityOptions.Length;

        Hashtable props = new Hashtable();
        props[key] = DensityOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void RefreshHostSettingsUi()
    {
        EnsureHostSettingsUiExists();

        bool isHost = PhotonNetwork.IsMasterClient;

        if (roundSettingText != null)
            roundSettingText.text = "ROUND: " + FormatRoundDuration(GetRoundDuration());

        if (obstacleSettingText != null)
            obstacleSettingText.text = "OBSTACLES: " + FormatDensity(GetObstacleDensity());

        if (treasureSettingText != null)
            treasureSettingText.text = "TREASURES: " + FormatDensity(GetTreasureDensity());

        if (extractionSettingText != null)
            extractionSettingText.text = "ZONES: " + GetExtractionCount();

        if (boosterSettingText != null)
            boosterSettingText.text = "SLOW: " + GetBoosterSlowdownPercent() + "%";

        if (ammoSettingText != null)
            ammoSettingText.text = "AMMO: " + GetAmmoCount();

        SetSettingButtonState(roundSettingButton, isHost);
        SetSettingButtonState(obstacleSettingButton, isHost);
        SetSettingButtonState(treasureSettingButton, isHost);
        SetSettingButtonState(extractionSettingButton, isHost);
        SetSettingButtonState(boosterSettingButton, isHost);
        SetSettingButtonState(ammoSettingButton, isHost);
    }

    void SetSettingButtonState(Button button, bool interactable)
    {
        if (button == null)
            return;

        button.interactable = interactable;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = interactable
                ? new Color(0.16f, 0.2f, 0.27f, 0.95f)
                : new Color(0.12f, 0.14f, 0.18f, 0.72f);
        }
    }

    float GetRoundDuration()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoundDurationKey, out object value))
        {
            if (value is float floatValue)
                return floatValue;
            if (value is int intValue)
                return intValue;
            if (value is double doubleValue)
                return (float)doubleValue;
        }

        return 180f;
    }

    string GetObstacleDensity()
    {
        return GetDensitySetting(ObstacleDensityKey);
    }

    string GetTreasureDensity()
    {
        return GetDensitySetting(TreasureDensityKey);
    }

    int GetExtractionCount()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ExtractionCountKey, out object value))
        {
            if (value is int intValue)
                return Mathf.Clamp(intValue, 1, 4);

            if (value is float floatValue)
                return Mathf.Clamp(Mathf.RoundToInt(floatValue), 1, 4);
        }

        return 3;
    }

    int GetBoosterSlowdownPercent()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BoosterSlowdownKey, out object value))
        {
            if (value is int intValue)
                return Mathf.Clamp(intValue, 30, 100);

            if (value is float floatValue)
                return Mathf.Clamp(Mathf.RoundToInt(floatValue), 30, 100);
        }

        return 30;
    }

    int GetAmmoCount()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(AmmoCountKey, out object value))
        {
            if (value is int intValue)
                return Mathf.Clamp(intValue, 5, 30);

            if (value is float floatValue)
                return Mathf.Clamp(Mathf.RoundToInt(floatValue), 5, 30);
        }

        return 10;
    }

    string GetDensitySetting(string key)
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value) &&
            value is string density)
        {
            return density;
        }

        return "medium";
    }

    string FormatRoundDuration(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.RoundToInt(seconds % 60f);
        return minutes + ":" + secs.ToString("00");
    }

    string FormatDensity(string density)
    {
        return density.ToUpperInvariant();
    }

    string GetDisplayName(Player player)
    {
        if (player == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(player.NickName))
            return player.NickName;

        return "Player " + player.ActorNumber;
    }
}
