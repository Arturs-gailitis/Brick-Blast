using UnityEngine;

public static class SoundSettingsManager
{
    private const string SoundMutedKey = "SoundEffectsMuted";

    public static bool IsMuted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSavedSetting()
    {
        IsMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;

        ApplySoundSetting();
    }

    public static void ToggleMute()
    {
        SetMuted(!IsMuted);
    }

    public static void SetMuted(bool isMuted)
    {
        IsMuted = isMuted;

        PlayerPrefs.SetInt(SoundMutedKey,IsMuted ? 1 : 0);

        PlayerPrefs.Save();

        ApplySoundSetting();
    }

    private static void ApplySoundSetting()
    {
        AudioListener.volume = IsMuted ? 0f : 1f;
    }
}