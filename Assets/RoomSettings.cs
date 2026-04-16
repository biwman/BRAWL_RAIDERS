using Photon.Pun;
using UnityEngine;

public static class RoomSettings
{
    public const string RoundDurationKey = "roundDuration";
    public const string ObstacleDensityKey = "obstacleDensity";
    public const string TreasureDensityKey = "treasureDensity";
    public const string NebulaDensityKey = "nebulaDensity";
    public const string ExtractionCountKey = "extractionCount";
    public const string BoosterSlowdownKey = "boosterSlowdownPercent";
    public const string AmmoCountKey = "ammoCount";
    public const string BoosterRecoveryDelayKey = "boosterRecoveryDelay";
    public const string DeathTimerPenaltyKey = "deathTimerPenalty";
    public const string KillRewardPercentKey = "killRewardPercent";
    public const string DeathRetainPercentKey = "deathRetainPercent";
    public const string TimeUpRetainPercentKey = "timeUpRetainPercent";
    public const string MapSizeKey = "mapSize";
    public const string ScoreKey = "score";

    public const float DefaultRoundDuration = 180f;
    public const int DefaultExtractionCount = 3;
    public const int DefaultBoosterSlowdownPercent = 30;
    public const int DefaultAmmoCount = 10;
    public const int DefaultBoosterRecoveryDelay = 0;
    public const int DefaultDeathTimerPenalty = 30;
    public const int DefaultKillRewardPercent = 50;
    public const int DefaultDeathRetainPercent = 25;
    public const int DefaultTimeUpRetainPercent = 25;
    public const string DefaultMapSize = "medium";

    public static float GetRoundDuration()
    {
        if (TryGetFloat(RoundDurationKey, out float value))
            return value;

        return DefaultRoundDuration;
    }

    public static int GetExtractionCount()
    {
        return GetInt(ExtractionCountKey, DefaultExtractionCount, 1, 4);
    }

    public static int GetBoosterSlowdownPercent()
    {
        return GetInt(BoosterSlowdownKey, DefaultBoosterSlowdownPercent, 30, 100);
    }

    public static int GetAmmoCount()
    {
        return GetInt(AmmoCountKey, DefaultAmmoCount, 5, 30);
    }

    public static int GetBoosterRecoveryDelay()
    {
        return GetInt(BoosterRecoveryDelayKey, DefaultBoosterRecoveryDelay, 0, 10);
    }

    public static int GetDeathTimerPenalty()
    {
        return GetInt(DeathTimerPenaltyKey, DefaultDeathTimerPenalty, 0, 30);
    }

    public static int GetKillRewardPercent()
    {
        return GetInt(KillRewardPercentKey, DefaultKillRewardPercent, 0, 100);
    }

    public static int GetDeathRetainPercent()
    {
        return GetInt(DeathRetainPercentKey, DefaultDeathRetainPercent, 0, 100);
    }

    public static int GetTimeUpRetainPercent()
    {
        return GetInt(TimeUpRetainPercentKey, DefaultTimeUpRetainPercent, 0, 100);
    }

    public static string GetMapSizeMode()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MapSizeKey, out object value) &&
            value is string mode)
        {
            switch (mode)
            {
                case "small":
                case "medium":
                case "large":
                case "very_large":
                case "super_large":
                    return mode;
            }
        }

        return DefaultMapSize;
    }

    public static Vector2 GetMapDimensions()
    {
        switch (GetMapSizeMode())
        {
            case "small":
                return new Vector2(20f, 20f);
            case "large":
                return new Vector2(32f, 32f);
            case "very_large":
                return new Vector2(40f, 40f);
            case "super_large":
                return new Vector2(50f, 50f);
            default:
                return new Vector2(25f, 25f);
        }
    }

    public static float GetMapAreaMultiplier()
    {
        Vector2 size = GetMapDimensions();
        const float baseArea = 25f * 25f;
        float area = size.x * size.y;
        return Mathf.Max(0.5f, area / baseArea);
    }

    public static int GetPlayerScore(Photon.Realtime.Player player)
    {
        if (player != null &&
            player.CustomProperties.TryGetValue(ScoreKey, out object value))
        {
            return ConvertToInt(value, 0);
        }

        return 0;
    }

    static int GetInt(string key, int defaultValue, int min, int max)
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value))
        {
            return Mathf.Clamp(ConvertToInt(value, defaultValue), min, max);
        }

        return defaultValue;
    }

    static bool TryGetFloat(string key, out float result)
    {
        result = DefaultRoundDuration;

        if (PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object value))
        {
            return false;
        }

        if (value is float floatValue)
        {
            result = floatValue;
            return true;
        }

        if (value is int intValue)
        {
            result = intValue;
            return true;
        }

        if (value is double doubleValue)
        {
            result = (float)doubleValue;
            return true;
        }

        return false;
    }

    static int ConvertToInt(object value, int fallback)
    {
        if (value is int intValue)
            return intValue;

        if (value is float floatValue)
            return Mathf.RoundToInt(floatValue);

        if (value is double doubleValue)
            return Mathf.RoundToInt((float)doubleValue);

        if (value is byte byteValue)
            return byteValue;

        return fallback;
    }
}
