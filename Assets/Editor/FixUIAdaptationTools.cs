using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class FixUIAdaptationTools
{
    private static readonly Vector2 TargetResolution = new Vector2(1920f, 1080f);
    private const float MatchWidthOrHeight = 0.5f;

    [MenuItem("Tools/UI适配/修复当前场景Canvas缩放")]
    public static void FixCurrentSceneCanvasScaling()
    {
        int changed = FixSceneCanvasScaling(true);
        if (changed == 0)
            Debug.LogWarning("当前场景未找到 CanvasScaler。");
        else
            Debug.Log($"UI适配修复完成：已处理 {changed} 个 CanvasScaler。");
    }

    [MenuItem("Tools/UI适配/批量修复Assets/Scenes全部场景")]
    public static void FixAllScenesCanvasScaling()
    {
        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        if (sceneGuids == null || sceneGuids.Length == 0)
        {
            Debug.LogWarning("未在 Assets/Scenes 下找到场景文件。");
            return;
        }

        int sceneCount = 0;
        int totalChanged = 0;
        foreach (var guid in sceneGuids)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(scenePath)) continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int changed = FixSceneCanvasScaling(false);
            if (changed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                totalChanged += changed;
            }

            sceneCount++;
        }

        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        Debug.Log($"批量UI适配修复完成：已扫描 {sceneCount} 个场景，累计处理 {totalChanged} 个 CanvasScaler。");
    }

    private static int FixSceneCanvasScaling(bool useUndo)
    {
        var scalers = Object.FindObjectsOfType<CanvasScaler>(true);
        int changed = 0;

        foreach (var scaler in scalers)
        {
            if (scaler == null) continue;

            if (useUndo) Undo.RecordObject(scaler, "Fix Canvas Scaler Adaptation");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = TargetResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;
            EditorUtility.SetDirty(scaler);
            changed++;
        }

        foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
        {
            if (canvas == null) continue;
            var rt = canvas.transform as RectTransform;
            if (rt == null || rt.localScale == Vector3.one) continue;

            if (useUndo) Undo.RecordObject(rt, "Normalize Canvas Scale");
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return changed;
    }
}
