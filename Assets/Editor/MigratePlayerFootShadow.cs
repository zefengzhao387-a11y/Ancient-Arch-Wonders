using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 迁移工具：为已有场景中的 Player 添加脚下阴影结构，不丢失任何素材和引用。
/// 菜单：Tools -> 迁移场景 -> 为当前场景的 Player 添加脚下阴影
/// </summary>
public static class MigratePlayerFootShadow
{
    [MenuItem("Tools/迁移场景/为当前场景的 Player 添加脚下阴影")]
    public static void MigrateCurrentScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("请先打开要迁移的场景。");
            return;
        }

        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            count += MigratePlayersInHierarchy(root.transform);
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"已迁移 {count} 个 Player，请保存场景 (Ctrl+S)。");
        }
        else
        {
            Debug.Log("未找到需要迁移的 Player（可能已迁移或场景中无 Player）。");
        }
    }

    static int MigratePlayersInHierarchy(Transform root)
    {
        int count = 0;
        var players = root.GetComponentsInChildren<RectTransform>(true);
        foreach (var rt in players)
        {
            var go = rt.gameObject;
            if (go.name != "Player") continue;
            if (MigrateOnePlayer(go)) count++;
        }
        return count;
    }

    /// <returns>true if migrated</returns>
    static bool MigrateOnePlayer(GameObject player)
    {
        var charBody = player.transform.Find("CharacterBody");
        if (charBody != null)
        {
            return false;
        }

        var oldImg = player.GetComponent<Image>();
        var color = oldImg != null ? oldImg.color : new Color(0.3f, 0.5f, 0.8f);
        var sprite = oldImg != null ? oldImg.sprite : null;

        var rect = player.GetComponent<RectTransform>();
        float charWidth = rect != null ? rect.sizeDelta.x : 80f;
        float charHeight = rect != null ? rect.sizeDelta.y : 120f;

        var existingShadow = player.transform.Find("FootShadow");
        GameObject shadow;
        if (existingShadow != null)
        {
            shadow = existingShadow.gameObject;
            shadow.transform.SetAsFirstSibling();
        }
        else
        {
            shadow = new GameObject("FootShadow");
            shadow.transform.SetParent(player.transform, false);
            shadow.transform.SetAsFirstSibling();
            var shRect = shadow.AddComponent<RectTransform>();
            shRect.anchorMin = new Vector2(0.5f, 0);
            shRect.anchorMax = new Vector2(0.5f, 0);
            shRect.pivot = new Vector2(0.5f, 0.5f);
            shRect.anchoredPosition = new Vector2(0, -4);
            shRect.sizeDelta = new Vector2(charWidth * 1.4f, 18);
            var shImg = shadow.AddComponent<Image>();
            shImg.color = new Color(0, 0, 0, 0.35f);
            shImg.raycastTarget = false;
            shadow.AddComponent<FootShadow>();
        }

        var body = new GameObject("CharacterBody");
        body.transform.SetParent(player.transform, false);
        body.transform.SetSiblingIndex(1);
        var bodyRect = body.AddComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = bodyRect.offsetMax = Vector2.zero;
        var bodyImg = body.AddComponent<Image>();
        bodyImg.color = color;
        if (sprite != null) bodyImg.sprite = sprite;

        var warm = body.GetComponent<CharacterWarmTint>();
        if (warm == null) warm = body.AddComponent<CharacterWarmTint>();
        var f = warm.GetType().GetField("targetImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(warm, bodyImg);

        if (oldImg != null)
        {
            UpdateControllerReferences(oldImg, bodyImg);
            Object.DestroyImmediate(oldImg);
        }

        return true;
    }

    static void UpdateControllerReferences(Image oldImg, Image newImg)
    {
        var bridge = Object.FindObjectOfType<Chapter3BridgeController>();
        if (bridge != null)
        {
            var so = new SerializedObject(bridge);
            var prop = so.FindProperty("playerImage");
            if (prop != null && prop.objectReferenceValue == oldImg)
            {
                prop.objectReferenceValue = newImg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
