using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ExtractionZone : MonoBehaviourPun
{
    public float activationTime = 3f;
    public float activeDuration = 15f;

    private bool isActive = false;
    private bool isBeingUsed = false;
    private bool isEvacuating = false;
    private bool messageShowing = false;

    private SpriteRenderer sr;
    private Coroutine blinkRoutine;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        SetColor(Color.red);
    }

    void SetColor(Color c)
    {
        if (sr != null)
            sr.color = c;
    }

    // FIX: działa w single i multi
    public void TryUse(PhotonView playerView)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        if (!isBeingUsed)
        {
            StartCoroutine(UseRoutine(playerView));
        }
    }

    IEnumerator UseRoutine(PhotonView playerView)
    {
        isBeingUsed = true;
        bool startingActivation = !isActive;

        if (startingActivation)
        {
            photonView.RPC(nameof(StartAlarmLoop), RpcTarget.All);
        }

        yield return null;

        if (!isActive)
        {
            isActive = true; // lokalnie od razu

            photonView.RPC("ActivateZone", RpcTarget.All);
            photonView.RPC("ShowExtractionMessage", RpcTarget.All);
        }
        else
        {
            EvacuatePlayers();
        }

        if (startingActivation)
        {
            photonView.RPC(nameof(StopAlarmLoop), RpcTarget.All);
        }

        isBeingUsed = false;
    }

    [PunRPC]
    void ActivateZone()
    {
        isActive = true;
        isEvacuating = false;

        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(Blink());

        StartCoroutine(ActiveTimer());
    }

    IEnumerator ActiveTimer()
    {
        float timer = 0f;

        while (timer < activeDuration)
        {
            if (!isActive) yield break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            EvacuatePlayers();
        }
    }

    IEnumerator Blink()
    {
        while (isActive)
        {
            SetColor(Color.green);
            yield return new WaitForSeconds(0.3f);

            SetColor(Color.white);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 🔥 NAJWAŻNIEJSZA FUNKCJA
    void EvacuatePlayers()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
        if (isEvacuating) return;

        isEvacuating = true;

        Debug.Log("EVACUATION!");

        PlayerHealth[] playersBeforeEvacuation = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        Collider2D[] hits = GetPlayersInsideZone();

        HashSet<int> processedPlayers = new HashSet<int>();

        foreach (var hit in hits)
        {
            PlayerHealth p = hit.GetComponentInParent<PlayerHealth>();

            if (p != null && !p.IsWreck && !p.IsBotControlled)
            {
                PhotonView pv = p.photonView;

                if (processedPlayers.Contains(pv.ViewID))
                    continue;

                processedPlayers.Add(pv.ViewID);

                Debug.Log("Evacuating: " + pv.Owner.NickName);
                int finalScore = RoundResultsTracker.GetKnownScore(pv.Owner, pv.gameObject) + 5;
                string outcome = p.IsAstronautControlled ? "evacuated" : "extracted";
                RoundResultsTracker.RecordOutcome(pv.Owner, finalScore, outcome);

                // punkt tylko dla właściciela
                pv.RPC("OnEvacuated", pv.Owner, 5);

                // ZAMIANA (ważne!)
                if (pv.IsMine || !PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Destroy(pv.gameObject);
                }
                else
                {
                    pv.RPC("DestroySelf", pv.Owner);
                }
            }
        }

        photonView.RPC("ResetZone", RpcTarget.All);

        // skrócenie czasu
        bool anyPlayerEvacuated = processedPlayers.Count > 0;

        // KLUCZOWE — KONIEC GRY
        if (anyPlayerEvacuated)
        {
            bool anyPlayerRemaining = false;

            for (int i = 0; i < playersBeforeEvacuation.Length; i++)
            {
                PlayerHealth player = playersBeforeEvacuation[i];
                if (player == null || player.IsWreck || player.photonView == null)
                    continue;

                if (!processedPlayers.Contains(player.photonView.ViewID))
                {
                    anyPlayerRemaining = true;
                    break;
                }
            }

            if (!anyPlayerRemaining)
            {
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.EndGame("evacuation");
                }
            }
        }
    }

    Collider2D[] GetPlayersInsideZone()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null)
        {
            return Physics2D.OverlapCircleAll(transform.position, 1.0f);
        }

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = false,
            useTriggers = true
        };

        Collider2D[] buffer = new Collider2D[32];
        int count = zoneCollider.Overlap(filter, buffer);
        if (count <= 0)
        {
            return System.Array.Empty<Collider2D>();
        }

        Collider2D[] hits = new Collider2D[count];
        System.Array.Copy(buffer, hits, count);
        return hits;
    }

    [PunRPC]
    void ResetZone()
    {
        isActive = false;
        isBeingUsed = false;
        isEvacuating = false;
        StopAlarmLoop();

        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        SetColor(Color.red);
    }

    [PunRPC]
    void ShowExtractionMessage()
    {
        if (messageShowing) return;

        GameObject obj = FindExtractionMessage();

        if (obj != null)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetAsLastSibling();
            }

            TMP_Text text = obj.GetComponent<TMP_Text>();
            if (text == null)
                text = obj.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                text.text = "Extraction Zone Activated";
                text.fontStyle = FontStyles.Bold;
            }

            messageShowing = true;
            obj.SetActive(true);
            StartCoroutine(HideMessage(obj));
        }
    }

    IEnumerator HideMessage(GameObject obj)
    {
        yield return new WaitForSeconds(3f);

        obj.SetActive(false);
        messageShowing = false;
    }

    GameObject FindExtractionMessage()
    {
        var allTexts = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in allTexts)
        {
            if (go.name == "ExtractionMessage")
            {
                return go;
            }
        }

        return null;
    }

    [PunRPC]
    void StartAlarmLoop()
    {
        AudioManager.Instance.StartAlarmLoop();
    }

    [PunRPC]
    void StopAlarmLoop()
    {
        AudioManager.Instance.StopAlarmLoop();
    }
}
