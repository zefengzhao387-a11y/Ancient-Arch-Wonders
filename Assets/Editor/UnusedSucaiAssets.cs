using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将「未被工程使用」的 Assets/素材 资源移到 Assets/素材_未使用。
/// 判定：以「素材」目录之外的 YAML 为种子，再迭代扩张引用闭包（避免漏掉仅被素材内 Material 引用的贴图）。
/// GUID 统一用 AssetDatabase.AssetPathToGUID，兼容 .meta 为 base64 而 .scene 为 32 位 hex 的情况。
/// </summary>
public static class UnusedSucaiAssets
{
    const string Sucai = "Assets/素材";
    const string DestRoot = "Assets/素材_未使用";

    static readonly HashSet<string> ScanExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        ".meta", ".cs", ".unity", ".scene", ".prefab", ".asset", ".mat", ".controller", ".anim",
        ".spriteatlas", ".shader", ".compute", ".json", ".asmdef", ".inputactions", ".playable",
        ".overrideController", ".physicMaterial", ".physicsMaterial2D", ".preset", ".mixer", ".signal",
    };

    static readonly Regex GuidHex = new Regex(@"guid:\s*([a-f0-9]{32})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [MenuItem("Tools/素材/Report Unused (Console)")]
    static void ReportUnused()
    {
        var used = CollectUsedGuidsTransitive();
        int unused = 0;
        foreach (var path in EnumerateSucaiAssetPaths())
        {
            var g = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(g)) continue;
            if (used.Contains(NormalizeGuid(g))) continue;
            Debug.Log($"UNUSED: {path}", AssetDatabase.LoadMainAssetAtPath(path));
            unused++;
        }
        Debug.Log($"[UnusedSucai] Used guids (closure): {used.Count}. Unused file assets under 素材: {unused}");
    }

    [MenuItem("Tools/素材/Move Unused To 素材_未使用 (Dry Run)")]
    static void MoveDryRun()
    {
        var used = CollectUsedGuidsTransitive();
        int n = 0;
        foreach (var path in EnumerateSucaiAssetPaths())
        {
            var g = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(g)) continue;
            if (used.Contains(NormalizeGuid(g))) continue;
            var rel = path.Substring(Sucai.Length).TrimStart('/');
            Debug.Log($"[DryRun] would move → {DestRoot}/{rel}");
            n++;
        }
        Debug.Log($"[UnusedSucai] Dry run: {n} files (+ .meta via MoveAsset).");
    }

    [MenuItem("Tools/素材/Move Unused To 素材_未使用")]
    static void MoveExecute()
    {
        if (!EditorUtility.DisplayDialog(
                "移动未使用素材",
                "将把「未被工程引用闭包包含」的 Assets/素材 下文件移到 Assets/素材_未使用。\n\n建议先提交 Git 或备份。是否继续？",
                "继续",
                "取消"))
            return;

        var used = CollectUsedGuidsTransitive();
        var toMove = new List<string>();
        foreach (var path in EnumerateSucaiAssetPaths())
        {
            var g = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(g)) continue;
            if (used.Contains(NormalizeGuid(g))) continue;
            toMove.Add(path);
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in toMove)
            {
                var rel = path.Substring(Sucai.Length).TrimStart('/');
                var dest = $"{DestRoot}/{rel}";
                var destParent = Path.GetDirectoryName(dest)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(destParent))
                    EnsureFolderExists(destParent);
                var err = AssetDatabase.MoveAsset(path, dest);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogError($"MoveAsset failed: {path} → {dest}\n{err}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[UnusedSucai] Moved {toMove.Count} assets to {DestRoot}.");
    }

    /// <summary>
    /// 种子：相对 Assets 路径既不在 素材/ 也不在 素材_未使用/ 下的文本资源中出现的 guid；
    /// 闭包：任意 Assets 内（除 素材_未使用）文本文件中，若出现已集合中的 guid，则把该文件内全部 guid 并入集合，直到不动点。
    /// </summary>
    static HashSet<string> CollectUsedGuidsTransitive()
    {
        var dataPath = Application.dataPath.Replace("\\", "/");
        var fileToGuids = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var full in Directory.GetFiles(dataPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(full);
            if (!ScanExtensions.Contains(ext)) continue;
            var rel = full.Substring(dataPath.Length).TrimStart('/', '\\').Replace("\\", "/");
            if (rel.StartsWith("素材_未使用/", System.StringComparison.OrdinalIgnoreCase)) continue;

            string text;
            try { text = File.ReadAllText(full); }
            catch { continue; }

            var list = new List<string>();
            foreach (Match m in GuidHex.Matches(text))
                list.Add(NormalizeGuid(m.Groups[1].Value));
            if (list.Count == 0) continue;
            fileToGuids[rel] = list;
        }

        var used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kv in fileToGuids)
        {
            if (kv.Key.StartsWith("素材/", System.StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var g in kv.Value)
                used.Add(g);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var kv in fileToGuids)
            {
                if (kv.Key.StartsWith("素材_未使用/", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!kv.Value.Any(used.Contains)) continue;
                foreach (var g in kv.Value)
                    if (used.Add(g)) changed = true;
            }
        }

        return used;
    }

    static string NormalizeGuid(string g) => string.IsNullOrEmpty(g) ? "" : g.Trim().ToLowerInvariant();

    static IEnumerable<string> EnumerateSucaiAssetPaths()
    {
        var baseFs = Path.Combine(Application.dataPath, "素材");
        if (!Directory.Exists(baseFs)) yield break;

        foreach (var full in Directory.GetFiles(baseFs, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(full);
            if (name.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

            var relFs = full.Substring(baseFs.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var assetPath = $"{Sucai}/{relFs.Replace("\\", "/")}";
            if (AssetDatabase.IsValidFolder(assetPath)) continue;
            yield return assetPath;
        }
    }

    static void EnsureFolderExists(string assetPath)
    {
        assetPath = assetPath.Replace("\\", "/").TrimEnd('/');
        if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath)) return;

        var parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        var name = Path.GetFileName(assetPath);
        if (!string.IsNullOrEmpty(parent) && parent != "Assets")
            EnsureFolderExists(parent);

        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            var p = string.IsNullOrEmpty(parent) ? "Assets" : parent;
            AssetDatabase.CreateFolder(p, name);
        }
    }
}
