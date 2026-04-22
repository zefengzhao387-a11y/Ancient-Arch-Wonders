using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 统一字幕：思源宋体；默认<strong>无</strong>渐变黑条与纯黑垫底，用 <see cref="Outline"/> 黑色描边保证可读性。
/// 若需旧版全屏宽渐变黑条，调用 <see cref="ApplyToSubtitle"/> 时设 <c>useGradientBackdrop: true</c>（底条与 Text 同父、先于文字绘制）。
/// </summary>
public static class SubtitleStyleUtility
{
    public const string BackdropChildName = "_SubtitleVGradientBackdrop";
    /// <summary>叠在渐变下方的纯黑条（同尺寸），整体压暗但不糊死边缘。</summary>
    public const string SolidBackdropChildName = "_SubtitleSolidUnderlay";

    static Font _font;
    static Sprite _vGradientSprite;
    static Sprite _whitePixelSprite;

    /// <summary>渐变纹理中心行最大 alpha。</summary>
    const float SubtitleGradientPeakAlpha = 0.94f;
    /// <summary>越大：距纹理竖直中心更远处仍保持较高 alpha，黑带更宽。</summary>
    const float SubtitleGradientSmoothMin = 0.4f;
    const float SubtitleGradientSmoothMax = 0.99f;
    /// <summary>falloff 曲线：小于 1 时中间段更「顶满」，同样峰值下主观更浓。</summary>
    const float SubtitleGradientFalloffGamma = 0.72f;
    /// <summary>纯黑垫底 alpha（与渐变相乘叠色）；淡入淡出时请按「目标 alpha × 系数」缩放。</summary>
    public const float SolidUnderlayTargetAlpha = 0.52f;

    static readonly string[] FontResourcePaths =
    {
        "Fonts/SourceHanSerifSC-Regular",
        "Fonts/SourceHanSerifSC-Medium",
        "Fonts/SourceHanSerifCN-Regular",
        "Fonts/NotoSerifCJKsc-Regular",
        "Fonts/NotoSerifSC-Regular",
    };

    public static Font GetSubtitleFont()
    {
        if (_font != null) return _font;
        foreach (var path in FontResourcePaths)
        {
            var f = Resources.Load<Font>(path);
            if (f != null)
            {
                _font = f;
                return _font;
            }
        }
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _font;
    }

