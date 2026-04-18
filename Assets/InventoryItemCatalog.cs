using System;
using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemType
{
    Resource,
    Equipment,
    Consumable,
    Quest,
    Misc
}

[Serializable]
public class InventoryItemDefinition
{
    public string Id;
    public string DisplayName;
    public string ShortLabel;
    public string Description;
    public InventoryItemType ItemType;
    public string IconResourcePath;

    Sprite cachedIcon;

    public Sprite GetIcon()
    {
        if (cachedIcon != null)
            return cachedIcon;

        if (string.IsNullOrWhiteSpace(IconResourcePath))
            return null;

        cachedIcon = Resources.Load<Sprite>(IconResourcePath);
        return cachedIcon;
    }
}

public static class InventoryItemCatalog
{
    public const string AsteroidResourceId = "asteroid_resource";

    static readonly Dictionary<string, InventoryItemDefinition> Definitions = BuildDefinitions();

    public static InventoryItemDefinition GetDefinition(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        Definitions.TryGetValue(itemId, out InventoryItemDefinition definition);
        return definition;
    }

    public static Sprite GetIcon(string itemId)
    {
        return GetDefinition(itemId)?.GetIcon();
    }

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

    static Dictionary<string, InventoryItemDefinition> BuildDefinitions()
    {
        var definitions = new Dictionary<string, InventoryItemDefinition>(StringComparer.Ordinal)
        {
            [AsteroidResourceId] = new InventoryItemDefinition
            {
                Id = AsteroidResourceId,
                DisplayName = "Asteroid Core",
                ShortLabel = "AST",
                Description = "A rough asteroid fragment recovered during the round.",
                ItemType = InventoryItemType.Resource,
                IconResourcePath = ResolveFirstExistingIcon(
                    "Visuals/Treasures/asteroid_treasure_resource",
                    "Visuals/Treasures/asteroid_treasure")
            }
        };

        return definitions;
    }

    static string ResolveFirstExistingIcon(params string[] candidates)
    {
        if (candidates == null)
            return string.Empty;

        for (int i = 0; i < candidates.Length; i++)
        {
            string path = candidates[i];
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (Resources.Load<Sprite>(path) != null)
                return path;
        }

        return candidates.Length > 0 ? candidates[0] : string.Empty;
    }
}
