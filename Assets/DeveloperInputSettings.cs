using UnityEngine;

public static class DeveloperInputSettings
{
    const string PcTouchJoystickTestModeKey = "starjackers.dev.pc_touch_joystick_test_mode";
    const string FpsLimitKey = "starjackers.dev.fps_limit";

    public const int AutoFpsLimit = -1;
    public const int DefaultFpsLimit = 45;

    public static bool PcTouchJoystickTestModeEnabled
    {
        get => PlayerPrefs.GetInt(PcTouchJoystickTestModeKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(PcTouchJoystickTestModeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int FpsLimit
    {
        get => NormalizeFpsLimit(PlayerPrefs.GetInt(FpsLimitKey, DefaultFpsLimit));
        set
        {
            PlayerPrefs.SetInt(FpsLimitKey, NormalizeFpsLimit(value));
            PlayerPrefs.Save();
        }
    }

    public static bool IsFpsLimitAuto => FpsLimit == AutoFpsLimit;

    public static int NormalizeFpsLimit(int value)
    {
        switch (value)
        {
            case AutoFpsLimit:
            case 30:
            case 40:
            case 45:
            case 60:
                return value;
            default:
                return DefaultFpsLimit;
        }
    }

    public static string FormatFpsLimit()
    {
        int limit = FpsLimit;
        return limit == AutoFpsLimit ? "AUTO" : limit.ToString();
    }
}
