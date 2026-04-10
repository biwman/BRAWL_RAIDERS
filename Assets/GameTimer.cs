using UnityEngine;
using Photon.Pun;
using TMPro;
using ExitGames.Client.Photon;

public class GameTimer : MonoBehaviourPun
{
    public float roundTime = 180f;

    private TMP_Text timerText;

    private double startTime;
    private bool timerStarted = false;

    void Start()
    {
        GameObject obj = GameObject.Find("TimerText");

        if (obj != null)
        {
            timerText = obj.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogError("❌ Nie znaleziono TimerText");
        }
    }

    void Update()
    {
        if (!IsGameStarted()) return;

        // 🔥 pobierz startTime z rooma
        if (!timerStarted)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("startTime", out object value))
            {
                startTime = (double)value;
                timerStarted = true;
            }
            else
            {
                return;
            }
        }

        // 🔥 oblicz czas na podstawie Photon Time
        double elapsed = PhotonNetwork.Time - startTime;
        float remaining = roundTime - (float)elapsed;

        // 🔥 clamp
        remaining = Mathf.Max(0f, remaining);

        // 🔥 update UI
        UpdateTimerUI(remaining);

        // 🔥 tylko master kończy grę
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
        // 🔥 zabezpieczenie żeby nie odpalało się wiele razy
        if (!IsGameStarted()) return;

        Debug.Log("⏰ KONIEC GRY");

        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        // 🔥 ustaw gameStarted = false w roomie
        Hashtable props = new Hashtable();
        props["gameStarted"] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable props = new Hashtable();

        props["gameStarted"] = true;
        props["startTime"] = PhotonNetwork.Time; // 🔥 KLUCZ

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
}