using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviourPun
{
    public int maxHP = 100;
    private int currentHP;

    private Slider hpBar;

    public GameObject deathMessage;
    private bool messageShowing = false;

    void Start()
    {
        currentHP = maxHP;

        if (photonView.IsMine)
        {
            GameObject barObj = GameObject.Find("HP_Bar");

            if (barObj != null)
            {
                hpBar = barObj.GetComponent<Slider>();

                hpBar.maxValue = maxHP;
                hpBar.value = currentHP;

                Debug.Log("✅ HP Bar podpięty");
            }
            else
            {
                Debug.LogError("❌ Nie znaleziono HP_Bar");
            }
        }
    }

    [PunRPC]
    public void TakeDamage(int dmg, int attackerViewID)
    {
        currentHP -= dmg;

        if (currentHP < 0)
            currentHP = 0;

        // 🔥 update HP bara tylko lokalnie
        if (photonView.IsMine && hpBar != null)
        {
            hpBar.value = currentHP;
        }

        if (currentHP <= 0)
        {
            Die(attackerViewID);
        }
    }

    void Die(int attackerViewID)
    {
        // 🔥 pokaż komunikat u wszystkich
        photonView.RPC("ShowDeathMessage", RpcTarget.All);

        PhotonView attackerPV = PhotonView.Find(attackerViewID);

        TreasureCollector victimTC = GetComponent<TreasureCollector>();

        if (victimTC != null)
        {
            int victimScore = victimTC.totalScore;

            // 🔥 zabójca dostaje 50%
            int reward = victimScore / 2;

            if (attackerPV != null)
            {
                attackerPV.RPC("AddKillScore", attackerPV.Owner, reward);
            }

            // 🔥 ofiara traci 75%
            int loss = (int)(victimScore * 0.75f);
            victimTC.totalScore -= loss;

            if (victimTC.totalScore < 0)
                victimTC.totalScore = 0;

            // 🔥 odśwież UI ofiary
            if (photonView.IsMine)
            {
                victimTC.AddScore(0);
            }
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public void AddKillScore(int amount)
    {
        if (!photonView.IsMine) return;

        TreasureCollector tc = GetComponent<TreasureCollector>();

        if (tc != null)
        {
            tc.AddScore(amount);
        }
    }

    [PunRPC]
    public void OnEvacuated(int reward)
    {
        if (!photonView.IsMine) return;

        TreasureCollector tc = GetComponent<TreasureCollector>();

        if (tc != null)
        {
            tc.AddScore(reward);
        }
    }

    [PunRPC]
    void ShowDeathMessage()
    {
        if (messageShowing) return;

        GameObject obj = GameObject.Find("DeathMessage");

        if (obj != null)
        {
            messageShowing = true;
            obj.SetActive(true);
            StartCoroutine(HideDeathMessage(obj));
        }
    }

    IEnumerator HideDeathMessage(GameObject obj)
    {
        yield return new WaitForSeconds(3f);

        obj.SetActive(false);
        messageShowing = false;
    }
}