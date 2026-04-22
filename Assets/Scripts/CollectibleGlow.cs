using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可拾取物发光特效：木构件等收集物添加柔和光晕，让玩家知道要拣
/// 不修改原有层级，仅在物体后方添加光晕子物体
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CollectibleGlow : MonoBehaviour
{
    [Header("发光")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private float centerOpacity = 0.75f;
    [SerializeField] private float falloff = 1.1f;
    [Tooltip("光晕比物体大多少倍")]
    [SerializeField] private float glowScale = 2.2f;
    [Tooltip("是否呼吸闪烁")]
    [SerializeField] private bool pulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.4f;
    [SerializeField] private float pulseMax = 0.8f;

    private RawImage _glowImage;
    private Texture2D _texture;
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        if (this == null || transform == null) return;
        SetupGlow();
    }

    private void OnEnable()
    {
        if (pulse && _glowImage != null)
            _pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void OnDisable()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    private void SetupGlow()
    {
        try
        {
            if (transform == null) return;
            Transform glowGo = transform.Find("CollectibleGlow");
            if (glowGo == null)
            {
                var go = new GameObject("CollectibleGlow");
                if (go == null) return;
                glowGo = go.transform;
                glowGo.SetParent(transform, false);
                glowGo.SetAsLastSibling();
            }
            if (glowGo == null) return;

            var rt = GetComponent<RectTransform>();
            var glowRect = glowGo.GetComponent<RectTransform>();
            if (glowRect == null) glowRect = glowGo.gameObject.AddComponent<RectTransform>();
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = rt != null ? rt.sizeDelta * glowScale : new Vector2(90, 90);

            _glowImage = glowGo.GetComponent<RawImage>();
            if (_glowImage == null) _glowImage = glowGo.gameObject.AddComponent<RawImage>();
            _glowImage.raycastTarget = false;
            _glowImage.color = new Color(1f, 1f, 1f, 0.9f);

            _texture = CreateRadialGradient();
            if (_texture != null) _glowImage.texture = _texture;
        }
        catch (UnityEngine.MissingReferenceException)
        {
            // 对象已销毁时跳过，避免报错
        }
    }

    private Texture2D CreateRadialGradient()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                float dist = Vector2.Distance(new Vector2(u, v), center) * 2f;
                float alpha = centerOpacity * Mathf.Clamp01(1f - Mathf.Pow(dist, falloff));
                var c = glowColor;
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    private IEnumerator PulseRoutine()
    {
        float t = 0;
        while (true)
        {
            yield return null;
            if (this == null || _glowImage == null) yield break;
            t += Time.deltaTime * pulseSpeed;
            float a = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(t) + 1f) * 0.5f);
            var c = _glowImage.color;
            c.a = a;
            _glowImage.color = c;
        }
    }

    private void OnDestroy()
    {
        if (_texture != null)
        {
            Destroy(_texture);
            _texture = null;
        }
    }
}
