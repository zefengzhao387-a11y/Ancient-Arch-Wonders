using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 罗盘季节槽：在「视频/静图」矩形四周叠一层柔和、轻微呼吸的动态光（画在 VideoDisplay 下层）。
/// </summary>
[DisallowMultipleComponent]
public class CompassSeasonVideoGlow : MonoBehaviour
{
    [SerializeField] private float expandPixels = 10f;
    [SerializeField] private float pulseSpeed = 1.75f;
    [SerializeField] private float alphaMin = 0.28f;
    [SerializeField] private float alphaMax = 0.62f;
    [SerializeField] private float scalePulse = 0.018f;
    [SerializeField] private Color glowTint = new Color(0.55f, 0.82f, 1f, 1f);

    private RawImage _raw;
    private RectTransform _rawRt;
    private bool _built;
    private static Texture2D _sharedEdgeTex;

    private void OnEnable()
    {
        EnsureBuilt();
        if (_raw != null && !enabled)
            _raw.enabled = false;
    }

    private void OnDisable()
    {
        if (_raw != null)
            _raw.enabled = false;
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        var glowGo = new GameObject("SeasonVideoEdgeGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        _rawRt = glowGo.GetComponent<RectTransform>();
        _rawRt.SetParent(transform, false);
        var videoTr = transform.Find("VideoDisplay");
        if (videoTr != null)
            _rawRt.SetSiblingIndex(videoTr.GetSiblingIndex());
        else
            _rawRt.SetAsFirstSibling();

        _rawRt.anchorMin = Vector2.zero;
        _rawRt.anchorMax = Vector2.one;
        float e = Mathf.Max(0f, expandPixels);
        _rawRt.offsetMin = new Vector2(-e, -e);
        _rawRt.offsetMax = new Vector2(e, e);

        _raw = glowGo.GetComponent<RawImage>();
        _raw.raycastTarget = false;
        _raw.texture = SharedEdgeTexture();
        _raw.color = glowTint;
        _raw.enabled = false;

        _built = true;
    }

    public void SetPulsing(bool on)
    {
        if (!_built) EnsureBuilt();
        enabled = on;
        if (_raw != null) _raw.enabled = on;
    }

    private void Update()
    {
        if (_raw == null || !_raw.enabled) return;
        float t = Time.unscaledTime * pulseSpeed;
        float w = (Mathf.Sin(t) + 1f) * 0.5f;
        float a = Mathf.Lerp(alphaMin, alphaMax, w);
        var c = glowTint;
        c.a = a;
        _raw.color = c;
        float s = 1f + (w - 0.5f) * 2f * scalePulse;
        _rawRt.localScale = new Vector3(s, s, 1f);
    }

    private static Texture2D SharedEdgeTexture()
    {
        if (_sharedEdgeTex != null) return _sharedEdgeTex;
        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                float edgeDist = 2f * Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                float a = Mathf.Clamp01(Mathf.Exp(-edgeDist * 2.85f) - 0.02f);
                a = Mathf.SmoothStep(0f, 1f, a * 1.15f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * 0.98f));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        _sharedEdgeTex = tex;
        return _sharedEdgeTex;
    }
}
