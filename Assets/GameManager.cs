using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    const string ObstacleLayoutKey = "obstacleLayout";
    const string ExtractionLayoutKey = "extractionLayout";
    const string NebulaLayoutKey = "nebulaLayout";
    const string MapSeedKey = "mapSeed";
    bool restartInProgress;
    bool leavingRoomToProfile;

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
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            if (IsRoundStopped())
            {
                Debug.Log("LOCAL READY AFTER HOST RESTART");
                NetworkManager networkManager = FindAnyObjectByType<NetworkManager>();
                if (networkManager != null)
                {
                    networkManager.RestoreRoomStateAfterSceneLoad();
                }
            }

            return;
        }

        if (restartInProgress) return;

        Debug.Log("RESTART GAME");
        StartCoroutine(RestartAfterCleanup());
    }

    public void EndGame()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        Debug.Log("GAME ENDED");

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        props[ObstacleLayoutKey] = string.Empty;
        props[ExtractionLayoutKey] = string.Empty;
        props[NebulaLayoutKey] = string.Empty;
        props[MapSeedKey] = -1;

        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public void LeaveRoomToProfile()
    {
        if (leavingRoomToProfile)
            return;

        leavingRoomToProfile = true;

        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.TagObject = null;
        }

        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!leavingRoomToProfile)
            return;

        leavingRoomToProfile = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    System.Collections.IEnumerator RestartAfterCleanup()
    {
        restartInProgress = true;

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        props[ObstacleLayoutKey] = string.Empty;
        props[ExtractionLayoutKey] = string.Empty;
        props[NebulaLayoutKey] = string.Empty;
        props[MapSeedKey] = -1;

        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.DestroyAll();
        }

        yield return null;
        yield return new WaitForSeconds(0.2f);

        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
        restartInProgress = false;
    }

    bool IsRoundStopped()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return true;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
            value is bool started)
        {
            return !started;
        }

        return true;
    }
}
