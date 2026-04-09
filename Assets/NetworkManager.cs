using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
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
        Debug.Log("Joined Room ✔");

        SpawnPlayer();

        // 🔥 DODAJ TO
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Tworzę TreasureSpawner");

            GameObject spawner = new GameObject("TreasureSpawner");
            spawner.AddComponent<TreasureSpawner>();
        }
    }

    void SpawnPlayer()
    {
        Debug.Log("Spawning player...");

        Vector3 spawnPos = new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);
        PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);
    }
}