using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色暖色滤镜：给 2D 角色加极淡暖色，使其更好融入黄昏环境光。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CharacterWarmTint : MonoBehaviour
{
    [Tooltip("暖色 tint，默认极淡暖色，让角色融入黄昏环境光")]
    [SerializeField] private Color warmTint = new Color(1.02f, 0.98f, 0.92f, 1f);

    [SerializeField] private Image targetImage;

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null)
        {
            var c = targetImage.color;
            targetImage.color = new Color(c.r * warmTint.r, c.g * warmTint.g, c.b * warmTint.b, c.a);
        }
    }
}
