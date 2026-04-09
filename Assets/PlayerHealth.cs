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

        // 🔥 tylko lokalny gracz szuka UI
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
    public void TakeDamage(int dmg)
    {
        // 🔥 tylko Master liczy HP
        if (!PhotonNetwork.IsMasterClient) return;

        currentHP -= dmg;

        // 🔥 synchronizacja do wszystkich
        photonView.RPC("SyncHP", RpcTarget.All, currentHP);
    }
    [PunRPC]
    void SyncHP(int newHP)
    {
        currentHP = newHP;

        if (photonView.IsMine && hpBar != null)
        {
            hpBar.value = currentHP;
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}