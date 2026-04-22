using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 让卢沟桥等线稿桥体略偏暖灰：仅对 Image.color 的 RGB 与基准色相乘，不改变 Alpha；不创建雾带子物体。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InkWashBridgeBlend : MonoBehaviour
{
    const string LegacyStripChildName = "InkWashBottomFog";

    [SerializeField] private Image targetImage;

    [Header("整体")]
    [Tooltip("与基准色的 RGB 相乘；本字段的 Alpha 不参与运算，Image 的 Alpha 始终等于基准色")]
    [SerializeField] private Color tintMultiply = new Color(0.94f, 0.92f, 0.90f, 1f);

    [SerializeField] private bool applyTint = true;

    private Color _capturedBaseColor = Color.white;
    private bool _captured;

    void Awake()
    {
        RemoveLegacyFogChild();
        Apply();
    }

    void OnEnable()
    {
        RemoveLegacyFogChild();
        Apply();
    }

    [ContextMenu("Rebuild ink blend")]
    public void Apply()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        if (targetImage == null) return;

        if (!_captured)
        {
            _capturedBaseColor = targetImage.color;
            _captured = true;
        }

        if (applyTint)
        {
            var o = _capturedBaseColor;
            var t = tintMultiply;
            targetImage.color = new Color(o.r * t.r, o.g * t.g, o.b * t.b, o.a);
        }
        else
            targetImage.color = _capturedBaseColor;
    }

    [ContextMenu("Recapture base color from Image")]
    public void RecaptureBaseColorFromImage()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        if (targetImage == null) return;
        _capturedBaseColor = targetImage.color;
        _captured = true;
        Apply();
    }

    /// <summary>移除旧版本生成的雾带子物体（若仍存在）。</summary>
    void RemoveLegacyFogChild()
    {
        var t = transform.Find(LegacyStripChildName);
        if (t == null) return;
        if (Application.isPlaying)
            Destroy(t.gameObject);
        else
            DestroyImmediate(t.gameObject);
    }

}
