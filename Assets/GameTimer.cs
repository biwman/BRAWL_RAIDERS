using UnityEngine;
using Photon.Pun;
using TMPro;

public class GameTimer : MonoBehaviourPun
{
    public float roundTime = 180f; // 🔥 łatwo zmienisz (np. 60 = 1 min)

    private float currentTime;
    private bool isRunning = false;

    private TMP_Text timerText;

    private float syncTimer = 0f; // 🔥 do ograniczenia RPC

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

        currentTime = roundTime;
    }

    void Update()
    {
        if (!IsGameStarted()) return;

        // 🔥 tylko MASTER liczy czas
        if (PhotonNetwork.IsMasterClient)
        {
            if (!isRunning)
            {
                isRunning = true;
            }

            currentTime -= Time.deltaTime;
            syncTimer += Time.deltaTime;

            // 🔥 wysyłaj co 0.1 sekundy zamiast co klatkę
            if (syncTimer >= 0.1f)
            {
                photonView.RPC("UpdateTimer", RpcTarget.All, currentTime);
                syncTimer = 0f;
            }

            // 🔥 koniec gry tylko raz
            if (currentTime <= 0 && isRunning)
            {
                isRunning = false;
                photonView.RPC("EndGame", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void UpdateTimer(float time)
    {
        currentTime = time;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.Clamp(Mathf.FloorToInt(currentTime % 60), 0, 59);

            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    [PunRPC]
    void EndGame()
    {
        Debug.Log("⏰ KONIEC GRY");

        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;
    }

    bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        object value;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out value))
        {
            return (bool)value;
        }

        return false;
    }
}