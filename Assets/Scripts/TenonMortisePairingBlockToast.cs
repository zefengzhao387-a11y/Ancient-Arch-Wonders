using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在「微微铆合后、绿色区回车彻底铆合前」尝试再次配对时的提示弹窗；未在 Inspector 绑定时会挂在 Canvas 下自动生成简单 UI。
/// </summary>
public class TenonMortisePairingBlockToast : MonoBehaviour
{
    static Sprite _whiteSprite;

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text messageText;
    [SerializeField] private Button okButton;
    [Tooltip(">0 时除点确定外会在若干秒后自动关闭")]
    [SerializeField] private float autoHideSeconds = 3.5f;

    private Coroutine _autoHideRoutine;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            WireOkButton();
        }
    }

    private void Start()
    {
        if (panelRoot == null)
            BuildRuntimeUiIfNeeded();
    }

    void WireOkButton()
    {
        if (okButton == null) return;
        okButton.onClick.RemoveListener(Hide);
        okButton.onClick.AddListener(Hide);
    }

    public void Show(string message)
    {
        if (panelRoot == null)
            BuildRuntimeUiIfNeeded();
        if (panelRoot == null) return;

        if (messageText != null)
            messageText.text = message ?? "";

        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        if (autoHideSeconds > 0f)
            _autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
    }

    IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoHideSeconds);
        _autoHideRoutine = null;
        Hide();
    }

    public void Hide()
    {
        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void BuildRuntimeUiIfNeeded()
    {
        if (panelRoot != null) return;

        var hostCanvas = FindBestHostCanvas();
        if (hostCanvas == null) return;

        var root = new GameObject("PairingBlockToast", typeof(RectTransform));
        root.transform.SetParent(hostCanvas.transform, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var sortCanvas = root.AddComponent<Canvas>();
        sortCanvas.overrideSorting = true;
        sortCanvas.sortingOrder = 32000;
        root.AddComponent<GraphicRaycaster>();

        var dim = CreateUiChild<Image>(root.transform, "Dim", out var dimRt);
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        dim.sprite = WhiteSprite();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        dim.raycastTarget = true;

        var box = CreateUiChild<Image>(root.transform, "Box", out var boxRt);
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(720f, 220f);
        boxRt.anchoredPosition = Vector2.zero;
        box.sprite = WhiteSprite();
        box.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
        box.raycastTarget = true;

        var textGo = CreateUiChild<Text>(box.transform, "Message", out var textRt);
        textRt.anchorMin = new Vector2(0.06f, 0.38f);
        textRt.anchorMax = new Vector2(0.94f, 0.92f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        messageText = textGo;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.fontSize = 26;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;
        messageText.raycastTarget = false;
        messageText.font = ResolveUiFont();

        var btnHost = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnHost.transform.SetParent(box.transform, false);
        var btnRt = btnHost.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.08f);
        btnRt.anchorMax = new Vector2(0.65f, 0.32f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        btnRt.localScale = Vector3.one;
        var btnImg = btnHost.GetComponent<Image>();
        btnImg.sprite = WhiteSprite();
        btnImg.color = new Color(0.25f, 0.55f, 0.35f, 1f);
        okButton = btnHost.GetComponent<Button>();
        okButton.targetGraphic = btnImg;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(btnHost.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var btnLabel = labelGo.GetComponent<Text>();
        btnLabel.text = "知道了";
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color = Color.white;
        btnLabel.fontSize = 22;
        btnLabel.raycastTarget = false;
        btnLabel.font = ResolveUiFont();

        panelRoot = root;
        panelRoot.SetActive(false);
        WireOkButton();
    }

    static T CreateUiChild<T>(Transform parent, string name, out RectTransform rt) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return go.AddComponent<T>();
    }

    static Canvas FindBestHostCanvas()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length == 0) return null;
        Canvas best = null;
        int bestOrder = int.MinValue;
        foreach (var c in canvases)
        {
            if (c == null || !c.isActiveAndEnabled || !c.gameObject.activeInHierarchy) continue;
            if (c.sortingOrder >= bestOrder)
            {
                bestOrder = c.sortingOrder;
                best = c;
            }
        }
        return best != null ? best : canvases[0];
    }

    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var t = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        _whiteSprite.name = "TenonPairingToastWhite";
        return _whiteSprite;
    }

    static Font ResolveUiFont()
    {
        try
        {
            var os = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun",
                "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial"
            }, 32);
            if (os != null) return os;
        }
        catch (System.Exception)
        {
            // 个别环境无上述字体
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
