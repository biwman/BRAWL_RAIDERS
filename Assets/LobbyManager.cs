using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public Button readyButton;
    public TMP_Text readyText;

    private bool isReady = false;

    void Start()
    {
        // 🔥 reset gry przy wejściu do lobby
        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReady);
        }

        SetReady(false);
    }

    void ToggleReady()
    {
        isReady = !isReady;
        SetReady(isReady);

        // 🔥 opóźnienie 0.1s (ważne)
        Invoke(nameof(CheckAllReady), 0.1f);
    }

    void SetReady(bool ready)
    {
        Hashtable props = new Hashtable();
        props["ready"] = ready;

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (readyText != null)
        {
            readyText.text = ready ? "READY ✅" : "NOT READY ❌";
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("ready"))
        {
            CheckAllReady();
        }
    }

    void CheckAllReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object readyValue;

            if (!p.CustomProperties.TryGetValue("ready", out readyValue))
            {
                Debug.Log("Brak ready u gracza");
                return;
            }

            if (!(bool)readyValue)
            {
                Debug.Log("Gracz nie gotowy");
                return;
            }
        }

        Debug.Log("✅ WSZYSCY GOTOWI");

        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        Debug.Log("🚀 START GRY");

        PlayerMovement.gameStarted = true;
        PlayerShooting.gameStarted = true;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        gameObject.SetActive(false);
    }
}