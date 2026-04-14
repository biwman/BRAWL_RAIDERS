using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPun
{
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
        // FIX: działa w single i multi
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Debug.Log("⏰ GAME ENDED");

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;

        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        
    }

    
}