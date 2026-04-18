using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    static readonly float[] RoundDurationOptions = { 60f, 90f, 120f, 150f, 180f, 210f, 240f };
    static readonly string[] DensityOptions = { "low", "medium", "high" };
    static readonly string[] MapSizeOptions = { "small", "medium", "large", "very_large", "super_large" };
    static readonly int[] ExtractionCountOptions = { 1, 2, 3, 4 };
    static readonly int[] BoosterSlowdownOptions = { 30, 40, 50, 60, 70, 80, 90, 100 };
    static readonly int[] AmmoCountOptions = { 5, 10, 15, 20, 25, 30 };
    static readonly int[] BoosterRecoveryDelayOptions = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    static readonly int[] MaxInputBoostPercentOptions = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    static readonly int[] LastShipTimerMultiplierOptions = { 1, 2, 3, 4, 5 };
    static readonly int[] WeightFactorOptions = { 2, 6, 12 };

    public Button readyButton;
    public TMP_Text readyText;
    public TMP_Text playerStatusListText;
    public TMP_Text roundSettingText;
    public TMP_Text mapSizeSettingText;
    public TMP_Text obstacleSettingText;
    public TMP_Text treasureSettingText;
    public TMP_Text nebulaSettingText;
    public TMP_Text extractionSettingText;
    public TMP_Text boosterSettingText;
    public TMP_Text ammoSettingText;
    public TMP_Text boosterDelaySettingText;
    public TMP_Text maxInputBoostSettingText;
    public TMP_Text shipDriftSettingText;
    public TMP_Text deathTimerSettingText;
    public TMP_Text movingObjectsSettingText;
    public TMP_Text obstacleWeightSettingText;
    public TMP_Text treasureWeightSettingText;
    public Button roundSettingButton;
    public Button mapSizeSettingButton;
    public Button obstacleSettingButton;
    public Button treasureSettingButton;
    public Button nebulaSettingButton;
    public Button extractionSettingButton;
    public Button boosterSettingButton;
    public Button ammoSettingButton;
    public Button boosterDelaySettingButton;
    public Button maxInputBoostSettingButton;
    public Button shipDriftSettingButton;
    public Button deathTimerSettingButton;
    public Button movingObjectsSettingButton;
    public Button obstacleWeightSettingButton;
    public Button treasureWeightSettingButton;

    bool isReady = false;
    bool hasRecordedCurrentRound = false;

    CanvasGroup EnsureCanvasGroup()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
        }

        return cg;
    }

    void Start()
    {
        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        EnsurePlayerStatusListExists();
        EnsureHostSettingsUiExists();

        if (PhotonNetwork.InRoom)
        {
            ShowLobby();
            EnsureDefaultRoomSettings();
        }
        else
        {
            CanvasGroup cg = EnsureCanvasGroup();
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

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
        hasRecordedCurrentRound = false;
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

        if (changedProps.ContainsKey(RoomSettings.RoundDurationKey) ||
            changedProps.ContainsKey(RoomSettings.MapSizeKey) ||
            changedProps.ContainsKey(RoomSettings.ObstacleDensityKey) ||
            changedProps.ContainsKey(RoomSettings.TreasureDensityKey) ||
            changedProps.ContainsKey(RoomSettings.NebulaDensityKey) ||
            changedProps.ContainsKey(RoomSettings.ExtractionCountKey) ||
            changedProps.ContainsKey(RoomSettings.BoosterSlowdownKey) ||
            changedProps.ContainsKey(RoomSettings.AmmoCountKey) ||
            changedProps.ContainsKey(RoomSettings.BoosterRecoveryDelayKey) ||
            changedProps.ContainsKey(RoomSettings.MaxInputBoostPercentKey) ||
            changedProps.ContainsKey(RoomSettings.ShipDriftEnabledKey) ||
            changedProps.ContainsKey(RoomSettings.LastShipTimerMultiplierKey) ||
            changedProps.ContainsKey(RoomSettings.MovingObjectsEnabledKey) ||
            changedProps.ContainsKey(RoomSettings.ObstacleWeightFactorKey) ||
            changedProps.ContainsKey(RoomSettings.TreasureWeightFactorKey))
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
        if (propertiesThatChanged.ContainsKey(RoomSettings.RoundDurationKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.MapSizeKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.ObstacleDensityKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.TreasureDensityKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.NebulaDensityKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.ExtractionCountKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.BoosterSlowdownKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.AmmoCountKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.BoosterRecoveryDelayKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.MaxInputBoostPercentKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.ShipDriftEnabledKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.LastShipTimerMultiplierKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.MovingObjectsEnabledKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.ObstacleWeightFactorKey) ||
            propertiesThatChanged.ContainsKey(RoomSettings.TreasureWeightFactorKey))
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
            if (!hasRecordedCurrentRound)
            {
                hasRecordedCurrentRound = true;
                _ = RecordStartedGameAsync();
            }
        }
        else
        {
            Debug.Log("GAME RESET TO LOBBY");
            PlayerMovement.gameStarted = false;
            PlayerShooting.gameStarted = false;
            hasRecordedCurrentRound = false;
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

        CanvasGroup cg = EnsureCanvasGroup();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    void ShowLobby()
    {
        CanvasGroup cg = EnsureCanvasGroup();
        cg.alpha = 1;
        cg.interactable = true;
        cg.blocksRaycasts = true;
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
        playerStatusListText.textWrappingMode = TextWrappingModes.NoWrap;
        playerStatusListText.color = new Color(0.94f, 0.97f, 1f, 1f);
        playerStatusListText.text = string.Empty;
    }

    void EnsureHostSettingsUiExists()
    {
        roundSettingButton = EnsureSettingButton(ref roundSettingText, roundSettingButton, "RoundSettingButton", "RoundSettingText", new Vector2(-205f, -208f), CycleRoundDuration);
        mapSizeSettingButton = EnsureSettingButton(ref mapSizeSettingText, mapSizeSettingButton, "MapSizeSettingButton", "MapSizeSettingText", new Vector2(205f, -208f), CycleMapSize);
        obstacleSettingButton = EnsureSettingButton(ref obstacleSettingText, obstacleSettingButton, "ObstacleSettingButton", "ObstacleSettingText", new Vector2(-205f, -262f), CycleObstacleDensity);
        treasureSettingButton = EnsureSettingButton(ref treasureSettingText, treasureSettingButton, "TreasureSettingButton", "TreasureSettingText", new Vector2(205f, -262f), CycleTreasureDensity);
        nebulaSettingButton = EnsureSettingButton(ref nebulaSettingText, nebulaSettingButton, "NebulaSettingButton", "NebulaSettingText", new Vector2(-205f, -316f), CycleNebulaDensity);
        extractionSettingButton = EnsureSettingButton(ref extractionSettingText, extractionSettingButton, "ExtractionSettingButton", "ExtractionSettingText", new Vector2(205f, -316f), CycleExtractionCount);
        boosterSettingButton = EnsureSettingButton(ref boosterSettingText, boosterSettingButton, "BoosterSettingButton", "BoosterSettingText", new Vector2(-205f, -370f), CycleBoosterSlowdown);
        ammoSettingButton = EnsureSettingButton(ref ammoSettingText, ammoSettingButton, "AmmoSettingButton", "AmmoSettingText", new Vector2(205f, -370f), CycleAmmoCount);
        boosterDelaySettingButton = EnsureSettingButton(ref boosterDelaySettingText, boosterDelaySettingButton, "BoosterDelaySettingButton", "BoosterDelaySettingText", new Vector2(-205f, -424f), CycleBoosterRecoveryDelay);
        maxInputBoostSettingButton = EnsureSettingButton(ref maxInputBoostSettingText, maxInputBoostSettingButton, "MaxInputBoostSettingButton", "MaxInputBoostSettingText", new Vector2(205f, -424f), CycleMaxInputBoostPercent);
        shipDriftSettingButton = EnsureSettingButton(ref shipDriftSettingText, shipDriftSettingButton, "ShipDriftSettingButton", "ShipDriftSettingText", new Vector2(-205f, -478f), CycleShipDriftEnabled);
        deathTimerSettingButton = EnsureSettingButton(ref deathTimerSettingText, deathTimerSettingButton, "DeathTimerSettingButton", "DeathTimerSettingText", new Vector2(205f, -478f), CycleLastShipTimerMultiplier);
        movingObjectsSettingButton = EnsureSettingButton(ref movingObjectsSettingText, movingObjectsSettingButton, "MovingObjectsSettingButton", "MovingObjectsSettingText", new Vector2(-205f, -532f), CycleMovingObjectsEnabled);
        obstacleWeightSettingButton = EnsureSettingButton(ref obstacleWeightSettingText, obstacleWeightSettingButton, "ObstacleWeightSettingButton", "ObstacleWeightSettingText", new Vector2(205f, -532f), CycleObstacleWeightFactor);
        treasureWeightSettingButton = EnsureSettingButton(ref treasureWeightSettingText, treasureWeightSettingButton, "TreasureWeightSettingButton", "TreasureWeightSettingText", new Vector2(0f, -586f), CycleTreasureWeightFactor);
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

    public void ForceRefreshUi()
    {
        RefreshPlayerStatusList();
        RefreshHostSettingsUi();
    }

    async Task RecordStartedGameAsync()
    {
        await PlayerProfileService.Instance.RecordGameStartedAsync();
    }

    void EnsureDefaultRoomSettings()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable props = new Hashtable();
        bool changed = false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.RoundDurationKey))
        {
            props[RoomSettings.RoundDurationKey] = RoomSettings.DefaultRoundDuration;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.ObstacleDensityKey))
        {
            props[RoomSettings.ObstacleDensityKey] = RoomSettings.DefaultObstacleDensity;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.MapSizeKey))
        {
            props[RoomSettings.MapSizeKey] = RoomSettings.DefaultMapSize;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.TreasureDensityKey))
        {
            props[RoomSettings.TreasureDensityKey] = "medium";
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.NebulaDensityKey))
        {
            props[RoomSettings.NebulaDensityKey] = "medium";
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.ExtractionCountKey))
        {
            props[RoomSettings.ExtractionCountKey] = RoomSettings.DefaultExtractionCount;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.BoosterSlowdownKey))
        {
            props[RoomSettings.BoosterSlowdownKey] = RoomSettings.DefaultBoosterSlowdownPercent;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.AmmoCountKey))
        {
            props[RoomSettings.AmmoCountKey] = RoomSettings.DefaultAmmoCount;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.BoosterRecoveryDelayKey))
        {
            props[RoomSettings.BoosterRecoveryDelayKey] = RoomSettings.DefaultBoosterRecoveryDelay;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.MaxInputBoostPercentKey))
        {
            props[RoomSettings.MaxInputBoostPercentKey] = RoomSettings.DefaultMaxInputBoostPercent;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.ShipDriftEnabledKey))
        {
            props[RoomSettings.ShipDriftEnabledKey] = RoomSettings.DefaultShipDriftEnabled;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.LastShipTimerMultiplierKey))
        {
            props[RoomSettings.LastShipTimerMultiplierKey] = RoomSettings.DefaultLastShipTimerMultiplier;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.MovingObjectsEnabledKey))
        {
            props[RoomSettings.MovingObjectsEnabledKey] = RoomSettings.DefaultMovingObjectsEnabled;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.ObstacleWeightFactorKey))
        {
            props[RoomSettings.ObstacleWeightFactorKey] = RoomSettings.DefaultObstacleWeightFactor;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomSettings.TreasureWeightFactorKey))
        {
            props[RoomSettings.TreasureWeightFactorKey] = RoomSettings.DefaultTreasureWeightFactor;
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
            rect.sizeDelta = new Vector2(280f, 42f);
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
            textField.fontSize = 17f;
            textField.fontStyle = FontStyles.Bold;
            textField.alignment = TextAlignmentOptions.Center;
            textField.color = Color.white;
            textField.textWrappingMode = TextWrappingModes.Normal;
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
        props[RoomSettings.RoundDurationKey] = RoundDurationOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleObstacleDensity()
    {
        CycleDensitySetting(RoomSettings.ObstacleDensityKey, GetObstacleDensity());
    }

    void CycleMapSize()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        string current = GetMapSize();
        int index = System.Array.IndexOf(MapSizeOptions, current);
        if (index < 0)
            index = 1;

        int nextIndex = (index + 1) % MapSizeOptions.Length;

        Hashtable props = new Hashtable();
        props[RoomSettings.MapSizeKey] = MapSizeOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleTreasureDensity()
    {
        CycleDensitySetting(RoomSettings.TreasureDensityKey, GetTreasureDensity());
    }

    void CycleNebulaDensity()
    {
        CycleDensitySetting(RoomSettings.NebulaDensityKey, GetNebulaDensity());
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
        props[RoomSettings.ExtractionCountKey] = ExtractionCountOptions[nextIndex];
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
        props[RoomSettings.BoosterSlowdownKey] = BoosterSlowdownOptions[nextIndex];
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
        props[RoomSettings.AmmoCountKey] = AmmoCountOptions[nextIndex];
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleBoosterRecoveryDelay()
    {
        CycleIntSetting(RoomSettings.BoosterRecoveryDelayKey, BoosterRecoveryDelayOptions, GetBoosterRecoveryDelay(), 0);
    }

    void CycleMaxInputBoostPercent()
    {
        CycleIntSetting(RoomSettings.MaxInputBoostPercentKey, MaxInputBoostPercentOptions, GetMaxInputBoostPercent(), RoomSettings.DefaultMaxInputBoostPercent);
    }

    void CycleShipDriftEnabled()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable props = new Hashtable();
        props[RoomSettings.ShipDriftEnabledKey] = !IsShipDriftEnabled();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleLastShipTimerMultiplier()
    {
        CycleIntSetting(RoomSettings.LastShipTimerMultiplierKey, LastShipTimerMultiplierOptions, GetLastShipTimerMultiplier(), RoomSettings.DefaultLastShipTimerMultiplier);
    }

    void CycleMovingObjectsEnabled()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable props = new Hashtable();
        props[RoomSettings.MovingObjectsEnabledKey] = !AreMovingObjectsEnabled();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        RefreshHostSettingsUi();
    }

    void CycleObstacleWeightFactor()
    {
        CycleIntSetting(RoomSettings.ObstacleWeightFactorKey, WeightFactorOptions, GetObstacleWeightFactor(), RoomSettings.DefaultObstacleWeightFactor);
    }

    void CycleTreasureWeightFactor()
    {
        CycleIntSetting(RoomSettings.TreasureWeightFactorKey, WeightFactorOptions, GetTreasureWeightFactor(), RoomSettings.DefaultTreasureWeightFactor);
    }

    void CycleIntSetting(string key, int[] options, int current, int fallbackIndexValue)
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int index = System.Array.IndexOf(options, current);
        if (index < 0)
        {
            index = System.Array.IndexOf(options, fallbackIndexValue);
            if (index < 0)
                index = 0;
        }

        int nextIndex = (index + 1) % options.Length;

        Hashtable props = new Hashtable();
        props[key] = options[nextIndex];
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
            roundSettingText.text = "ROUND TIME: " + FormatRoundDuration(GetRoundDuration());

        if (mapSizeSettingText != null)
            mapSizeSettingText.text = "MAP SIZE: " + FormatMapSize(GetMapSize());

        if (obstacleSettingText != null)
            obstacleSettingText.text = "OBSTACLES DENSITY: " + FormatDensity(GetObstacleDensity());

        if (treasureSettingText != null)
            treasureSettingText.text = "RESOURCES DENSITY: " + FormatDensity(GetTreasureDensity());

        if (nebulaSettingText != null)
            nebulaSettingText.text = "NEBULA DENSITY: " + FormatDensity(GetNebulaDensity());

        if (extractionSettingText != null)
            extractionSettingText.text = "EXTRACTION ZONES: " + GetExtractionCount();

        if (boosterSettingText != null)
            boosterSettingText.text = "EMPTY BOOSTER SLOWDOWN: " + GetBoosterSlowdownPercent() + "%";

        if (ammoSettingText != null)
            ammoSettingText.text = "AMMO: " + GetAmmoCount();

        if (boosterDelaySettingText != null)
            boosterDelaySettingText.text = "BOOST COOLDOWN: " + GetBoosterRecoveryDelay() + "s";

        if (maxInputBoostSettingText != null)
            maxInputBoostSettingText.text = "MAX BOOST BONUS: +" + GetMaxInputBoostPercent() + "%";

        if (shipDriftSettingText != null)
            shipDriftSettingText.text = "BRAKING DRIFT: " + (IsShipDriftEnabled() ? "ON" : "OFF");

        if (deathTimerSettingText != null)
            deathTimerSettingText.text = "LONE SHIP TIMER: X" + GetLastShipTimerMultiplier();

        if (movingObjectsSettingText != null)
            movingObjectsSettingText.text = "MOVING OBJECTS: " + (AreMovingObjectsEnabled() ? "ON" : "OFF");

        if (obstacleWeightSettingText != null)
            obstacleWeightSettingText.text = "OBSTACLE MASS: " + RoomSettings.GetMassLabel(GetObstacleWeightFactor());

        if (treasureWeightSettingText != null)
            treasureWeightSettingText.text = "TREASURE MASS: " + RoomSettings.GetMassLabel(GetTreasureWeightFactor());

        SetSettingButtonState(roundSettingButton, isHost);
        SetSettingButtonState(mapSizeSettingButton, isHost);
        SetSettingButtonState(obstacleSettingButton, isHost);
        SetSettingButtonState(treasureSettingButton, isHost);
        SetSettingButtonState(nebulaSettingButton, isHost);
        SetSettingButtonState(extractionSettingButton, isHost);
        SetSettingButtonState(boosterSettingButton, isHost);
        SetSettingButtonState(ammoSettingButton, isHost);
        SetSettingButtonState(boosterDelaySettingButton, isHost);
        SetSettingButtonState(maxInputBoostSettingButton, isHost);
        SetSettingButtonState(shipDriftSettingButton, isHost);
        SetSettingButtonState(deathTimerSettingButton, isHost);
        SetSettingButtonState(movingObjectsSettingButton, isHost);
        SetSettingButtonState(obstacleWeightSettingButton, isHost);
        SetSettingButtonState(treasureWeightSettingButton, isHost);
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
        return RoomSettings.GetRoundDuration();
    }

    string GetObstacleDensity()
    {
        return GetDensitySetting(RoomSettings.ObstacleDensityKey);
    }

    string GetMapSize()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomSettings.MapSizeKey, out object value) &&
            value is string mode)
        {
            return mode;
        }

        return RoomSettings.DefaultMapSize;
    }

    string GetTreasureDensity()
    {
        return GetDensitySetting(RoomSettings.TreasureDensityKey);
    }

    string GetNebulaDensity()
    {
        return GetDensitySetting(RoomSettings.NebulaDensityKey);
    }

    int GetExtractionCount()
    {
        return RoomSettings.GetExtractionCount();
    }

    int GetBoosterSlowdownPercent()
    {
        return RoomSettings.GetBoosterSlowdownPercent();
    }

    int GetAmmoCount()
    {
        return RoomSettings.GetAmmoCount();
    }

    int GetBoosterRecoveryDelay()
    {
        return RoomSettings.GetBoosterRecoveryDelay();
    }

    int GetMaxInputBoostPercent()
    {
        return RoomSettings.GetMaxInputBoostPercent();
    }

    bool IsShipDriftEnabled()
    {
        return RoomSettings.IsShipDriftEnabled();
    }

    int GetLastShipTimerMultiplier()
    {
        return RoomSettings.GetLastShipTimerMultiplier();
    }

    bool AreMovingObjectsEnabled()
    {
        return RoomSettings.AreMovingObjectsEnabled();
    }

    int GetObstacleWeightFactor()
    {
        return RoomSettings.GetObstacleWeightFactor();
    }

    int GetTreasureWeightFactor()
    {
        return RoomSettings.GetTreasureWeightFactor();
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

    string FormatMapSize(string mapSize)
    {
        switch (mapSize)
        {
            case "very_large":
                return "VERY LARGE";
            case "super_large":
                return "SUPER LARGE";
            default:
                return mapSize.ToUpperInvariant();
        }
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
