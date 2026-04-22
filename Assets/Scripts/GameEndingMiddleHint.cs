using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结尾「中间插图」阶段（倒数第二张静图）：叠一层动态光圈、指向画面焦点的箭头与「点击查看」提示。
/// 点击推进仍由 <see cref="GameEndingController"/> 的全屏按钮负责；本组件仅负责表现与引导。
/// </summary>
[DisallowMultipleComponent]
public class GameEndingMiddleHint : MonoBehaviour
{
    [Header("布局（相对 MiddleStillPhase 全屏，可对准插图里的「盒子」与文字）")]
    [SerializeField] private Vector2 focusAnchoredPosition = new Vector2(40f, 28f);
    [SerializeField] private Vector2 haloSize = new Vector2(300f, 300f);
    [Tooltip("箭头相对焦点的偏移，箭头落在外侧，指向焦点")]
    [SerializeField] private Vector2 arrowOffsetFromFocus = new Vector2(200f, -8f);
    [Tooltip("「点击查看」相对箭头位置的偏移")]
    [SerializeField] private Vector2 hintTextOffsetFromArrow = new Vector2(88f, -44f);

    [Header("动画")]
    [SerializeField] private float haloPulseSpeed = 1.55f;
    [SerializeField] private float haloScaleAmplitude = 0.08f;
    [SerializeField] private float haloAlphaAmplitude = 0.2f;
    [SerializeField] private float arrowBobSpeed = 2.15f;
    [SerializeField] private float arrowBobPixels = 16f;
    [SerializeField] private string hintString = "点击查看";

    private RawImage _halo;
    private RectTransform _haloRt;
    private RectTransform _arrowRt;
    private Text _arrowGlyph;
    private Text _hintText;
    private RectTransform _hintRt;
    private RectTransform _focusRt;
    private Texture2D _ringTex;
    private Vector2 _arrowBaseAnchored;
    private Color _haloBaseColor;
    private bool _built;

