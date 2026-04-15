using UnityEngine;
using Photon.Pun;
using TMPro;
using ExitGames.Client.Photon;

public class GameTimer : MonoBehaviourPun
{
    const string RoundDurationKey = "roundDuration";
    const string ObstacleLayoutKey = "obstacleLayout";
    const string ExtractionLayoutKey = "extractionLayout";
    const string MapSeedKey = "mapSeed";

    public float roundTime = 180f;

    private TMP_Text timerText;
    private double startTime;

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
            EndGame();
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

    void EndGame()
    {
        if (!IsGameStarted())
            return;

        Debug.Log("KONIEC GRY");

        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            PhotonView pv = p.photonView;
            if (pv != null)
            {
                pv.RPC("OnTimeUp", pv.Owner);
            }
        }

        EndScreenUI ui = FindFirstObjectByType<EndScreenUI>();
        if (ui != null)
        {
            Debug.Log("CALLING UI");
            ui.Show();
        }
        else
        {
            Debug.LogError("EndScreenUI NOT FOUND");
        }

        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        props[ObstacleLayoutKey] = string.Empty;
        props[ExtractionLayoutKey] = string.Empty;
        props[MapSeedKey] = -1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
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
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoundDurationKey, out object value))
        {
            if (value is float floatValue)
                return floatValue;
            if (value is int intValue)
                return intValue;
            if (value is double doubleValue)
                return (float)doubleValue;
        }

        return roundTime;
    }
}
