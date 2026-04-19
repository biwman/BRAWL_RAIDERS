using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum InventoryItemType
{
    Resource,
    Equipment,
    Consumable,
    Quest,
    Misc
}

public enum InventoryItemRarity
{
    Common,
    Uncommon,
    Rare,
    VeryRare,
    Epic,
    Legendary
}

[Serializable]
public class InventoryItemDefinition
{
    public string Id;
    public string DisplayName;
    public string ShortLabel;
    public string Description;
    public InventoryItemType ItemType;
    public InventoryItemRarity Rarity;
    public int SellValueAstrons;
    public string IconResourcePath;
    public string ProjectFileName;

    Sprite cachedIcon;

    public Sprite GetIcon()
    {
        if (cachedIcon != null)
            return cachedIcon;

        if (!string.IsNullOrWhiteSpace(IconResourcePath))
        {
            cachedIcon = Resources.Load<Sprite>(IconResourcePath);
            if (cachedIcon != null)
                return cachedIcon;
        }

        if (string.IsNullOrWhiteSpace(ProjectFileName))
            return null;

        string filePath = Path.Combine(Application.dataPath, ProjectFileName);
        if (!File.Exists(filePath))
            return null;

        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        cachedIcon = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            Mathf.Max(1f, texture.width));
        return cachedIcon;
    }
}

public static class InventoryItemCatalog
{
    public const string AsteroidResourceId = "asteroid_resource";
    public const string AsteroidGoldId = "asteroid_gold_resource";
    public const string AsteroidRareId = "asteroid_rare_resource";
    public const string DroidScrapId = "droid_scrap";

    static readonly Dictionary<string, InventoryItemDefinition> Definitions = BuildDefinitions();

    public static InventoryItemDefinition GetDefinition(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        Definitions.TryGetValue(itemId, out InventoryItemDefinition definition);
        return definition;
    }

    public static Sprite GetIcon(string itemId) => GetDefinition(itemId)?.GetIcon();

    public static string GetShortLabel(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        if (definition != null && !string.IsNullOrWhiteSpace(definition.ShortLabel))
            return definition.ShortLabel;

        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        return itemId.Length <= 3 ? itemId.ToUpperInvariant() : itemId.Substring(0, 3).ToUpperInvariant();
    }

    public static string GetDisplayName(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        if (definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName))
            return definition.DisplayName;

        return itemId ?? string.Empty;
    }

    public static string GetDescription(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        return definition != null ? definition.Description : string.Empty;
    }

    public static InventoryItemType GetItemType(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        return definition != null ? definition.ItemType : InventoryItemType.Misc;
    }

    public static InventoryItemRarity GetRarity(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        return definition != null ? definition.Rarity : InventoryItemRarity.Common;
    }

    public static int GetSellValueAstrons(string itemId)
    {
        InventoryItemDefinition definition = GetDefinition(itemId);
        return definition != null ? Mathf.Max(0, definition.SellValueAstrons) : 0;
    }

    public static Color GetRarityColor(string itemId)
    {
        return GetRarityColor(GetRarity(itemId));
    }

    public static Color GetRarityColor(InventoryItemRarity rarity)
    {
        switch (rarity)
        {
            case InventoryItemRarity.Uncommon: return new Color(0.2f, 0.62f, 0.24f, 0.98f);
            case InventoryItemRarity.Rare: return new Color(0.18f, 0.42f, 0.92f, 0.98f);
            case InventoryItemRarity.VeryRare: return new Color(0.48f, 0.22f, 0.78f, 0.98f);
            case InventoryItemRarity.Epic: return new Color(0.46f, 0.08f, 0.14f, 0.98f);
            case InventoryItemRarity.Legendary: return new Color(0.83f, 0.63f, 0.12f, 0.98f);
            default: return new Color(0.95f, 0.95f, 0.95f, 0.98f);
        }
    }

    static Dictionary<string, InventoryItemDefinition> BuildDefinitions()
    {
        return new Dictionary<string, InventoryItemDefinition>(StringComparer.Ordinal)
        {
            [AsteroidResourceId] = new InventoryItemDefinition
            {
                Id = AsteroidResourceId,
                DisplayName = "Common Asteroid",
                ShortLabel = "AST",
                Description = "A common asteroid fragment.",
                ItemType = InventoryItemType.Resource,
                Rarity = InventoryItemRarity.Common,
                SellValueAstrons = 10,
                IconResourcePath = "Visuals/Treasures/asteroid_treasure_resource",
                ProjectFileName = "asteroida_treasure.png"
            },
            [AsteroidGoldId] = new InventoryItemDefinition
            {
                Id = AsteroidGoldId,
                DisplayName = "Golden Asteroid",
                ShortLabel = "GLD",
                Description = "A richer asteroid vein with a higher resale value.",
                ItemType = InventoryItemType.Resource,
                Rarity = InventoryItemRarity.Legendary,
                SellValueAstrons = 30,
                IconResourcePath = "asteroida_zloto_clean_resource",
                ProjectFileName = "asteroida_zloto_clean.png"
            },
            [AsteroidRareId] = new InventoryItemDefinition
            {
                Id = AsteroidRareId,
                DisplayName = "Rare Asteroid",
                ShortLabel = "RAR",
                Description = "A rare asteroid sample shimmering with unusual energy.",
                ItemType = InventoryItemType.Resource,
                Rarity = InventoryItemRarity.VeryRare,
                SellValueAstrons = 60,
                IconResourcePath = "asteroida_rare_clean_resource",
                ProjectFileName = "asteroida_rare_clean.png"
            },
            [DroidScrapId] = new InventoryItemDefinition
            {
                Id = DroidScrapId,
                DisplayName = "Droid Wreck",
                ShortLabel = "BOT",
                Description = "A recoverable drone wreck fragment.",
                ItemType = InventoryItemType.Resource,
                Rarity = InventoryItemRarity.Uncommon,
                SellValueAstrons = 40,
                IconResourcePath = "droid1_resource",
                ProjectFileName = "droid1.png"
            }
        };
    }
}
