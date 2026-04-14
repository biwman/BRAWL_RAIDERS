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

        float timer = 0f;

        while (timer < activationTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

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

        PlayerHealth[] playersBeforeEvacuation = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.0f);

        HashSet<int> processedPlayers = new HashSet<int>();

        foreach (var hit in hits)
        {
            PlayerHealth p = hit.GetComponentInParent<PlayerHealth>();

            if (p != null)
            {
                PhotonView pv = p.photonView;

                if (processedPlayers.Contains(pv.ViewID))
                    continue;

                processedPlayers.Add(pv.ViewID);

                Debug.Log("Evacuating: " + pv.Owner.NickName);

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
        GameTimer timer = FindFirstObjectByType<GameTimer>();
        if (timer != null && anyPlayerEvacuated)
        {
            timer.ReduceTime(30f);
        }

        // KLUCZOWE — KONIEC GRY
        GameManager gm = FindFirstObjectByType<GameManager>();
        bool anyPlayerRemaining = false;

        foreach (PlayerHealth player in playersBeforeEvacuation)
        {
            if (player == null || player.photonView == null)
                continue;

            if (!processedPlayers.Contains(player.photonView.ViewID))
            {
                anyPlayerRemaining = true;
                break;
            }
        }

        if (gm != null && anyPlayerEvacuated && !anyPlayerRemaining)
        {
            gm.EndGame();
        }
    }

    [PunRPC]
    void ResetZone()
    {
        isActive = false;
        isBeingUsed = false;
        isEvacuating = false;

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
}
