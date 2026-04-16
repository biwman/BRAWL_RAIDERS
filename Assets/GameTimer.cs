using UnityEngine;
using Photon.Pun;
using TMPro;
using ExitGames.Client.Photon;

public class GameTimer : MonoBehaviourPun
{
    const string ObstacleLayoutKey = "obstacleLayout";
    const string ExtractionLayoutKey = "extractionLayout";
    const string NebulaLayoutKey = "nebulaLayout";
    const string MapSeedKey = "mapSeed";

    public float roundTime = 180f;

    private TMP_Text timerText;
    private double startTime;
    bool isEndingRound;

    void Start()
    {
        GameObject obj = GameObject.Find("TimerText");

        if (obj != null)
        {
            timerText = obj.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogError("Nie znaleziono TimerText");
        }
    }

    void Update()
    {
        if (!IsGameStarted())
            return;

        roundTime = GetConfiguredRoundTime();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("startTime", out object value))
        {
            startTime = (double)value;
        }
        else
        {
            return;
        }

        double elapsed = PhotonNetwork.Time - startTime;
        float remaining = roundTime - (float)elapsed;
        remaining = Mathf.Max(0f, remaining);

        UpdateTimerUI(remaining);

        if (remaining <= 0f && PhotonNetwork.IsMasterClient)
        {
            StartRoundTimeout();
        }
    }

    void UpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    void StartRoundTimeout()
    {
        if (!IsGameStarted() || isEndingRound)
            return;

        isEndingRound = true;
        StartCoroutine(EndGameAfterTimeUpSync());
    }

    System.Collections.IEnumerator EndGameAfterTimeUpSync()
    {
        Debug.Log("KONIEC GRY");

        var players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            PhotonView pv = p.photonView;
            if (pv != null)
            {
                pv.RPC("OnTimeUp", pv.Owner);
            }
        }

        yield return new WaitForSeconds(0.35f);

        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        props[ObstacleLayoutKey] = string.Empty;
        props[ExtractionLayoutKey] = string.Empty;
        props[NebulaLayoutKey] = string.Empty;
        props[MapSeedKey] = -1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        isEndingRound = false;
    }

    public static void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Hashtable props = new Hashtable();
        props["gameStarted"] = true;
        props["startTime"] = PhotonNetwork.Time;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value))
        {
            return (bool)value;
        }

        return false;
    }

    public void ReduceTime(float amount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("startTime", out object value))
        {
            double currentStart = (double)value;
            double newStart = currentStart + amount;

            Hashtable props = new Hashtable();
            props["startTime"] = newStart;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log("Timer skrocony o " + amount + " sekund");
        }
    }

    float GetConfiguredRoundTime()
    {
        return RoomSettings.GetRoundDuration();
    }
}
