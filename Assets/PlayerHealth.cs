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

    public bool IsWreck { get; private set; }
    public bool IsBotControlled => GetComponent<EnemyBot>() != null;

    void Start()
    {
        currentHP = maxHP;

        if (photonView.IsMine && !IsBotControlled)
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
        if (!PhotonNetwork.IsMasterClient || IsWreck)
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
        if (!IsBotControlled)
        {
            photonView.RPC(nameof(ShowDeathMessage), RpcTarget.All);
            int currentRoundXp = GetCurrentRoundXp();
            RoundResultsTracker.RecordOutcome(photonView.Owner, currentRoundXp, "dead");
        }

        if (IsBotControlled)
        {
            if (PhotonNetwork.IsConnected && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
            else if (!PhotonNetwork.IsConnected)
                Destroy(gameObject);

            return;
        }

        PhotonView attackerView = PhotonView.Find(attackerViewID);
        bool killedByAnotherPlayer = attackerView != null && attackerView != photonView;

        if (killedByAnotherPlayer)
        {
            string wreckLoot = PlayerProfileService.SerializeShipInventorySlots(PlayerProfileService.GetPlayerShipInventorySlots(photonView.Owner));
            photonView.RPC(nameof(ClearLocalShipInventoryForWreck), photonView.Owner);
            photonView.RPC(nameof(BecomeWreck), RpcTarget.All, wreckLoot);
        }
        else
        {
            photonView.RPC(nameof(DestroySelf), photonView.Owner);
        }

    }

    int GetCurrentRoundXp()
    {
        int propScore = RoomSettings.GetPlayerRoundXp(photonView.Owner);
        if (propScore > 0)
            return propScore;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            return collector.totalScore;

        return 0;
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
    public async void ClearLocalShipInventoryForWreck()
    {
        if (!photonView.IsMine)
            return;

        try
        {
            await PlayerProfileService.Instance.ReplaceShipInventoryAsync(new string[PlayerInventoryData.ShipSlotCount]);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to clear ship inventory for wreck: " + ex);
        }
    }

    [PunRPC]
    void BecomeWreck(string serializedLoot)
    {
        IsWreck = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
            shooting.enabled = false;

        TreasureCollector collector = GetComponent<TreasureCollector>();
        if (collector != null)
            collector.enabled = false;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        ShipWreck wreck = GetComponent<ShipWreck>();
        if (wreck == null)
            wreck = gameObject.AddComponent<ShipWreck>();

        wreck.InitializeFromLootJson(serializedLoot);
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
