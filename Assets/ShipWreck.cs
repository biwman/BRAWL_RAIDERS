using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[Serializable]
public class ShipWreckLootSnapshot
{
    public string[] items;
}

[RequireComponent(typeof(PhotonView))]
public class ShipWreck : MonoBehaviourPun
{
    readonly List<string> lootItems = new List<string>();
    SpriteRenderer spriteRenderer;
    Color baseColor = new Color(0.46f, 0.48f, 0.52f, 0.96f);
    int sourceShipSkinIndex;

    public bool isBeingCollected;
    public bool HasLoot => lootItems.Count > 0;
    public int LootCount => lootItems.Count;
    public int SourceShipSkinIndex => sourceShipSkinIndex;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeFromLootJson(string rawLoot, int shipSkinIndex = 0)
    {
        sourceShipSkinIndex = Mathf.Max(0, shipSkinIndex);
        lootItems.Clear();
        string[] slots = PlayerProfileService.DeserializeShipInventorySlots(rawLoot);
        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(slots[i]))
                lootItems.Add(slots[i]);
        }

        RefreshVisualState();
    }

    public int GetFirstLootIndex()
    {
        return HasLoot ? 0 : -1;
    }

    public string GetLootItemAt(int index)
    {
        if (index < 0 || index >= lootItems.Count)
            return null;

        return lootItems[index];
    }

    public void Highlight()
    {
        if (!HasLoot || spriteRenderer == null)
            return;

        spriteRenderer.color = Color.green;
    }

    public void Unhighlight()
    {
        RefreshVisualState();
    }

    void RefreshVisualState()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = HasLoot ? baseColor : new Color(0.28f, 0.29f, 0.32f, 0.78f);
    }

    [PunRPC]
    public void RemoveLootAtIndexRpc(int index)
    {
        if (index >= 0 && index < lootItems.Count)
        {
            lootItems.RemoveAt(index);
        }

        isBeingCollected = false;
        RefreshVisualState();
    }
}
