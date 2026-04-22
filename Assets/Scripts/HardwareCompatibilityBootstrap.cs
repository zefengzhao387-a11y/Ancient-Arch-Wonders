using UnityEngine;

/// <summary>
/// 单机版启动：<see cref="QualitySettings"/> 始终设为<strong>最高档</strong>（除非关闭自动）；主显示器全屏。
/// 与 <see cref="StandaloneDisplayBootstrap"/> 衔接。
/// 若将来做画质菜单，可设 PlayerPrefs 键 <c>AutoGraphicsQualityDisabled</c> 为 1，则不再改写画质。
/// </summary>
public static class HardwareCompatibilityBootstrap
{
    public const string PlayerPrefsDisableAutoQuality = "AutoGraphicsQualityDisabled";

    static bool _launchProfileApplied;

    /// <summary>当前会话是否已应用启动档案（画质 + 全屏）。</summary>
    public static bool LaunchProfileApplied => _launchProfileApplied;

    /// <summary>启动时写入的画质档位索引；关闭自动时为当前 <see cref="QualitySettings"/> 档位；未应用前为 -1。</summary>
    public static int LastRecommendedQualityLevel { get; private set; } = -1;

    /// <summary>单机：画质 + 全屏 + 帧率策略，幂等。</summary>
    public static void ApplyStandaloneLaunchProfileIfNeeded()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE
        if (_launchProfileApplied)
            return;
        _launchProfileApplied = true;

        if (PlayerPrefs.GetInt(PlayerPrefsDisableAutoQuality, 0) == 0)
        {
            int max = QualitySettings.names.Length - 1;
            if (max >= 0)
            {
                LastRecommendedQualityLevel = max;
                QualitySettings.SetQualityLevel(max, true);
                Debug.Log($"[AncientArchWonders] 画质已设为最高档: {QualitySettings.names[max]} (索引 {max})");
            }
        }
        else
            LastRecommendedQualityLevel = QualitySettings.GetQualityLevel();

        ApplyFrameRatePolicy();
        ApplyStartupWindow();
        GlobalCanvasAdaptation.ApplyToAllCanvases();
#endif
    }

#if !UNITY_EDITOR && UNITY_STANDALONE
    static void ApplyFrameRatePolicy()
    {
        if (QualitySettings.vSyncCount > 0)
            Application.targetFrameRate = -1;
        else
            Application.targetFrameRate = 60;
    }

    static void ApplyStartupWindow()
    {
        int w = Mathf.Max(64, Screen.currentResolution.width);
        int h = Mathf.Max(64, Screen.currentResolution.height);
        w &= ~1;
        h &= ~1;

        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
        Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
    }
#endif
}
