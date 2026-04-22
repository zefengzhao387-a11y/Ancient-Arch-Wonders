using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 运行时统一 UI.Text 字体，避免部分场景仍使用内置 Arial。
/// 同时将 TMP 文本切到运行时创建的动态 TMP FontAsset。
/// </summary>
public static class RuntimeUIFontNormalizer
{
    private static TMP_FontAsset _tmpFontAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        Font target = SubtitleStyleUtility.GetSubtitleFont();
        if (target == null) return;
        TMP_FontAsset tmpTarget = GetOrCreateTmpFontAsset(target);

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            var texts = roots[i].GetComponentsInChildren<Text>(true);
            for (int j = 0; j < texts.Length; j++)
            {
                var t = texts[j];
                if (t == null) continue;
                if (t.font != target)
                    t.font = target;
            }

            if (tmpTarget != null)
            {
                var tmpTexts = roots[i].GetComponentsInChildren<TMP_Text>(true);
                for (int j = 0; j < tmpTexts.Length; j++)
                {
                    var t = tmpTexts[j];
                    if (t == null) continue;
                    if (t.font != tmpTarget)
                        t.font = tmpTarget;
                }
            }
        }
    }

    private static TMP_FontAsset GetOrCreateTmpFontAsset(Font targetFont)
    {
        if (targetFont == null) return null;
        if (_tmpFontAsset != null) return _tmpFontAsset;
        try
        {
            _tmpFontAsset = TMP_FontAsset.CreateFontAsset(targetFont);
            _tmpFontAsset.name = "Runtime_SourceHanSerifSC_Dynamic";
            return _tmpFontAsset;
        }
        catch
        {
            return null;
        }
    }
}
