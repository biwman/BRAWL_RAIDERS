using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HideInNebulaTarget : MonoBehaviour
{
    Renderer[] renderers;
    PhotonView photonView;
    PlayerHealth playerHealth;
    Coroutine damageRoutine;
    Dictionary<int, bool> nebulaStates = new Dictionary<int, bool>();

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        playerHealth = GetComponent<PlayerHealth>();
        CacheRenderers();
    }

    public void UpdateNebulaState(int nebulaId, bool shouldHide)
    {
        CacheRenderers();
        nebulaStates[nebulaId] = shouldHide;
        ApplyVisibility();

        if (playerHealth != null && photonView != null && photonView.IsMine && damageRoutine == null && nebulaStates.Count > 0)
        {
            damageRoutine = StartCoroutine(ApplyNebulaDamage());
        }
    }

    public void RemoveNebula(int nebulaId)
    {
        nebulaStates.Remove(nebulaId);
        ApplyVisibility();

        if (nebulaStates.Count == 0 && damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
    }

    void CacheRenderers()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    void ApplyVisibility()
    {
        bool shouldHide = false;

        foreach (bool value in nebulaStates.Values)
        {
            if (value)
            {
                shouldHide = true;
                break;
            }
        }

        bool keepLocallyVisible = photonView != null && photonView.IsMine;
        bool shouldBeVisible = !shouldHide || keepLocallyVisible;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = shouldBeVisible;
            }
        }
    }

    IEnumerator ApplyNebulaDamage()
    {
        while (nebulaStates.Count > 0)
        {
            yield return new WaitForSeconds(2f);

            if (nebulaStates.Count <= 0)
                break;

            if (playerHealth == null || photonView == null || !photonView.IsMine)
                continue;

            if (PhotonNetwork.CurrentRoom == null ||
                !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) ||
                value is not bool started ||
                !started)
            {
                continue;
            }

            playerHealth.photonView.RPC("TakeDamage", RpcTarget.MasterClient, 1, -1);
        }

        damageRoutine = null;
    }
}
