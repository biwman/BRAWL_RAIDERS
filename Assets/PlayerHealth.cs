using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    public int maxHP = 100;
    private int currentHP;

    private Slider hpBar;
    private bool messageShowing = false;

    private string playerName;

    // END SCREEN UI
    public GameObject endScreen;
    public TMP_Text endMessage;
    public Transform playerListContent;
    public GameObject playerListItemPrefab;

    void Start()
    {
        currentHP = maxHP;

        // przypisz zawsze
        if (photonView.IsMine)
        {
            PhotonNetwork.LocalPlayer.TagObject = gameObject;
        }

        if (photonView.IsMine)
        {
            playerName = PhotonNetwork.NickName;

            if (GetComponent<HealthBarUI>() == null)
            {
                gameObject.AddComponent<HealthBarUI>();
            }

            GameObject barObj = GameObject.Find("HP_Bar");
            if (barObj != null)
            {
                hpBar = barObj.GetComponent<Slider>();
                hpBar.maxValue = maxHP;
                hpBar.value = currentHP;
            }
        }
    }

    // DAMAGE liczy tylko MASTER
    [PunRPC]
    public void TakeDamage(int dmg, int attackerViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentHP -= dmg;
        if (currentHP < 0)
            currentHP = 0;

        photonView.RPC("SyncHP", RpcTarget.All, currentHP);

        if (currentHP <= 0)
        {
            HandleDeath(attackerViewID);
        }
    }

    [PunRPC]
    void SyncHP(int newHP)
    {
        currentHP = newHP;

        if (photonView.IsMine && hpBar != null)
        {
            hpBar.value = currentHP;
        }
    }

    void HandleDeath(int attackerViewID)
    {
        photonView.RPC("ShowDeathMessage", RpcTarget.All);

        PhotonView attackerPV = PhotonView.Find(attackerViewID);
        TreasureCollector victimTC = GetComponent<TreasureCollector>();

        if (victimTC != null)
        {
            int victimScore = victimTC.totalScore;

            // killer dostaje 50%
            int reward = victimScore / 2;

            if (attackerPV != null)
            {
                attackerPV.RPC("AddKillScore", attackerPV.Owner, reward);
            }

            // ofiara traci 75%
            int loss = (int)(victimScore * 0.75f);
            victimTC.totalScore -= loss;

            if (victimTC.totalScore < 0)
                victimTC.totalScore = 0;

            photonView.RPC("RefreshScore", photonView.Owner, victimTC.totalScore);
        }

        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    void RefreshScore(int newScore)
    {
        if (!photonView.IsMine) return;

        TreasureCollector tc = GetComponent<TreasureCollector>();

        if (tc != null)
        {
            tc.totalScore = newScore;
            tc.AddScore(0); // refresh UI
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
    public void OnEvacuated(int amount)
    {
        if (!photonView.IsMine) return;

        Debug.Log("EVACUATED");

        TreasureCollector tc = GetComponent<TreasureCollector>();
        if (tc != null)
        {
            tc.AddScore(amount);
        }
    }

    [PunRPC]
    public void DestroySelf()
    {
        if (!photonView.IsMine) return;

        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // DEATH MESSAGE
    [PunRPC]
    void ShowDeathMessage()
    {
        if (messageShowing) return;

        GameObject obj = FindObjectEvenIfDisabled("DeathMessage");

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

    // END SCREEN
    [PunRPC]
    

    void DisplaySortedPlayers()
    {
        if (playerListContent == null)
        {
            GameObject contentObj = FindObjectEvenIfDisabled("PlayerListContent");
            if (contentObj != null)
                playerListContent = contentObj.transform;
        }

        if (playerListItemPrefab == null)
        {
            playerListItemPrefab = Resources.Load<GameObject>("PlayerListItem");
        }

        if (playerListContent == null || playerListItemPrefab == null)
            return;

        //  usuń stare wpisy
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // znajdź wszystkich graczy w scenie
        var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        var sorted = allPlayers
            .OrderByDescending(p =>
            {
                TreasureCollector tc = p.GetComponent<TreasureCollector>();
                return tc != null ? tc.totalScore : 0;
            })
            .ToList();

        foreach (var player in sorted)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();

            string name = player.photonView.Owner.NickName;

            TreasureCollector tc = player.GetComponent<TreasureCollector>();
            int score = tc != null ? tc.totalScore : 0;

            text.text = name + " - " + score;
        }
    }

    // TIME UP
    [PunRPC]
    public void OnTimeUp()
    {
        if (!photonView.IsMine) return;

        Debug.Log("💀 TIME UP");

        TreasureCollector tc = GetComponent<TreasureCollector>();

        if (tc != null)
        {
            int newScore = tc.totalScore / 4;
            tc.totalScore = newScore;
            tc.AddScore(0);
        }

        ShowTimeUpMessage();

        StartCoroutine(DieAfterDelay());
    }

    void ShowTimeUpMessage()
    {
        GameObject obj = GameObject.Find("TimeUpMessage");

        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // znajdzie nawet NIEAKTYWNY obiekt
    GameObject FindObjectEvenIfDisabled(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in all)
        {
            if (go.name == name)
                return go;
        }

        return null;
    }
}
