using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPun
{
    const string ObstacleLayoutKey = "obstacleLayout";
    const string ExtractionLayoutKey = "extractionLayout";
    const string MapSeedKey = "mapSeed";

    public void StartGame()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Hashtable props = new Hashtable();
        props["gameStarted"] = true;
        props["startTime"] = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void RestartGame()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Debug.Log("RESTART GAME");
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
    }

    public void EndGame()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Debug.Log("GAME ENDED");

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        props[ObstacleLayoutKey] = string.Empty;
        props[ExtractionLayoutKey] = string.Empty;
        props[MapSeedKey] = -1;

        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
}
