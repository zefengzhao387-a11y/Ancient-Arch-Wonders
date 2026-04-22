using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色脚下阴影：生成半透明黑色椭圆贴图，增强接地感。
/// 挂到 Player 下的 FootShadow 子物体（带 Image 组件）上。
/// </summary>
[RequireComponent(typeof(Image))]
public class FootShadow : MonoBehaviour
{
    /// <summary>
    /// 从 Player 或其子物体中获取角色 Image（排除 FootShadow）
    /// </summary>
    public static Image GetCharacterImage(Transform playerRoot)
    {
        if (playerRoot == null) return null;
        foreach (var img in playerRoot.GetComponentsInChildren<Image>(true))
        {
            if (img.GetComponent<FootShadow>() == null) return img;
        }
        return null;
    }

    [Tooltip("阴影宽度（相对角色宽度的倍数）")]
    [SerializeField] private float widthScale = 1.4f;
    [Tooltip("阴影高度（像素）")]
    [SerializeField] private float height = 18f;
    [Tooltip("阴影透明度 0-1")]
    [Range(0f, 1f)]
    [SerializeField] private float alpha = 0.35f;

    void Awake()
    {
        var img = GetComponent<Image>();
        if (img == null) return;
        var rt = transform as RectTransform;
        if (rt != null && height > 0.01f)
        {
            float w = rt.sizeDelta.x > 0.01f ? rt.sizeDelta.x * widthScale : 64f * widthScale;
            rt.sizeDelta = new Vector2(Mathf.Max(8f, w), height);
        }
        img.sprite = CreateSoftEllipseSprite();
        img.color = new Color(0, 0, 0, alpha);
        img.raycastTarget = false;
    }

    static Sprite CreateSoftEllipseSprite()
    {
        int w = 64;
        int h = 32;
        var tex = new Texture2D(w, h);
        var pixels = new Color32[w * h];
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float rx = cx * 0.95f;
        float ry = cy * 0.95f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            float d = dx * dx + dy * dy;
            float a = d <= 1f ? Mathf.Clamp01(1f - d * d) : 0f;
            pixels[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}
