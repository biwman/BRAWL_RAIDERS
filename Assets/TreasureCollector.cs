using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TreasureCollector : MonoBehaviourPun
{
    public Button collectButton;
    public TMP_Text scoreText;

    public PlayerMovement movement;
    public PlayerShooting shooting;

    private Treasure currentTreasure;
    private bool isCollecting = false;

    public float collectTime = 3f;
    public int totalScore = 0;

    void Start()
    {
        if (!photonView.IsMine) return;

        // 🎯 SCORE UI
        if (scoreText == null)
            scoreText = FindObjectOfType<TMP_Text>();

        if (scoreText != null)
            scoreText.text = "Score: 0";

        // 🔘 BUTTON
        if (collectButton == null)
            collectButton = FindObjectOfType<Button>();

        if (collectButton != null)
        {
            EventTrigger trigger = collectButton.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = collectButton.gameObject.AddComponent<EventTrigger>();

            trigger.triggers = new List<EventTrigger.Entry>();

            // 🟢 HOLD START
            EventTrigger.Entry down = new EventTrigger.Entry();
            down.eventID = EventTriggerType.PointerDown;
            down.callback.AddListener((data) => { StartHolding(); });
            trigger.triggers.Add(down);

            // 🔴 HOLD STOP
            EventTrigger.Entry up = new EventTrigger.Entry();
            up.eventID = EventTriggerType.PointerUp;
            up.callback.AddListener((data) => { StopHolding(); });
            trigger.triggers.Add(up);
        }
    }

    // 🔥 wejście w skarb
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!photonView.IsMine) return;

        Debug.Log("Trigger z: " + other.name);

        // 🔍 pokaż wszystkie komponenty na tym obiekcie
        var components = other.GetComponents<Component>();
        foreach (var c in components)
        {
            Debug.Log("Component on collider: " + c.GetType().Name);
        }

        Treasure t = other.GetComponent<Treasure>();

        if (t == null)
        {
            Debug.Log("❌ GetComponent NIE znalazł Treasure");
        }
        else
        {
            Debug.Log("✅ ZNALAZŁEM Treasure (direct)");

            currentTreasure = t;
            t.Highlight();
        }

        Treasure tParent = other.GetComponentInParent<Treasure>();

        if (tParent == null)
        {
            Debug.Log("❌ GetComponentInParent NIE znalazł Treasure");
        }
        else
        {
            Debug.Log("✅ ZNALAZŁEM Treasure (parent)");

            currentTreasure = tParent;
            tParent.Highlight();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!photonView.IsMine) return;

        Treasure t = other.GetComponent<Treasure>();
        if (t != null)
        {
            t.Unhighlight();
            currentTreasure = null;
        }
    }

    public void StartHolding()
    {
        if (!photonView.IsMine) return;

        Debug.Log("START HOLD");
        Debug.Log("currentTreasure: " + currentTreasure);

        if (currentTreasure != null && !isCollecting)
        {
            isCollecting = true;
            StartCoroutine(CollectRoutine());
        }
    }

    public void StopHolding()
    {
        if (!photonView.IsMine) return;

        Debug.Log("STOP HOLD");

        isCollecting = false;
        

        if (movement != null) movement.enabled = true;
        if (shooting != null) shooting.enabled = true;
    }

    IEnumerator CollectRoutine()
    {
        Debug.Log("CollectRoutine START");

        if (currentTreasure == null)
        {
            isCollecting = false;
            yield break;
        }

        Treasure treasureToCollect = currentTreasure;

        // 🔥 blokada spamu (drugi gracz / drugi klik)
        if (treasureToCollect.isBeingCollected)
        {
            isCollecting = false;
            yield break;
        }

        treasureToCollect.isBeingCollected = true;

        // 🔒 blokujemy ruch i strzał
        if (movement != null) movement.enabled = false;
        if (shooting != null) shooting.enabled = false;

        float timer = 0f;

        while (timer < collectTime)
        {
            // ❌ przerwanie jeśli puścisz przycisk
            if (!isCollecting)
            {
                treasureToCollect.isBeingCollected = false;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("Collect DONE");

        // 💰 dodanie punktów
        totalScore += treasureToCollect.value;

        if (scoreText != null)
            scoreText.text = "Score: " + totalScore;

        // 🌐 MULTIPLAYER DESTROY
        PhotonView treasureView = treasureToCollect.GetComponent<PhotonView>();

        if (treasureView != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(treasureView.gameObject);
            }
            else
            {
                photonView.RPC("RequestDestroyTreasure", RpcTarget.MasterClient, treasureView.ViewID);
            }
        }

        // 🔓 reset stanu
        treasureToCollect.isBeingCollected = false;
        isCollecting = false;

        if (movement != null) movement.enabled = true;
        if (shooting != null) shooting.enabled = true;
    }

    // 🔥 MASTER usuwa obiekt
    [PunRPC]
    void RequestDestroyTreasure(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewID);

        if (pv != null)
        {
            PhotonNetwork.Destroy(pv.gameObject);
        }
    }
}