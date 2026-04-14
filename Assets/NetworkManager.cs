using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Awake()
    {
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

        Debug.Log("Connecting...");
        PhotonNetwork.GameVersion = Application.version;
        Debug.Log("Game Version: " + PhotonNetwork.GameVersion);

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");

        PhotonNetwork.AutomaticallySyncScene = true;

        Debug.Log("Trying to join random room...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found, creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");

        if (string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
        }

        RestoreRoomStateAfterSceneLoad();
    }

    void SpawnPlayer()
    {
        Debug.Log("Spawning player...");

        Vector3 spawnPos = new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }

    void RestoreRoomStateAfterSceneLoad()
    {
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

        if (PhotonNetwork.LocalPlayer.TagObject is GameObject taggedObject && taggedObject != null)
            return;

        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth player in players)
        {
            if (player.photonView != null && player.photonView.IsMine)
            {
                PhotonNetwork.LocalPlayer.TagObject = player.gameObject;
                return;
            }
        }

        SpawnPlayer();
    }

    void EnsureTreasureSpawnerExists()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (FindFirstObjectByType<TreasureSpawner>() != null)
            return;

        Debug.Log("Tworze TreasureSpawner");

        GameObject spawner = new GameObject("TreasureSpawner");
        spawner.AddComponent<TreasureSpawner>();
    }
}
