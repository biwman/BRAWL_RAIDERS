using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

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

    // 🔥 wywoływane przez gracza (idzie do MASTER)
    public void TryUse(PhotonView playerView)
    {
        if (!PhotonNetwork.IsMasterClient) return;

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
            photonView.RPC("ActivateZone", RpcTarget.All);
            photonView.RPC("ShowExtractionMessage", RpcTarget.All);
        }
        else
        {
            // 🔥 tylko MASTER wywołuje ewakuację
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

        // 🔥 timeout → tylko MASTER odpala ewakuację
        if (PhotonNetwork.IsMasterClient)
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

    // 🔥 KLUCZOWA FUNKCJA (tylko MASTER)
    void EvacuatePlayers()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isEvacuating) return;

        isEvacuating = true;

        Debug.Log("🚁 EVACUATION!");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.0f);

        // 🔥 zabezpieczenie przed duplikacją
        HashSet<int> processedPlayers = new HashSet<int>();

        foreach (var hit in hits)
        {
            PlayerHealth p = hit.GetComponent<PlayerHealth>();

            if (p != null)
            {
                PhotonView pv = p.photonView;

                if (processedPlayers.Contains(pv.ViewID))
                    continue;

                processedPlayers.Add(pv.ViewID);

                // 🔥 punkt tylko dla właściciela
                pv.RPC("OnEvacuated", pv.Owner, 5);

                // 🔥 usunięcie gracza
                PhotonNetwork.Destroy(pv.gameObject);
            }
        }

        photonView.RPC("ResetZone", RpcTarget.All);
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
        var allTexts = Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();

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