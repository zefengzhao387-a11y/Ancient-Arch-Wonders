using UnityEngine;

/// <summary>
/// 单机构建：启动时由 <see cref="HardwareCompatibilityBootstrap"/> 设画质与<strong>主屏全屏</strong>；
/// 切窗口或改分辨率后，仍由 <see cref="GlobalCanvasAdaptation"/> / <see cref="Aspect16x9StrictEnforcer"/> 按当前像素重算。
/// </summary>
public static class StandaloneDisplayBootstrap
{
    /// <summary>保留作参考分辨率（16:9），当前启动逻辑已改为全屏，不再强制此窗口尺寸。</summary>
    public const int DefaultWindowWidth = 1920;
    public const int DefaultWindowHeight = 1080;

#if !UNITY_EDITOR && UNITY_STANDALONE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void ApplyBeforeSplash()
    {
        HardwareCompatibilityBootstrap.ApplyStandaloneLaunchProfileIfNeeded();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyBeforeFirstScene()
    {
        HardwareCompatibilityBootstrap.ApplyStandaloneLaunchProfileIfNeeded();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RefreshCanvasScaleAfterScene()
    {
        GlobalCanvasAdaptation.ApplyToAllCanvases();
    }
#endif
}