    public static Sprite GetOrCreateVerticalBlackGradientSprite()
    {
        if (_vGradientSprite != null) return _vGradientSprite;
        const int w = 4;
        const int h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float distFromMid = Mathf.Abs(v - 0.5f) * 2f;
            float falloff = 1f - Mathf.SmoothStep(SubtitleGradientSmoothMin, SubtitleGradientSmoothMax, distFromMid);
            if (SubtitleGradientFalloffGamma > 0.001f && Mathf.Abs(SubtitleGradientFalloffGamma - 1f) > 0.001f)
                falloff = Mathf.Pow(Mathf.Clamp01(falloff), SubtitleGradientFalloffGamma);
            float a = Mathf.Clamp01(falloff * SubtitleGradientPeakAlpha);
            var col = new Color(0f, 0f, 0f, a);
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, col);
        }
        tex.Apply(false);
        _vGradientSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return _vGradientSprite;
    }

    public static void ApplySubtitleFont(Text text)
    {
        if (text == null) return;
        var f = GetSubtitleFont();
        if (f != null)
            text.font = f;
    }

    /// <summary>删除与字幕同父节点下的渐变底条（用于某场景只要统一字体、不要黑底）。</summary>
    public static void RemoveSubtitleBackdrop(Text text)
    {
        if (text == null) return;
        var p = text.transform.parent;
        if (p == null) return;
        var t = p.Find(BackdropChildName);
        if (t != null)
            UnityEngine.Object.Destroy(t.gameObject);
        var s = p.Find(SolidBackdropChildName);
        if (s != null)
            UnityEngine.Object.Destroy(s.gameObject);
    }

    /// <summary>仅删除叠在渐变条下的纯黑垫底，保留渐变条本身。</summary>
    public static void RemoveSolidUnderlayOnly(Text text)
    {
        if (text == null) return;
        var p = text.transform.parent;
        if (p == null) return;
        var s = p.Find(SolidBackdropChildName);
        if (s != null)
            UnityEngine.Object.Destroy(s.gameObject);
    }

    static void RemoveSolidUnderlayBesideBar(Image bar)
    {
        if (bar == null) return;
        var p = bar.transform.parent;
        if (p == null) return;
        var s = p.Find(SolidBackdropChildName);
        if (s != null)
            UnityEngine.Object.Destroy(s.gameObject);
    }

    /// <summary>纯黑描边（略加粗，主观更「深」），<c>useGraphicAlpha</c> 与字幕字一起渐隐。</summary>
    public static void EnsureSubtitleBlackOutline(Text text, float distance = 2.55f)
    {
        if (text == null) return;
        var outline = text.GetComponent<Outline>();
        if (outline == null)
            outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 1f);
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    public static void ApplyGradientToBarImage(Image bar, Text layoutFromText = null)
    {
        if (bar == null) return;
        bar.sprite = GetOrCreateVerticalBlackGradientSprite();
        bar.type = Image.Type.Simple;
        bar.color = new Color(1f, 1f, 1f, 1f);
        bar.raycastTarget = false;
        var t = layoutFromText != null ? layoutFromText.rectTransform : bar.GetComponentInChildren<Text>()?.rectTransform;
        if (t != null)
            LayoutFullWidthBarAroundText(bar.rectTransform, t);
        else
            LayoutFullWidthBarBottomFallback(bar.rectTransform);
    }

    /// <summary>
    /// 旧布局把字幕嵌在「条形容器」里且把容器上的 Image 当作字幕条传入时，Layout 会误改整条容器的锚点，把条拉到屏幕中线，与首图/视频脱节。此时应改用自动创建的渐变条（与 Text 同父、兄弟节点）。
    /// </summary>
    public static Image SanitizeSubtitleBarReference(Text text, Image existingBackdrop)
    {
        if (text == null || existingBackdrop == null) return existingBackdrop;
        if (existingBackdrop.gameObject == text.transform.parent.gameObject)
            return null;
        return existingBackdrop;
    }

    /// <param name="useGradientBackdrop">false（默认）：去掉自动/Inspector 条的黑底，仅字 + 黑描边。true：渐变黑条 + 可选纯黑垫底。</param>
    /// <param name="includeSolidUnderlay">仅当 <paramref name="useGradientBackdrop"/> 为 true 时生效。</param>
    public static Image ApplyToSubtitle(Text text, Image existingBackdrop, bool useGradientBackdrop = false, bool includeSolidUnderlay = true)
    {
        if (text == null) return null;
        existingBackdrop = SanitizeSubtitleBarReference(text, existingBackdrop);

        if (!useGradientBackdrop)
        {
            RemoveSubtitleBackdrop(text);
            ApplySubtitleFont(text);
            EnsureSubtitleBlackOutline(text);
            if (existingBackdrop != null)
            {
                RemoveSolidUnderlayBesideBar(existingBackdrop);
                existingBackdrop.enabled = false;
            }
            return existingBackdrop;
        }

        if (!includeSolidUnderlay)
            RemoveSolidUnderlayOnly(text);
        ApplySubtitleFont(text);
        if (existingBackdrop != null)
        {
            existingBackdrop.enabled = true;
            ApplyGradientToBarImage(existingBackdrop, text);
            PlaceBarJustBelowSubtitleText(text, existingBackdrop.transform);
            if (includeSolidUnderlay)
                SyncSolidUnderlay(existingBackdrop.rectTransform);
            return existingBackdrop;
        }
        return EnsureFullWidthGradientBar(text, includeSolidUnderlay);
    }

    static Image EnsureFullWidthGradientBar(Text text, bool includeSolidUnderlay = true)
    {
        var textRt = text.rectTransform;
        Canvas canvas = text.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var parentTf = textRt.parent != null ? textRt.parent : canvas.transform;
        Transform ex = parentTf.Find(BackdropChildName);
        GameObject go;
        RectTransform barRt;
        Image img;
        if (ex != null)
        {
            go = ex.gameObject;
            barRt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
        }
        else
        {
            go = new GameObject(BackdropChildName);
            barRt = go.AddComponent<RectTransform>();
            barRt.SetParent(parentTf, false);
            img = go.AddComponent<Image>();
            img.raycastTarget = false;
        }
        img.sprite = GetOrCreateVerticalBlackGradientSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(1f, 1f, 1f, 1f);
        LayoutFullWidthBarAroundText(barRt, textRt);
        PlaceBarJustBelowSubtitleText(text, barRt);
        if (includeSolidUnderlay)
            SyncSolidUnderlay(barRt);
        return img;
    }

    /// <summary>底条与字幕同父节点，sibling 插在文字前，先画条再画字，且盖在同级全屏底图之上。</summary>
    static void PlaceBarJustBelowSubtitleText(Text text, Transform barTransform)
    {
        if (text == null || barTransform == null) return;
        var textRt = text.rectTransform;
        if (barTransform.parent != textRt.parent)
            barTransform.SetParent(textRt.parent, false);
        barTransform.SetSiblingIndex(textRt.GetSiblingIndex());
    }

    /// <summary>在字幕父节点坐标系内水平拉满父节点，垂直包住字幕（父级一般为全屏 StoryPanel 时即全屏宽）。</summary>
    static void LayoutFullWidthBarAroundText(RectTransform barRt, RectTransform textRt)
    {
        Canvas canvas = textRt.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRt = canvas.GetComponent<RectTransform>();
        var layoutRoot = textRt.parent as RectTransform;
        if (layoutRoot == null)
            layoutRoot = canvasRt;

        barRt.SetParent(layoutRoot, false);

        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(layoutRoot, textRt);
        float hText = b.size.y;
        if (hText < 1f)
            hText = Mathf.Max(24f, textRt.sizeDelta.y);
        float pad = Mathf.Max(44f, hText * 0.72f);
        float minH = layoutRoot.rect.height * 0.135f;
        if (minH < 56f && canvasRt.rect.height > 1f)
            minH = Mathf.Max(minH, canvasRt.rect.height * 0.135f);
        float barH = Mathf.Max(minH, hText + pad);
        float centerY = (b.min.y + b.max.y) * 0.5f;

        barRt.anchorMin = new Vector2(0f, 0.5f);
        barRt.anchorMax = new Vector2(1f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.sizeDelta = new Vector2(0f, barH);
        barRt.anchoredPosition = new Vector2(0f, centerY);
        barRt.localScale = Vector3.one;
    }

    static void LayoutFullWidthBarBottomFallback(RectTransform barRt)
    {
        Canvas canvas = barRt.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRt = canvas.GetComponent<RectTransform>();
        var parentRt = barRt.parent as RectTransform;
        if (parentRt == null)
        {
            barRt.SetParent(canvasRt, false);
            parentRt = canvasRt;
        }
        float barH = Mathf.Max(118f, parentRt.rect.height * 0.17f);
        if (parentRt.rect.height < 1f && canvasRt.rect.height > 1f)
            barH = Mathf.Max(118f, canvasRt.rect.height * 0.17f);
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(1f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.sizeDelta = new Vector2(0f, barH);
        barRt.anchoredPosition = Vector2.zero;
        barRt.localScale = Vector3.one;
    }

    static Sprite GetWhitePixelSprite()
    {
        if (_whitePixelSprite != null) return _whitePixelSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int iy = 0; iy < 4; iy++)
            for (int ix = 0; ix < 4; ix++)
                tex.SetPixel(ix, iy, Color.white);
        tex.Apply(false);
        _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _whitePixelSprite;
    }

    static void MatchRectTransform(RectTransform dst, RectTransform src)
    {
        if (dst == null || src == null) return;
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        dst.pivot = src.pivot;
        dst.sizeDelta = src.sizeDelta;
        dst.anchoredPosition = src.anchoredPosition;
        dst.localScale = src.localScale;
    }

    /// <summary>与渐变条同矩形、叠在其下（ sibling 更前），加深整体不透明度。</summary>
    static void SyncSolidUnderlay(RectTransform gradientRt)
    {
        if (gradientRt == null) return;
        var parentTf = gradientRt.parent;
        if (parentTf == null) return;

        Transform ex = parentTf.Find(SolidBackdropChildName);
        GameObject solidGo;
        RectTransform solidRt;
        Image solidImg;
        if (ex != null)
        {
            solidGo = ex.gameObject;
            solidRt = solidGo.GetComponent<RectTransform>();
            solidImg = solidGo.GetComponent<Image>();
            if (solidImg == null) solidImg = solidGo.AddComponent<Image>();
        }
        else
        {
            solidGo = new GameObject(SolidBackdropChildName);
            solidRt = solidGo.AddComponent<RectTransform>();
            solidRt.SetParent(parentTf, false);
            solidImg = solidGo.AddComponent<Image>();
            solidImg.raycastTarget = false;
        }

        solidImg.sprite = GetWhitePixelSprite();
        solidImg.type = Image.Type.Simple;
        solidImg.color = new Color(0f, 0f, 0f, SolidUnderlayTargetAlpha);
        MatchRectTransform(solidRt, gradientRt);
        solidRt.SetSiblingIndex(gradientRt.GetSiblingIndex());
    }

    /// <summary>与渐变条同步缩放纯黑垫底的 alpha（GameEnding 等逐帧改渐变 Image 时使用）。</summary>
    public static void SyncSolidUnderlayAlphaFromGradient(Image gradientBar, float gradientImageAlpha, float referenceOpaqueAlpha)
    {
        if (gradientBar == null) return;
        var p = gradientBar.transform.parent;
        if (p == null) return;
        var tr = p.Find(SolidBackdropChildName);
        var solid = tr != null ? tr.GetComponent<Image>() : null;
        if (solid == null) return;
        float refA = referenceOpaqueAlpha > 0.001f ? referenceOpaqueAlpha : 1f;
        float u = Mathf.Clamp01(gradientImageAlpha / refA);
        var c = solid.color;
        c.a = SolidUnderlayTargetAlpha * u;
        solid.color = c;
    }
}

