using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Linq;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    static NetworkManager instance;
    public static bool SessionRequested { get; private set; }

    public static void RequestSessionStart()
    {
        SessionRequested = true;
        if (instance == null)
        {
            instance = FindAnyObjectByType<NetworkManager>();
        }

        if (instance != null)
        {
            instance.TryConnectOrJoin();
        }
        else
        {
            Debug.LogWarning("NetworkManager instance not found when requesting session start.");
        }
    }

    void Awake()
    {
        instance = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Already in room, restoring scene state...");
            RestoreRoomStateAfterSceneLoad();
            return;
        }

        PhotonNetwork.GameVersion = Application.version;
        Debug.Log("Game Version: " + PhotonNetwork.GameVersion);

        if (SessionRequested)
        {
            TryConnectOrJoin();
        }
    }

    void TryConnectOrJoin()
    {
        if (PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Trying to join random room...");
            PhotonNetwork.JoinRandomRoom();
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Connecting...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");

        PhotonNetwork.AutomaticallySyncScene = true;
        PlayerProfileService.Instance.ApplyProfileToPhoton();

        if (SessionRequested)
        {
            Debug.Log("Trying to join random room...");
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found, creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        SessionRequested = false;

        PlayerProfileService.Instance.ApplyProfileToPhoton();

        if (string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;

        Hashtable props = new Hashtable();
        props[RoomSettings.ScoreKey] = 0;
        props[RoomSettings.ShipSkinKey] = PlayerProfileService.Instance.CurrentProfile.ShipSkinIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        RestoreRoomStateAfterSceneLoad();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!propertiesThatChanged.ContainsKey("gameStarted"))
            return;

        EnsureDroppedCargoManagerExists();
        EnsureEnemyBotManagerExists();
        EnsureNebulaSpawnerExists();
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureTreasureSpawnerExists();
        }
    }

    void SpawnPlayer()
    {
        Debug.Log("Spawning player...");

        Vector3 spawnPos = GetSpawnPosition();
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }

    public void RestoreRoomStateAfterSceneLoad()
    {
        PlayerProfileService.Instance.ApplyProfileToPhoton();
        EnsureDroppedCargoManagerExists();
        EnsureEnemyBotManagerExists();

        if (PhotonNetwork.LocalPlayer != null && string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
        }

        SpawnPlayerIfNeeded();
        EnsureTreasureSpawnerExists();
    }

    void SpawnPlayerIfNeeded()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.LocalPlayer.TagObject is GameObject taggedObject)
        {
            if (taggedObject != null && taggedObject.scene.IsValid())
                return;

            PhotonNetwork.LocalPlayer.TagObject = null;
        }

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth player in players)
        {
            if (player != null &&
                !player.IsBotControlled &&
                player.photonView != null &&
                player.photonView.IsMine)
            {
                PhotonNetwork.LocalPlayer.TagObject = player.gameObject;
                return;
            }
        }

        SpawnPlayer();
    }

    Vector3 GetSpawnPosition()
    {
        Vector2 mapSize = RoomSettings.GetMapDimensions();
        float spawnX = Mathf.Max(3f, mapSize.x * 0.34f);
        float spawnY = Mathf.Max(3f, mapSize.y * 0.34f);
        Vector2[] spawnCorners =
        {
            new Vector2(-spawnX, -spawnY),
            new Vector2(spawnX, spawnY),
            new Vector2(-spawnX, spawnY),
            new Vector2(spawnX, -spawnY)
        };

        Player[] playerOrder = PhotonNetwork.PlayerList
            .OrderBy(player => player.ActorNumber)
            .ToArray();
        int actorIndex = System.Array.FindIndex(playerOrder, player => player == PhotonNetwork.LocalPlayer);
        if (actorIndex < 0)
            actorIndex = 0;

        int rotationOffset = 0;

        if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrWhiteSpace(PhotonNetwork.CurrentRoom.Name))
        {
            rotationOffset = Mathf.Abs(PhotonNetwork.CurrentRoom.Name.GetHashCode()) % spawnCorners.Length;
        }

        Vector2 baseCorner = spawnCorners[(actorIndex + rotationOffset) % spawnCorners.Length];
        int jitterSeed = actorIndex * 97 + rotationOffset * 31 + 11;
        float maxJitterX = Mathf.Min(2.2f, mapSize.x * 0.06f);
        float maxJitterY = Mathf.Min(2.2f, mapSize.y * 0.06f);
        float jitterX = Mathf.Lerp(-maxJitterX, maxJitterX, Mathf.PerlinNoise(jitterSeed, 0.17f));
        float jitterY = Mathf.Lerp(-maxJitterY, maxJitterY, Mathf.PerlinNoise(0.31f, jitterSeed));

        return new Vector3(baseCorner.x + jitterX, baseCorner.y + jitterY, 0f);
    }

    void EnsureTreasureSpawnerExists()
    {
        EnsureNebulaSpawnerExists();

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (FindFirstObjectByType<TreasureSpawner>() != null)
        {
            return;
        }

        Debug.Log("Tworze TreasureSpawner");

        GameObject spawner = new GameObject("TreasureSpawner");
        spawner.AddComponent<TreasureSpawner>();
    }

    void EnsureNebulaSpawnerExists()
    {
        if (FindFirstObjectByType<NebulaSpawner>() != null)
            return;

        GameObject spawner = new GameObject("NebulaSpawner");
        spawner.AddComponent<NebulaSpawner>();
    }

    void EnsureDroppedCargoManagerExists()
    {
        DroppedCargoManager.EnsureExists();
    }

    void EnsureEnemyBotManagerExists()
    {
        EnemyBotManager.EnsureExists();
    }
}
