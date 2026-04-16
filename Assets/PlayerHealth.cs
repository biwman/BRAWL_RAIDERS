using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    public int maxHP = 100;

    int currentHP;
    Slider hpBar;
    bool messageShowing;

    void Start()
    {
        currentHP = maxHP;

        if (photonView.IsMine)
        {
            PhotonNetwork.LocalPlayer.TagObject = gameObject;

            if (GetComponent<HealthBarUI>() == null)
            {
                gameObject.AddComponent<HealthBarUI>();
            }

            GameObject barObj = GameObject.Find("HP_Bar");
            if (barObj != null)
            {
                hpBar = barObj.GetComponent<Slider>();
                if (hpBar != null)
                {
                    hpBar.maxValue = maxHP;
                    hpBar.value = currentHP;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (photonView != null &&
            photonView.IsMine &&
            PhotonNetwork.LocalPlayer != null &&
            ReferenceEquals(PhotonNetwork.LocalPlayer.TagObject, gameObject))
        {
            PhotonNetwork.LocalPlayer.TagObject = null;
        }
    }

    [PunRPC]
    public void TakeDamage(int dmg, int attackerViewID)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        currentHP = Mathf.Max(0, currentHP - dmg);
        photonView.RPC(nameof(SyncHP), RpcTarget.All, currentHP);

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
        photonView.RPC(nameof(PlayDeathExplosion), RpcTarget.All);
        photonView.RPC(nameof(ShowDeathMessage), RpcTarget.All);

        int victimScore = GetCurrentScore();
        int retainedScore = Mathf.FloorToInt(victimScore * (RoomSettings.GetDeathRetainPercent() / 100f));
        int reward = Mathf.FloorToInt(victimScore * (RoomSettings.GetKillRewardPercent() / 100f));

        PhotonView attackerView = PhotonView.Find(attackerViewID);
        if (attackerView != null && attackerView != photonView)
        {
            attackerView.RPC(nameof(AddKillScore), attackerView.Owner, reward);
        }

        photonView.RPC(nameof(SetScoreDirect), photonView.Owner, retainedScore);

        GameTimer timer = FindFirstObjectByType<GameTimer>();
        if (timer != null && RoomSettings.GetDeathTimerPenalty() > 0)
        {
            timer.ReduceTime(RoomSettings.GetDeathTimerPenalty());
        }

        bool shouldEndGame = GetRemainingPlayersAfterThisDeath() <= 1;

        photonView.RPC(nameof(DestroySelf), photonView.Owner);

        if (shouldEndGame)
        {
            GameManager manager = FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                manager.EndGame();
            }
        }
    }

    int GetRemainingPlayersAfterThisDeath()
    {
        int aliveCount = 0;
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth player in players)
        {
            if (player == null || player == this)
                continue;

            aliveCount++;
        }

        return aliveCount;
    }

    int GetCurrentScore()
    {
        int propScore = RoomSettings.GetPlayerScore(photonView.Owner);
        if (propScore > 0)
            return propScore;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            return collector.totalScore;

        return 0;
    }

    [PunRPC]
    public void SetScoreDirect(int newScore)
    {
        if (!photonView.IsMine)
            return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[RoomSettings.ScoreKey] = Mathf.Max(0, newScore);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
        {
            collector.totalScore = Mathf.Max(0, newScore);
            collector.AddScore(0);
        }
    }

    [PunRPC]
    public void AddKillScore(int amount)
    {
        if (!photonView.IsMine)
            return;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
        {
            collector.AddScore(amount);
        }
    }

    [PunRPC]
    public void OnEvacuated(int amount)
    {
        if (!photonView.IsMine)
            return;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
        {
            collector.AddScore(amount);
        }
    }

    [PunRPC]
    public void DestroySelf()
    {
        if (!photonView.IsMine)
            return;

        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    void ShowDeathMessage()
    {
        if (messageShowing)
            return;

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

        if (obj != null)
            obj.SetActive(false);

        messageShowing = false;
    }

    [PunRPC]
    void PlayDeathExplosion()
    {
        AudioManager.Instance.PlayExplosionAt(transform.position);
    }

    [PunRPC]
    public void OnTimeUp()
    {
        if (!photonView.IsMine)
            return;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
        {
            int retainedScore = Mathf.FloorToInt(collector.totalScore * (RoomSettings.GetTimeUpRetainPercent() / 100f));
            collector.totalScore = retainedScore;
            collector.AddScore(0);
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
        yield return new WaitForSeconds(1.5f);

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    GameObject FindObjectEvenIfDisabled(string name)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject go in all)
        {
            if (go.name == name)
                return go;
        }

        return null;
    }
}