/// <summary>
/// VideoPlayer 兼容辅助（与 <see cref="SubtitleStyleUtility"/> 同文件，避免单独 .meta 在部分环境下被忽略导致整类不参与编译）。
/// </summary>
public static class VideoPlaybackUtility
{
    /// <summary>
    /// 将物体提到场景根再 <see cref="Object.DontDestroyOnLoad"/>，避免「仅根物体可持久化」警告。
    /// </summary>
    public static void MarkPersistRoot(GameObject go)
    {
        if (go == null) return;
        if (go.transform.parent != null)
            go.transform.SetParent(null, false);
        Object.DontDestroyOnLoad(go);
    }

    public static string FileUrlFromPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "";
        try
        {
            return new System.Uri(absolutePath).AbsoluteUri;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VideoPlaybackUtility: Uri 失败，回退原始拼接。path={absolutePath} err={e.Message}");
            return "file:///" + absolutePath.Replace("\\", "/");
        }
    }

    public static RenderTexture CreateVideoRenderTexture(int width, int height)
    {
        int w = width <= 0 ? 1920 : width;
        int h = height <= 0 ? 1080 : height;
        w = Mathf.Max(2, (w / 2) * 2);
        h = Mathf.Max(2, (h / 2) * 2);
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };
        rt.Create();
        return rt;
    }

    public static void ApplyStandardCompat(VideoPlayer vp)
    {
        if (vp == null) return;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = false;
    }

    // AI辅助：Google Gemini（2026-04，HEVC/VideoPlayer 报错与用户提示文案讨论）— 以下为人工实现的字符串匹配、链接与发行说明。
    public static void LogVideoError(VideoPlayer vp, string message)
    {
        var id = vp != null
            ? (vp.source == VideoSource.VideoClip && vp.clip != null ? vp.clip.name : vp.url)
            : "?";
        Debug.LogError($"[VideoPlayer] {id} : {message}");

        if (string.IsNullOrEmpty(message)) return;
        var m = message.ToLowerInvariant();
        if (m.Contains("hevc") || m.Contains("hvc1") || m.Contains("h.265") || m.Contains("0xc00d5212")
            || m.Contains("suitable transform"))
        {
            Debug.LogError(
                "[VideoPlayer] 本机无法解码 HEVC(H.265)：未装微软 HEVC 扩展时 Windows 常黑屏/无声。**发行建议：把所有成片用 H.264(AVC) 重编码后再导入 Unity**（不要用 HEVC）。"
                + " 若必须保留 HEVC，可让用户安装： https://www.microsoft.com/p/hevc-video-extensions/9nmzlz57r3t7");
        }
    }

    public static IEnumerator CoWaitFirstFrameOrTimeout(VideoPlayer vp, System.Func<bool> failed, float timeoutSeconds)
    {
        if (vp == null) yield break;
        float t = 0f;
        while (t < timeoutSeconds)
        {
            if (failed != null && failed()) yield break;
            if (vp.frame >= 0) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }
}
