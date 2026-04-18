using Photon.Pun;
using UnityEngine;

public class EnemyBotManager : MonoBehaviour
{
    const float ScanInterval = 0.2f;

    static EnemyBotManager instance;

    double lastHandledStartTime = double.MinValue;
    bool spawnedForCurrentRound;
    float nextScanTime;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("EnemyBotManager");
        instance = root.AddComponent<EnemyBotManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + ScanInterval;
        EnsureRuntimeComponents();
        HandleSpawnLifecycle();
    }

    void EnsureRuntimeComponents()
    {
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsInactive.Exclude);
        for (int i = 0; i < views.Length; i++)
        {
            PhotonView view = views[i];
            if (view == null || view.gameObject == null || !view.gameObject.name.StartsWith("Player"))
                continue;

            if (!EnemyBot.IsBotInstantiationData(view.InstantiationData))
                continue;

            EnemyBot bot = view.GetComponent<EnemyBot>();
            if (bot == null)
                bot = view.gameObject.AddComponent<EnemyBot>();

            bot.InitializeFromPhotonData();
        }
    }

    void HandleSpawnLifecycle()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        bool gameStarted = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object startedValue) &&
                           startedValue is bool started &&
                           started;

        if (!gameStarted)
        {
            spawnedForCurrentRound = false;
            lastHandledStartTime = double.MinValue;
            return;
        }

        double currentStartTime = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("startTime", out object startValue) && startValue is double roomStart
            ? roomStart
            : 0d;

        if (currentStartTime != lastHandledStartTime)
        {
            lastHandledStartTime = currentStartTime;
            spawnedForCurrentRound = false;
        }

        if (spawnedForCurrentRound)
            return;

        if (FindAnyObjectByType<EnemyBot>() != null)
        {
            spawnedForCurrentRound = true;
            return;
        }

        SpawnEnemyBot();
        spawnedForCurrentRound = true;
    }

    void SpawnEnemyBot()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        Vector2 spawn = new Vector2(0f, mapSize.y * 0.18f);
        GameObject botObject = PhotonNetwork.Instantiate("Player", spawn, Quaternion.identity, 0, new object[] { EnemyBot.BotInstantiationMarker });
        if (botObject != null)
        {
            EnemyBot bot = botObject.GetComponent<EnemyBot>();
            if (bot == null)
                bot = botObject.AddComponent<EnemyBot>();

            bot.InitializeFromPhotonData();
        }
    }
}
