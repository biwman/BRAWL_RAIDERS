using System;
using System.Collections.Generic;
using UnityEngine;

public static class TraderItemUnlockCatalog
{
    static readonly Dictionary<string, int> RequiredLevels = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        { InventoryItemCatalog.TripleGunId, 1 },
        { InventoryItemCatalog.ArtilleryGunId, 1 },
        { InventoryItemCatalog.GatlingGunId, 5 },
        { InventoryItemCatalog.DoubleIonizerId, 5 },
        { InventoryItemCatalog.RocketLauncherId, 5 },
        { InventoryItemCatalog.PlasmaGunId, 10 },
        { InventoryItemCatalog.PulseDisruptorId, 10 },
        { InventoryItemCatalog.DoubleRocketLauncherId, 12 },
        { InventoryItemCatalog.AstroCutterId, 15 },
        { InventoryItemCatalog.RailGunId, 20 },

        { InventoryItemCatalog.ShieldReactorId, 1 },
        { InventoryItemCatalog.KineticDampenerId, 1 },
        { InventoryItemCatalog.CargoBayExtensionId, 1 },
        { InventoryItemCatalog.ShieldCapacitorId, 1 },
        { InventoryItemCatalog.PowerEngineId, 4 },
        { InventoryItemCatalog.IonEngineId, 4 },
        { InventoryItemCatalog.AfterburnerStabilizerId, 4 },
        { InventoryItemCatalog.PhaseShieldId, 6 },
        { InventoryItemCatalog.StrongPlatingId, 6 },
        { InventoryItemCatalog.AegisBatteryId, 6 },
        { InventoryItemCatalog.RegenerativeShieldMatrixId, 9 },
        { InventoryItemCatalog.FuelTankId, 9 },
        { InventoryItemCatalog.FusionEngineId, 9 },
        { InventoryItemCatalog.HybridEngineId, 12 },
        { InventoryItemCatalog.BulwarkProjectorId, 12 },
        { InventoryItemCatalog.SuperBoosterId, 15 },
        { InventoryItemCatalog.BlackMarketThrusterId, 15 },
        { InventoryItemCatalog.DoubleEngineId, 20 },
        { InventoryItemCatalog.AlienAegisCoreId, 20 },

        { InventoryItemCatalog.EmergencySuitBeaconId, 1 },
        { InventoryItemCatalog.BatteryId, 1 },
        { InventoryItemCatalog.MagneticBeamId, 1 },
        { InventoryItemCatalog.TractorBeamId, 1 },
        { InventoryItemCatalog.GadgetMineId, 4 },
        { InventoryItemCatalog.SpaceTrapId, 4 },
        { InventoryItemCatalog.SpaceDrillId, 4 },
        { InventoryItemCatalog.LootHookId, 6 },
        { InventoryItemCatalog.LureBeaconId, 6 },
        { InventoryItemCatalog.MetalDriftWallId, 6 },
        { InventoryItemCatalog.OverclockedMagazineId, 6 },
        { InventoryItemCatalog.StasisBuoyId, 8 },
        { InventoryItemCatalog.TreasureScannerId, 8 },
        { InventoryItemCatalog.ShortScannerId, 8 },
        { InventoryItemCatalog.SpaceBombId, 10 },
        { InventoryItemCatalog.TetherHarpoonId, 10 },
        { InventoryItemCatalog.SalvageMagnetArrayId, 10 },
        { InventoryItemCatalog.DropbotId, 12 },
        { InventoryItemCatalog.BioTrapId, 12 },
        { InventoryItemCatalog.AutoTurretId, 12 },
        { InventoryItemCatalog.RocketAutoTurretId, 15 },
        { InventoryItemCatalog.EscapePodId, 15 },
        { InventoryItemCatalog.CloakDeviceId, 18 },
        { InventoryItemCatalog.HackingDeviceId, 18 },
        { InventoryItemCatalog.GuidanceSystemId, 18 },

        { InventoryItemCatalog.AvengerStartingCodesId, 10 },
        { InventoryItemCatalog.PirateSymbolId, 10 },
        { InventoryItemCatalog.ShipPrototypeDocumentationId, 12 },
        { InventoryItemCatalog.PreservedAlphaSpecimenId, 15 },
        { InventoryItemCatalog.AncientGateKeyId, 20 },
        { InventoryItemCatalog.AlienTransmitterId, 15 }
    };

    public static int GetRequiredLevel(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 1;

        string normalizedItemId = itemId.Trim();
        if (RequiredLevels.TryGetValue(normalizedItemId, out int requiredLevel))
            return Mathf.Max(1, requiredLevel);

        if (InventoryItemCatalog.IsBlueprintItem(normalizedItemId))
        {
            string targetItemId = InventoryItemCatalog.GetBlueprintTargetItemId(normalizedItemId);
            if (!string.IsNullOrWhiteSpace(targetItemId) &&
                !string.Equals(targetItemId, normalizedItemId, StringComparison.Ordinal))
            {
                return GetRequiredLevel(targetItemId);
            }
        }

        InventoryItemDefinition definition = InventoryItemCatalog.GetDefinition(normalizedItemId);
        if (definition == null)
            return 1;

        return GetFallbackRequiredLevel(definition);
    }

    public static bool IsUnlocked(PlayerProfileData profile, string itemId)
    {
        int requiredLevel = GetRequiredLevel(itemId);
        if (requiredLevel <= 1)
            return true;

        if (profile != null && profile.CheatUnlockAllTraderItems)
            return true;

        int totalXp = profile != null ? Mathf.Max(0, profile.TotalXp) : 0;
        return RoundXpBalance.GetLevelForTotalXp(totalXp) >= requiredLevel;
    }

    static int GetFallbackRequiredLevel(InventoryItemDefinition definition)
    {
        if (definition == null)
            return 1;

        if (definition.ItemType == InventoryItemType.Equipment)
        {
            switch (definition.Rarity)
            {
                case InventoryItemRarity.Common:
                case InventoryItemRarity.Uncommon:
                    return 1;
                case InventoryItemRarity.Rare:
                    return 5;
                case InventoryItemRarity.VeryRare:
                    return 8;
                case InventoryItemRarity.Epic:
                    return 12;
                case InventoryItemRarity.Legendary:
                    return 20;
                default:
                    return 1;
            }
        }

        if (definition.ItemType == InventoryItemType.Quest)
        {
            switch (definition.Rarity)
            {
                case InventoryItemRarity.Legendary:
                    return 20;
                case InventoryItemRarity.Epic:
                    return 15;
                case InventoryItemRarity.Rare:
                case InventoryItemRarity.VeryRare:
                    return 10;
                default:
                    return 5;
            }
        }

        return 1;
    }
}
