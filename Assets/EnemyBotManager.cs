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

            if (EnemyBot.IsBotInstantiationData(view.InstantiationData))
            {
                EnemyBot bot = view.GetComponent<EnemyBot>();
                if (bot == null)
                    bot = view.gameObject.AddComponent<EnemyBot>();

                bot.InitializeFromPhotonData();
                continue;
            }

            if (AstronautSurvivor.IsAstronautInstantiationData(view.InstantiationData))
            {
                AstronautSurvivor astronaut = view.GetComponent<AstronautSurvivor>();
                if (astronaut == null)
                    astronaut = view.gameObject.AddComponent<AstronautSurvivor>();

                astronaut.InitializeFromPhotonData();
            }
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

        if (!RoomSettings.AreEnemyBotsEnabled())
        {
            DestroyExistingBots();
            spawnedForCurrentRound = true;
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

    void DestroyExistingBots()
    {
        EnemyBot[] bots = FindObjectsByType<EnemyBot>(FindObjectsInactive.Exclude);
        for (int i = 0; i < bots.Length; i++)
        {
            EnemyBot bot = bots[i];
            if (bot == null || bot.GetComponent<PhotonView>() == null)
                continue;

            PhotonView view = bot.GetComponent<PhotonView>();
            if (view.IsMine)
                PhotonNetwork.Destroy(bot.gameObject);
        }
    }

    void SpawnEnemyBot()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        Vector2 spawn = GetSafeBotSpawnPosition(mapSize);
        GameObject botObject = PhotonNetwork.Instantiate("Player", spawn, Quaternion.identity, 0, new object[] { EnemyBot.BotInstantiationMarker });
        if (botObject != null)
        {
            EnemyBot bot = botObject.GetComponent<EnemyBot>();
            if (bot == null)
                bot = botObject.AddComponent<EnemyBot>();

            bot.InitializeFromPhotonData();
        }
    }

    Vector2 GetSafeBotSpawnPosition(Vector2 mapSize)
    {
        Vector2[] candidates =
        {
            new Vector2(-mapSize.x * 0.34f, mapSize.y * 0.34f),
            new Vector2(mapSize.x * 0.34f, mapSize.y * 0.34f),
            new Vector2(-mapSize.x * 0.34f, -mapSize.y * 0.34f),
            new Vector2(mapSize.x * 0.34f, -mapSize.y * 0.34f),
            new Vector2(0f, mapSize.y * 0.38f),
            new Vector2(0f, -mapSize.y * 0.38f)
        };

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        float bestScore = float.MinValue;
        Vector2 bestCandidate = candidates[0];

        for (int i = 0; i < candidates.Length; i++)
        {
            float nearestDistance = float.MaxValue;
            for (int j = 0; j < players.Length; j++)
            {
                PlayerHealth player = players[j];
                if (player == null || player.IsBotControlled || player.IsWreck)
                    continue;

                float distance = Vector2.Distance(candidates[i], player.transform.position);
                nearestDistance = Mathf.Min(nearestDistance, distance);
            }

            if (nearestDistance > bestScore)
            {
                bestScore = nearestDistance;
                bestCandidate = candidates[i];
            }
        }

        return bestCandidate;
    }
}
