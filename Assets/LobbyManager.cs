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
        // reset stanu gry lokalnie
        PlayerMovement.gameStarted = false;
        PlayerShooting.gameStarted = false;

        // przypięcie przycisku
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReady);
        }

        // ustaw ready = false tylko jeśli brak
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("ready"))
        {
            SetReady(false);
        }

        // jeśli gra już wystartowała (ważne dla drugiego gracza)
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value))
        {
            if ((bool)value)
            {
                Debug.Log("🎮 GAME ALREADY STARTED (Start)");

                PlayerMovement.gameStarted = true;
                PlayerShooting.gameStarted = true;

                CanvasGroup cg = GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }


    }

    void ToggleReady()
    {
        isReady = !isReady;
        SetReady(isReady);
    }

    void SetReady(bool ready)
    {
        Hashtable props = new Hashtable();
        props["ready"] = ready;

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (readyText != null)
        {
            readyText.text = ready ? "READY " : "NOT READY ";
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
        Debug.Log("SPRAWDZAM READY");

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object readyValue;

            if (!p.CustomProperties.TryGetValue("ready", out readyValue))
            {
                return;
            }

            if (!(bool)readyValue)
            {
                return;
            }
        }

        Debug.Log("✅ WSZYSCY GOTOWI");

        // 🔥 tylko master ustawia start gry
        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        Debug.Log("START GRY");

        GameTimer.StartGame(); // 🔥 NAJWAŻNIEJSZE
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        
        if (propertiesThatChanged.ContainsKey("gameStarted"))
        {
            Debug.Log("GAME STARTED (ROOM PROP)");

            PlayerMovement.gameStarted = true;
            PlayerShooting.gameStarted = true;

            CanvasGroup cg = GetComponent<CanvasGroup>();

            if (cg != null)
            {
                cg.alpha = 0;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}