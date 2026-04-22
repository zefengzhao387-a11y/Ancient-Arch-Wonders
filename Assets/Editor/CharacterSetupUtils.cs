using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 角色通用设置：脚下阴影、暖色滤镜
/// 结构：Player（容器）→ FootShadow（先画）→ CharacterBody（后画，挂角色 Image）
/// </summary>
public static class CharacterSetupUtils
{
    /// <summary>
    /// 给 Player 添加脚下阴影和暖色滤镜，并重构为 FootShadow + CharacterBody 结构。
    /// 阴影先画在脚下，角色后画在上方，阴影随角色移动。
    /// </summary>
    /// <param name="player">Player GameObject（有 RectTransform，可有 Image）</param>
    /// <param name="charWidth">角色宽度（用于阴影尺寸）</param>
    /// <param name="charHeight">角色高度</param>
    /// <returns>CharacterBody 的 Image，供控制器引用</returns>
    public static Image AddFootShadowAndWarmTint(GameObject player, float charWidth, float charHeight)
    {
        if (player == null) return null;

        var oldImg = player.GetComponent<Image>();
        var color = oldImg != null ? oldImg.color : new Color(0.3f, 0.5f, 0.8f);
        var sprite = oldImg != null ? oldImg.sprite : null;
        if (oldImg != null) Object.DestroyImmediate(oldImg);

        var shadow = new GameObject("FootShadow");
        shadow.transform.SetParent(player.transform, false);
        shadow.transform.SetAsFirstSibling();
        var shRect = shadow.AddComponent<RectTransform>();
        shRect.anchorMin = new Vector2(0.5f, 0);
        shRect.anchorMax = new Vector2(0.5f, 0);
        shRect.pivot = new Vector2(0.5f, 0.5f);
        shRect.anchoredPosition = new Vector2(0, -4);
        shRect.sizeDelta = new Vector2(charWidth * 1.4f, 18);
        shadow.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        shadow.AddComponent<FootShadow>();

        var body = new GameObject("CharacterBody");
        body.transform.SetParent(player.transform, false);
        body.transform.SetAsLastSibling();
        var bodyRect = body.AddComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = bodyRect.offsetMax = Vector2.zero;
        var bodyImg = body.AddComponent<Image>();
        bodyImg.color = color;
        if (sprite != null) bodyImg.sprite = sprite;

        var warm = body.AddComponent<CharacterWarmTint>();
        SetField(warm, "targetImage", bodyImg);

        return bodyImg;
    }

    static void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }
}
