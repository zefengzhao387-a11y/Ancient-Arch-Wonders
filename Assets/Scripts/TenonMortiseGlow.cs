using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 榫卯零度时的柔和光晕，由 MeasurementBarController 控制显隐
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TenonMortiseGlow : MonoBehaviour
{
    [SerializeField] private int textureSize = 128;
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.7f);
    [SerializeField] private float centerOpacity = 0.35f;
    [SerializeField] private float falloff = 1.3f;

    private RawImage _rawImage;
    private Texture2D _texture;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        if (_rawImage == null) _rawImage = gameObject.AddComponent<RawImage>();
        _rawImage.raycastTarget = true;
        _rawImage.color = Color.white;

        _texture = CreateRadialGradient();
        _rawImage.texture = _texture;
    }

    private Texture2D CreateRadialGradient()
    {
        var tex = new Texture2D(textureSize, textureSize);
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (x + 0.5f) / textureSize;
                float v = (y + 0.5f) / textureSize;
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

    private void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}