    private void OnEnable()
    {
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        var root = transform as RectTransform;
        if (root == null) return;

        var panel = new GameObject("MiddleClickHint", typeof(RectTransform));
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.SetParent(root, false);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panelRt.SetAsLastSibling();

        var focusGo = new GameObject("FocusPoint", typeof(RectTransform));
        _focusRt = focusGo.GetComponent<RectTransform>();
        _focusRt.SetParent(panelRt, false);
        _focusRt.anchorMin = _focusRt.anchorMax = new Vector2(0.5f, 0.5f);
        _focusRt.pivot = new Vector2(0.5f, 0.5f);
        _focusRt.anchoredPosition = focusAnchoredPosition;
        _focusRt.sizeDelta = Vector2.zero;

        var haloGo = new GameObject("HaloRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        _haloRt = haloGo.GetComponent<RectTransform>();
        _haloRt.SetParent(panelRt, false);
        _haloRt.anchorMin = _haloRt.anchorMax = new Vector2(0.5f, 0.5f);
        _haloRt.pivot = new Vector2(0.5f, 0.5f);
        _haloRt.anchoredPosition = focusAnchoredPosition;
        _haloRt.sizeDelta = haloSize;
        _halo = haloGo.GetComponent<RawImage>();
        _halo.raycastTarget = false;
        _ringTex = BuildRingTexture(128);
        _halo.texture = _ringTex;
        _haloBaseColor = new Color(1f, 0.9f, 0.5f, 0.9f);
        _halo.color = _haloBaseColor;

        var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        _arrowRt = arrowGo.GetComponent<RectTransform>();
        _arrowRt.SetParent(panelRt, false);
        _arrowRt.anchorMin = _arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        _arrowRt.pivot = new Vector2(0.5f, 0.5f);
        _arrowBaseAnchored = focusAnchoredPosition + arrowOffsetFromFocus;
        _arrowRt.anchoredPosition = _arrowBaseAnchored;
        _arrowRt.sizeDelta = new Vector2(80f, 80f);
        _arrowGlyph = arrowGo.GetComponent<Text>();
        _arrowGlyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _arrowGlyph.text = "➤";
        _arrowGlyph.fontSize = 58;
        _arrowGlyph.color = new Color(1f, 0.82f, 0.15f, 1f);
        _arrowGlyph.alignment = TextAnchor.MiddleCenter;
        _arrowGlyph.raycastTarget = false;

        var hintGo = new GameObject("HintClickLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        _hintRt = hintGo.GetComponent<RectTransform>();
        _hintRt.SetParent(panelRt, false);
        _hintRt.anchorMin = _hintRt.anchorMax = new Vector2(0.5f, 0.5f);
        _hintRt.pivot = new Vector2(0f, 0.5f);
        _hintRt.anchoredPosition = _arrowBaseAnchored + hintTextOffsetFromArrow;
        _hintRt.sizeDelta = new Vector2(240f, 56f);
        _hintText = hintGo.GetComponent<Text>();
        _hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hintText.text = hintString;
        _hintText.fontSize = 26;
        _hintText.color = new Color(1f, 1f, 1f, 0.92f);
        _hintText.alignment = TextAnchor.MiddleLeft;
        _hintText.raycastTarget = false;
        SubtitleStyleUtility.ApplyToSubtitle(_hintText, null);
        if (_arrowGlyph != null)
            SubtitleStyleUtility.ApplySubtitleFont(_arrowGlyph);

        AimArrowTowardFocus();

        _built = true;
    }

    private void AimArrowTowardFocus()
    {
        if (_focusRt == null || _arrowRt == null) return;
        Vector2 toFocus = _focusRt.anchoredPosition - _arrowRt.anchoredPosition;
        if (toFocus.sqrMagnitude < 4f) return;
        float z = Mathf.Atan2(toFocus.y, toFocus.x) * Mathf.Rad2Deg;
        _arrowRt.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private void LateUpdate()
    {
        if (!_built || _haloRt == null || _arrowRt == null || _focusRt == null || _hintRt == null) return;

        // Inspector 里改焦点/光圈大小/偏移时立刻生效（否则只在首次生成时读一次，调数字光圈不动）
        _focusRt.anchoredPosition = focusAnchoredPosition;
        _haloRt.anchoredPosition = focusAnchoredPosition;
        _haloRt.sizeDelta = haloSize;
        _arrowBaseAnchored = focusAnchoredPosition + arrowOffsetFromFocus;
        if (_hintText != null && _hintText.text != hintString)
            _hintText.text = hintString;

        float t = Time.unscaledTime;
        float pulse = (Mathf.Sin(t * haloPulseSpeed) + 1f) * 0.5f;
        float scale = 1f + (pulse - 0.5f) * 2f * haloScaleAmplitude;
        _haloRt.localScale = new Vector3(scale, scale, 1f);
        var hc = _haloBaseColor;
        hc.a = Mathf.Clamp01(_haloBaseColor.a + (pulse - 0.5f) * 2f * haloAlphaAmplitude);
        _halo.color = hc;

        Vector2 focus = _focusRt.anchoredPosition;
        Vector2 dir = (focus - _arrowBaseAnchored).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;
        float bob = Mathf.Sin(t * arrowBobSpeed) * arrowBobPixels;
        _arrowRt.anchoredPosition = _arrowBaseAnchored + dir * bob;

        AimArrowTowardFocus();

        _hintRt.anchoredPosition = _arrowRt.anchoredPosition + hintTextOffsetFromArrow;
        var tc = _hintText.color;
        tc.a = 0.7f + 0.3f * ((Mathf.Sin(t * 2.05f) + 1f) * 0.5f);
        _hintText.color = tc;
    }

    private void OnDestroy()
    {
        if (_ringTex != null)
        {
            Destroy(_ringTex);
            _ringTex = null;
        }
    }

    private static Texture2D BuildRingTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = Color.white;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f - 1.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxR;
                float inner = 0.3f, outer = 0.64f;
                float band = Mathf.SmoothStep(inner, inner + 0.07f, d) * (1f - Mathf.SmoothStep(outer - 0.07f, outer, d));
                float glow = Mathf.Exp(-Mathf.Pow((d - 0.45f) / 0.13f, 2f)) * 0.92f + band * 0.5f;
                px.a = Mathf.Clamp01(glow);
                px.r = px.g = px.b = 1f;
                tex.SetPixel(x, y, px);
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
