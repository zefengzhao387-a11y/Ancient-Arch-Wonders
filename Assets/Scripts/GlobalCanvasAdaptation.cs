using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 构建后统一 Canvas 缩放策略：对尚未使用「随屏幕缩放 + 参考分辨率」的 <see cref="CanvasScaler"/> 设为 1920×1080 + <see cref="CanvasScaler.ScreenMatchMode.Expand"/>；
/// 已在场景里配好的 <c>ScaleWithScreenSize</c>（含 MatchWidthOrHeight）且参考分辨率一致时<strong>不再改写</strong>，避免开局 UI 缩放突变。
/// 16:9 黑边 / 摄像机视口仍由 <see cref="Aspect16x9StrictEnforcer"/> 负责；不再在 Canvas 下内接 <c>__16x9LayoutRoot</c>，UI 相对根 Canvas 全屏布局。
/// </summary>
public static class GlobalCanvasAdaptation
{
    private static readonly Vector2 TargetResolution = new Vector2(1920f, 1080f);
    private const float TargetAspect = 16f / 9f;
    private const string SkipCanvasName = "__AspectBarsCanvas";
    private const string LegacyLayoutRootName = "__16x9LayoutRoot";

    private static Vector2Int _lastAppliedScreenSize = new Vector2Int(int.MinValue, 0);
    private static Vector2Int _lastAppliedRenderKey = new Vector2Int(int.MinValue, 0);
    private static bool _driverSpawned;
    private static CanvasAdaptationDriver _activeDriver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapDriver()
    {
        if (!_driverSpawned)
        {
            _driverSpawned = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var go = new GameObject(nameof(GlobalCanvasAdaptation) + "_Driver");
            VideoPlaybackUtility.MarkPersistRoot(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<CanvasAdaptationDriver>();
        }
        ApplyToAllCanvases();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _lastAppliedScreenSize = new Vector2Int(int.MinValue, 0);
        _lastAppliedRenderKey = new Vector2Int(int.MinValue, 0);
        ApplyToAllCanvases();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyAfterSceneLoad()
    {
        _lastAppliedScreenSize = new Vector2Int(int.MinValue, 0);
        _lastAppliedRenderKey = new Vector2Int(int.MinValue, 0);
        ApplyToAllCanvases();
    }

    /// <summary>给定像素宽高时，内接 16:9 内容区宽高（与 <see cref="Aspect16x9StrictEnforcer"/> 视口一致）。</summary>
    public static void GetEffective16x9ScreenSize(float widthPx, float heightPx, out float sw, out float sh)
    {
        float W = Mathf.Max(1f, widthPx);
        float H = Mathf.Max(1f, heightPx);
        float a = W / H;
        if (a > TargetAspect + 0.0001f)
        {
            sh = H;
            sw = sh * TargetAspect;
        }
        else if (a < TargetAspect - 0.0001f)
        {
            sw = W;
            sh = sw / TargetAspect;
        }
        else
        {
            sw = W;
            sh = H;
        }
    }

    /// <summary>当前 <see cref="Screen"/> 尺寸下的内接 16:9 像素宽高。</summary>
    public static void GetEffective16x9ScreenSize(out float sw, out float sh)
    {
        GetEffective16x9ScreenSize(Screen.width, Screen.height, out sw, out sh);
    }

    internal static void ApplyToAllCanvases()
    {
        UnwrapLegacy16x9LayoutRoots();

        var scalers = UnityEngine.Object.FindObjectsOfType<CanvasScaler>(true);
        foreach (var scaler in scalers)
        {
            if (scaler == null) continue;
            if (scaler.gameObject.name == SkipCanvasName) continue;
            ApplyUniformFitScaler(scaler);
        }
    }

    /// <summary>旧版在根 Canvas 下创建的 <c>__16x9LayoutRoot</c>：子 UI 移回根节点并删除容器，恢复全屏布局。</summary>
    static void UnwrapLegacy16x9LayoutRoots()
    {
        var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            if (!canvas.isRootCanvas) continue;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (canvas.gameObject.name == SkipCanvasName) continue;

            var canvasRt = canvas.transform as RectTransform;
            if (canvasRt == null) continue;

            var layoutRt = canvasRt.Find(LegacyLayoutRootName) as RectTransform;
            if (layoutRt == null) continue;

            var movers = new System.Collections.Generic.List<Transform>();
            foreach (Transform ch in layoutRt)
                movers.Add(ch);
            foreach (var ch in movers)
                ch.SetParent(canvasRt, true);

            Object.Destroy(layoutRt.gameObject);
        }
    }

    /// <summary>
    /// 整页等比缩放：使用 <see cref="CanvasScaler.ScreenMatchMode.Expand"/>（即 min 宽高比），
    /// 与引擎内对 <see cref="Canvas.renderingDisplaySize"/> 的计算一致，避免手写 match 与 DPI/渲染尺寸不同步。
    /// </summary>
    // AI辅助：Google Gemini（2026-04，CanvasScaler Match/Expand 与重复 Apply 讨论）— 以下为人工添加的早返回与后续工程调整。
    public static void ApplyUniformFitScaler(CanvasScaler scaler)
    {
        // 已是「随屏幕缩放 + 设计分辨率」时不再改写（含 MatchWidthOrHeight），避免开局把 Expand 硬套上去导致整棵 UI 缩放突变、按钮错位。
        if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
            scaler.referenceResolution == TargetResolution)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = TargetResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    private sealed class CanvasAdaptationDriver : MonoBehaviour
    {
        void Awake()
        {
            _activeDriver = this;
        }

        void OnDestroy()
        {
            if (_activeDriver == this)
                _activeDriver = null;
        }

        /// <summary>
        /// 仅在分辨率 / 主 Overlay 渲染尺寸变化时重算，避免开局连续多帧写 CanvasScaler 导致 UI 反复布局、按钮「跳位」。
        /// </summary>
        private void LateUpdate()
        {
            var cur = new Vector2Int(Screen.width, Screen.height);
            var renderKey = ComputeRenderingSizeKey();
            bool screenChanged = cur != _lastAppliedScreenSize;
            bool renderChanged = renderKey != _lastAppliedRenderKey;
            if (!screenChanged && !renderChanged)
                return;

            _lastAppliedScreenSize = cur;
            _lastAppliedRenderKey = renderKey;
            ApplyToAllCanvases();
        }

        /// <summary>用主 Overlay 根 Canvas 的渲染尺寸作为附加键（与 DPI / 独占全屏切换相关）。</summary>
        static Vector2Int ComputeRenderingSizeKey()
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c == null || !c.isRootCanvas) continue;
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (c.gameObject.name == SkipCanvasName) continue;
                var s = c.renderingDisplaySize;
                return new Vector2Int(Mathf.RoundToInt(s.x), Mathf.RoundToInt(s.y));
            }

            return new Vector2Int(Screen.width, Screen.height);
        }
    }
}
