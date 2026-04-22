using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[CustomEditor(typeof(PlatformHeightMap))]
public class PlatformHeightMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var comp = (PlatformHeightMap)target;
        var rt = comp.GetComponent<RectTransform>();
        var img = comp.GetComponent<Image>();
        if (img == null) img = comp.GetComponentInChildren<Image>();
        if (GUILayout.Button("烘焙高度图（从 Sprite）"))
        {
            if (img == null || img.sprite == null) { Debug.LogWarning("平台需有 Image 且已拖入 Sprite"); return; }
            Bake(comp, img.sprite);
        }
    }

    public static void BakeStatic(PlatformHeightMap comp, Sprite sprite) => Bake(comp, sprite);

    static void Bake(PlatformHeightMap comp, Sprite sprite)
    {
        var tex = sprite.texture as Texture2D;
        if (tex == null) { Debug.LogWarning("无法获取纹理"); return; }
        string path = AssetDatabase.GetAssetPath(tex);
        if ((string.IsNullOrEmpty(path) || path.StartsWith("Packages/")) && !tex.isReadable)
        {
            Debug.LogWarning("无法修改该纹理（内置或包内资源），请使用单独导入的平台图并勾选 Read/Write");
            return;
        }
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        bool wasReadable = imp != null && imp.isReadable;
        if (imp != null && !imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
        try
        {
            var rect = sprite.rect;
            int w = Mathf.Max(1, (int)rect.width);
            int h = Mathf.Max(1, (int)rect.height);
            var heights = new float[w];
            var bottomHeights = new float[w];
            var leftMap = new float[h];
            var rightMap = new float[h];

            for (int x = 0; x < w; x++)
            {
                int texX = (int)(rect.x + (x + 0.5f) / w * rect.width);
                int yMin = (int)rect.y;
                int yMax = (int)rect.yMax;
                heights[x] = 0;
                bottomHeights[x] = -1f;
                for (int y = yMax - 1; y >= yMin; y--)
                {
                    int tx = Mathf.Clamp(texX, (int)rect.x, (int)rect.xMax - 1);
                    if (tex.GetPixel(tx, y).a > 0.15f)
                    {
                        heights[x] = (y - yMin) / rect.height;
                        break;
                    }
                }
                for (int y = yMin; y < yMax; y++)
                {
                    int tx = Mathf.Clamp(texX, (int)rect.x, (int)rect.xMax - 1);
                    if (tex.GetPixel(tx, y).a > 0.15f)
                    {
                        bottomHeights[x] = (y - yMin) / rect.height;
                        break;
                    }
                }
            }

            for (int row = 0; row < h; row++)
            {
                int texY = (int)(rect.y + (row + 0.5f) / h * rect.height);
                leftMap[row] = 1f;
                rightMap[row] = 0f;
                for (int col = 0; col < w; col++)
                {
                    int texX = (int)(rect.x + (col + 0.5f) / w * rect.width);
                    int tx = Mathf.Clamp(texX, (int)rect.x, (int)rect.xMax - 1);
                    int ty = Mathf.Clamp(texY, (int)rect.y, (int)rect.yMax - 1);
                    if (tex.GetPixel(tx, ty).a > 0.15f)
                    {
                        float u = (col + 0.5f) / w;
                        if (u < leftMap[row]) leftMap[row] = u;
                        if (u > rightMap[row]) rightMap[row] = u;
                    }
                }
            }

            comp.heightMap = heights;
            comp.bottomHeightMap = bottomHeights;
            comp.leftMap = leftMap;
            comp.rightMap = rightMap;
            comp.width = w;
            comp.height = h;
            EditorUtility.SetDirty(comp);
            Debug.Log($"已烘焙 {w}x{h} 轮廓（顶底+左右边缘），物体是啥样空气墙就啥样");
        }
        finally
        {
            if (imp != null && !wasReadable) { imp.isReadable = false; imp.SaveAndReimport(); }
        }
    }
}
