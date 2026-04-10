using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    public int maxHP = 100;
    private int currentHP;

    private Slider hpBar;

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
        // 🔥 zabity znika
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }

        // 🔥 znajdź kto zabił
        PhotonView attacker = PhotonView.Find(attackerViewID);

        if (attacker != null && attacker.IsMine)
        {
            TreasureCollector collector = attacker.GetComponent<TreasureCollector>();

            if (collector != null)
            {
                collector.AddScore(10);
                Debug.Log("🏆 KILL +10");
            }
        }
    }
}