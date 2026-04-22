using UnityEngine;
using UnityEngine.UI;

// 构建说明：GetOrCreateBar 仅使用 barImage 单变量（勿改回双 var img，会触发 CS0136）。

/// <summary>
/// 任意窗口比例 / 全屏下：游戏画面与 UI 内容区<strong>严格 16:9</strong>，多出来的屏幕区域用<strong>纯黑</strong>填充。
/// 1) 底层全屏黑相机：保证 letterbox/pillarbox 区域不是花屏或桌面透出来；
/// 2) 其余相机 viewport 裁成与 16:9 内容区一致；
/// 3) 名为 __AspectBarsCanvas 的全屏 Overlay 再叠一层黑条（ConstantPixelSize），与 UI 对齐。
/// </summary>
public class Aspect16x9StrictEnforcer : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private const string RuntimeRootName = "__Aspect16x9StrictEnforcer";
    private const string BarsCanvasName = "__AspectBarsCanvas";
    private const string BlackBgCameraName = "__16x9BlackBackgroundCam";

    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private int _lastCameraCount = -1;
    private Canvas _barsCanvas;
    private RectTransform _topBar;
    private RectTransform _bottomBar;
    private RectTransform _leftBar;
    private RectTransform _rightBar;
    private Camera _blackBackgroundCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var existing = FindObjectOfType<Aspect16x9StrictEnforcer>();
        if (existing != null) return;

        var go = new GameObject(RuntimeRootName);
        VideoPlaybackUtility.MarkPersistRoot(go);
        go.AddComponent<Aspect16x9StrictEnforcer>();
    }

    private void Awake()
    {
        EnsureBlackBackgroundCamera();
        EnsureBarsCanvas();
        Apply();
    }

    private void LateUpdate()
    {
        var now = new Vector2Int(Screen.width, Screen.height);
        int camCount = Camera.allCameras != null ? Camera.allCameras.Length : 0;
        if (now == _lastScreenSize && camCount == _lastCameraCount) return;
        _lastCameraCount = camCount;
        Apply();
    }

    private void Apply()
    {
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        SyncBlackBackgroundCamera();
        ApplyCameraViewport();
        ApplyBars();
    }

    /// <summary>最底层：整屏清成纯黑，不参与 16:9 裁切。</summary>
    void EnsureBlackBackgroundCamera()
    {
        if (_blackBackgroundCamera != null) return;

        var existing = GameObject.Find(BlackBgCameraName);
        GameObject go;
        if (existing == null)
        {
            go = new GameObject(BlackBgCameraName);
            VideoPlaybackUtility.MarkPersistRoot(go);
        }
        else
            go = existing;

        _blackBackgroundCamera = go.GetComponent<Camera>();
        if (_blackBackgroundCamera == null)
            _blackBackgroundCamera = go.AddComponent<Camera>();

        SyncBlackBackgroundCamera();
    }

    void SyncBlackBackgroundCamera()
    {
        if (_blackBackgroundCamera == null) return;
        _blackBackgroundCamera.enabled = true;
        _blackBackgroundCamera.depth = -100;
        _blackBackgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);
        _blackBackgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        _blackBackgroundCamera.backgroundColor = Color.black;
        _blackBackgroundCamera.cullingMask = 0;
        _blackBackgroundCamera.orthographic = true;
        _blackBackgroundCamera.orthographicSize = 1f;
        _blackBackgroundCamera.nearClipPlane = 0.3f;
        _blackBackgroundCamera.farClipPlane = 2f;
        _blackBackgroundCamera.useOcclusionCulling = false;
        _blackBackgroundCamera.allowHDR = false;
        _blackBackgroundCamera.allowMSAA = false;
    }

    private void ApplyCameraViewport()
    {
        float windowAspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
        Rect rect;

        if (windowAspect > TargetAspect)
        {
            float normalizedWidth = TargetAspect / windowAspect;
            float x = (1f - normalizedWidth) * 0.5f;
            rect = new Rect(x, 0f, normalizedWidth, 1f);
        }
        else
        {
            float normalizedHeight = windowAspect / TargetAspect;
            float y = (1f - normalizedHeight) * 0.5f;
            rect = new Rect(0f, y, 1f, normalizedHeight);
        }

        var cams = Camera.allCameras;
        foreach (var cam in cams)
        {
            if (cam == null) continue;
            if (cam.gameObject.name == BlackBgCameraName) continue;
            cam.rect = rect;
            // 非 16:9 窗口下，若仍用 Skybox / 默认实色整屏清屏，会盖住 depth=-100 的黑底相机，letterbox 呈默认天蓝色。
            ApplyLetterboxSafeClear(cam);
        }
    }

    /// <summary>
    /// 让裁切后的相机只清深度（保留底层纯黑），或把实色背景改为黑，避免上下/左右边带发蓝。
    /// </summary>
    static void ApplyLetterboxSafeClear(Camera cam)
    {
        switch (cam.clearFlags)
        {
            case CameraClearFlags.Skybox:
                cam.clearFlags = CameraClearFlags.Depth;
                cam.backgroundColor = Color.black;
                break;
            case CameraClearFlags.SolidColor:
                cam.backgroundColor = Color.black;
                break;
            default:
                break;
        }
    }

    private void EnsureBarsCanvas()
    {
        if (_barsCanvas != null) return;

        var canvasGo = GameObject.Find(BarsCanvasName);
        if (canvasGo == null)
            canvasGo = new GameObject(BarsCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        VideoPlaybackUtility.MarkPersistRoot(canvasGo);

        _barsCanvas = canvasGo.GetComponent<Canvas>();
        _barsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _barsCanvas.sortingOrder = -1000;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        // 与物理像素 1:1，黑条锚点按屏比例才不会被 ScaleWithScreenSize 拉变形
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        var root = canvasGo.transform as RectTransform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        _topBar = GetOrCreateBar(root, "TopBar");
        _bottomBar = GetOrCreateBar(root, "BottomBar");
        _leftBar = GetOrCreateBar(root, "LeftBar");
        _rightBar = GetOrCreateBar(root, "RightBar");
    }

    private static RectTransform GetOrCreateBar(RectTransform parent, string name)
    {
        Transform child = parent.Find(name);
        GameObject go;
        if (child == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
        }
        else
            go = child.gameObject;

        Image barImage = go.GetComponent<Image>();
        if (barImage != null)
        {
            barImage.color = Color.black;
            barImage.raycastTarget = false;
        }

        return go.GetComponent<RectTransform>();
    }

    private void ApplyBars()
    {
        if (_barsCanvas == null) return;

        float screenW = Mathf.Max(1f, Screen.width);
        float screenH = Mathf.Max(1f, Screen.height);
        float windowAspect = screenW / screenH;

        SetBar(_topBar, 0f, 0f, 1f, 0f);
        SetBar(_bottomBar, 0f, 1f, 1f, 1f);
        SetBar(_leftBar, 0f, 0f, 0f, 1f);
        SetBar(_rightBar, 1f, 0f, 1f, 1f);

        if (windowAspect > TargetAspect)
        {
            float usedWidth = TargetAspect / windowAspect;
            float side = (1f - usedWidth) * 0.5f;
            SetBar(_leftBar, 0f, 0f, side, 1f);
            SetBar(_rightBar, 1f - side, 0f, 1f, 1f);
        }
        else if (windowAspect < TargetAspect)
        {
            float usedHeight = windowAspect / TargetAspect;
            float side = (1f - usedHeight) * 0.5f;
            SetBar(_topBar, 0f, 1f - side, 1f, 1f);
            SetBar(_bottomBar, 0f, 0f, 1f, side);
        }
    }

    private static void SetBar(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
