using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HideInNebulaTarget : MonoBehaviour
{
    static readonly HashSet<int> LocalPlayerNebulas = new HashSet<int>();

    Renderer[] renderers;
    PhotonView photonView;
    PlayerHealth playerHealth;
    Coroutine damageRoutine;
    Dictionary<int, bool> nebulaStates = new Dictionary<int, bool>();
    public bool IsHiddenForOthers => HasHiddenNebula();

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
        RefreshLocalNebulaCache();
        ApplyVisibility();

        if (playerHealth != null && photonView != null && photonView.IsMine && damageRoutine == null && nebulaStates.Count > 0)
        {
            damageRoutine = StartCoroutine(ApplyNebulaDamage());
        }
    }

    public void RemoveNebula(int nebulaId)
    {
        nebulaStates.Remove(nebulaId);
        RefreshLocalNebulaCache();
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
        bool shouldHide = HasHiddenNebula();
        bool keepLocallyVisible = photonView != null && photonView.IsMine;
        bool shouldBeVisible = !shouldHide || keepLocallyVisible || SharesNebulaWithLocalPlayer();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = shouldBeVisible;
            }
        }
    }

    bool HasHiddenNebula()
    {
        foreach (bool value in nebulaStates.Values)
        {
            if (value)
                return true;
        }

        return false;
    }

    void RefreshLocalNebulaCache()
    {
        if (photonView == null || !photonView.IsMine)
            return;

        LocalPlayerNebulas.Clear();
        foreach (KeyValuePair<int, bool> state in nebulaStates)
        {
            if (state.Value)
                LocalPlayerNebulas.Add(state.Key);
        }
    }

    bool SharesNebulaWithLocalPlayer()
    {
        if (LocalPlayerNebulas.Count == 0)
            return false;

        foreach (KeyValuePair<int, bool> state in nebulaStates)
        {
            if (state.Value && LocalPlayerNebulas.Contains(state.Key))
                return true;
        }

        return false;
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
