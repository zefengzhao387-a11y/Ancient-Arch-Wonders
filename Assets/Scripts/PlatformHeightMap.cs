using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 平台高度图：编辑时烘焙 Sprite 轮廓，运行时按轮廓碰撞，物体是啥样空气墙就啥样
/// </summary>
public class PlatformHeightMap : MonoBehaviour
{
    [Tooltip("烘焙的顶面高度，每列一个 0-1 值（底部到顶部）")]
    [SerializeField] public float[] heightMap = new float[0];
    [Tooltip("烘焙的底面高度（头顶碰撞用），每列一个 0-1 值")]
    [SerializeField] public float[] bottomHeightMap = new float[0];
    [Tooltip("每行最左边缘 0-1，用于侧面碰撞，消除空气墙")]
    [SerializeField] public float[] leftMap = new float[0];
    [Tooltip("每行最右边缘 0-1，用于侧面碰撞")]
    [SerializeField] public float[] rightMap = new float[0];

    [SerializeField] public int width;
    [SerializeField] public int height;

    public bool HasData => heightMap != null && heightMap.Length > 0;
    public bool HasEdgeData => leftMap != null && leftMap.Length > 0 && rightMap != null && rightMap.Length > 0;

    private void GetBounds(RectTransform rt, Transform parent, out float left, out float right, out float bottom, out float top)
    {
        rt.GetWorldCorners(_corners);
        left = float.MaxValue; right = float.MinValue; bottom = float.MaxValue; top = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            var p = parent.InverseTransformPoint(_corners[i]);
            if (p.x < left) left = p.x; if (p.x > right) right = p.x;
            if (p.y < bottom) bottom = p.y; if (p.y > top) top = p.y;
        }
    }

    private float SampleHeightMap(float[] map, float u)
    {
        if (map == null || map.Length == 0) return 0;
        float t = u * (map.Length - 1);
        int i0 = Mathf.Clamp((int)t, 0, map.Length - 1);
        int i1 = Mathf.Min(i0 + 1, map.Length - 1);
        return map.Length > 1 ? Mathf.Lerp(map[i0], map[i1], t - i0) : map[0];
    }

    /// <summary>在指定 X（父空间）处获取顶面 Y，返回是否有效</summary>
    public bool TryGetSurfaceY(RectTransform rt, Transform parent, float worldX, out float surfaceY)
    {
        surfaceY = 0;
        if (!HasData || rt == null || parent == null) return false;
        GetBounds(rt, parent, out float left, out float right, out float bottom, out float top);
        if (worldX < left - 2f || worldX > right + 2f) return false;
        float span = right - left;
        if (span < 0.01f) return false;
        float u = Mathf.Clamp01((worldX - left) / span);
        float h = SampleHeightMap(heightMap, u);
        surfaceY = bottom + h * (top - bottom);
        return true;
    }

    /// <summary>在指定 X 处获取底面 Y（头顶碰撞用），返回是否有效</summary>
    public bool TryGetBottomSurfaceY(RectTransform rt, Transform parent, float worldX, out float bottomY)
    {
        bottomY = 0;
        if (bottomHeightMap == null || bottomHeightMap.Length == 0 || rt == null || parent == null) return false;
        GetBounds(rt, parent, out float left, out float right, out float bottom, out float top);
        if (worldX < left - 2f || worldX > right + 2f) return false;
        float span = right - left;
        if (span < 0.01f) return false;
        float u = Mathf.Clamp01((worldX - left) / span);
        float h = SampleHeightMap(bottomHeightMap, u);
        if (h < 0) return false; // -1 = 该列无底面，头顶可穿过
        bottomY = bottom + h * (top - bottom);
        return true;
    }

    /// <summary>在指定 Y（父空间）处获取左边缘 X，用于侧面碰撞，消除空气墙</summary>
    public bool TryGetLeftEdgeX(RectTransform rt, Transform parent, float worldY, out float edgeX)
    {
        edgeX = 0;
        if (!HasEdgeData || rt == null || parent == null) return false;
        GetBounds(rt, parent, out float left, out float right, out float bottom, out float top);
        if (worldY < bottom - 2f || worldY > top + 2f) return false;
        float span = top - bottom;
        if (span < 0.01f) return false;
        float v = Mathf.Clamp01((worldY - bottom) / span);
        float t = v * (leftMap.Length - 1);
        int i0 = Mathf.Clamp((int)t, 0, leftMap.Length - 1);
        int i1 = Mathf.Min(i0 + 1, leftMap.Length - 1);
        float uLeft = leftMap.Length > 1 ? Mathf.Lerp(leftMap[i0], leftMap[i1], t - i0) : leftMap[0];
        float uRight = rightMap.Length > 1 ? Mathf.Lerp(rightMap[i0], rightMap[i1], t - i0) : rightMap[0];
        if (uLeft > uRight + 0.01f) return false; // 该行无实体
        edgeX = left + uLeft * (right - left);
        return true;
    }

    /// <summary>在指定 Y 处获取右边缘 X</summary>
    public bool TryGetRightEdgeX(RectTransform rt, Transform parent, float worldY, out float edgeX)
    {
        edgeX = 0;
        if (!HasEdgeData || rt == null || parent == null) return false;
        GetBounds(rt, parent, out float left, out float right, out float bottom, out float top);
        if (worldY < bottom - 2f || worldY > top + 2f) return false;
        float span = top - bottom;
        if (span < 0.01f) return false;
        float v = Mathf.Clamp01((worldY - bottom) / span);
        float t = v * (rightMap.Length - 1);
        int i0 = Mathf.Clamp((int)t, 0, rightMap.Length - 1);
        int i1 = Mathf.Min(i0 + 1, rightMap.Length - 1);
        float uLeft = leftMap.Length > 1 ? Mathf.Lerp(leftMap[i0], leftMap[i1], t - i0) : leftMap[0];
        float uRight = rightMap.Length > 1 ? Mathf.Lerp(rightMap[i0], rightMap[i1], t - i0) : rightMap[0];
        if (uLeft > uRight + 0.01f) return false;
        edgeX = left + uRight * (right - left);
        return true;
    }

    private static readonly Vector3[] _corners = new Vector3[4];
}
