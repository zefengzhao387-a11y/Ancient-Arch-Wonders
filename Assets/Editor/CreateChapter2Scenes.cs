using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 创建第二章场景：Tools -> 创建第二章场景
/// </summary>
public static class CreateChapter2Scenes
{
    static GameObject CreateCanvas()
    {
        var go = new GameObject("Canvas");
        go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void CreateFadeOverlay(Transform parent)
    {
        var go = new GameObject("FadeOverlay");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 1);
        img.raycastTarget = false;
    }

    static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetFullRect(GameObject go)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }

    [MenuItem("Tools/创建第二章场景/0.第二章插图")]
    public static void CreateChapter2Title()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var area = CreateChild(canvas.transform, "Area");
        SetFullRect(area);

        var bg = CreateChild(area.transform, "Background");
        SetFullRect(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.12f, 0.1f);

        var illu = CreateChild(area.transform, "Illustration");
        SetFullRect(illu);
        var illuImg = illu.AddComponent<Image>();
        illuImg.color = new Color(0.25f, 0.2f, 0.18f);

        var title = CreateChild(area.transform, "TitleText");
        var titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400, 120);
        var titleTxt = title.AddComponent<Text>();
        titleTxt.text = "第二章";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 72;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.9f, 0.85f, 0.75f);

        var nextBtn = CreateChild(area.transform, "NextChapterButton");
        var nextBtnRect = nextBtn.AddComponent<RectTransform>();
        nextBtnRect.anchorMin = new Vector2(0.5f, 0.1f);
        nextBtnRect.anchorMax = new Vector2(0.5f, 0.1f);
        nextBtnRect.pivot = new Vector2(0.5f, 0.5f);
        nextBtnRect.anchoredPosition = Vector2.zero;
        nextBtnRect.sizeDelta = new Vector2(320, 60);
        var nextBtnImg = nextBtn.AddComponent<Image>();
        nextBtnImg.color = new Color(0.4f, 0.35f, 0.3f);
        var nextBtnComp = nextBtn.AddComponent<Button>();
        var nextBtnTxtGo = CreateChild(nextBtn.transform, "Text");
        SetFullRect(nextBtnTxtGo);
        var nextBtnTxt = nextBtnTxtGo.AddComponent<Text>();
        nextBtnTxt.text = "进入下一站章节";
        nextBtnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nextBtnTxt.fontSize = 32;
        nextBtnTxt.alignment = TextAnchor.MiddleCenter;
        nextBtnTxt.color = Color.white;

        var clickArea = CreateChild(canvas.transform, "ClickArea");
        SetFullRect(clickArea);
        var clickImg = clickArea.AddComponent<Image>();
        clickImg.color = new Color(0, 0, 0, 0);
        var clickBtn = clickArea.AddComponent<Button>();
        clickBtn.targetGraphic = clickImg;

        var ctrl = canvas.AddComponent<Chapter2TitleController>();
        clickBtn.onClick.AddListener(ctrl.OnClickArea);
        SetField(ctrl, "illustrationImage", illuImg);
        SetField(ctrl, "titleText", titleTxt);
        SetField(ctrl, "nextChapterButton", nextBtnComp);
        SetField(ctrl, "nextChapterButtonText", nextBtnTxt);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "nextSceneName", "Chapter2Intro");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2Title.scene");
        var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        var path = "Assets/Scenes/Chapter2Title.scene";
        if (!list.Exists(x => x.path == path))
        {
            int idx = list.FindIndex(x => x.path.Contains("Chapter2Intro"));
            list.Insert(idx >= 0 ? idx : list.Count, new UnityEditor.EditorBuildSettingsScene(path, true));
            UnityEditor.EditorBuildSettings.scenes = list.ToArray();
        }
        Debug.Log("第二章插图已创建。请将插图拖到 Illustration 的 Source Image，或替换 Background。将第一章最后一场景的 nextSceneName 改为 Chapter2Title。");
    }

    [MenuItem("Tools/创建第二章场景/为 Chapter2Title 添加进入下一站章节按钮")]
    public static void AddNextChapterButtonToChapter2Title()
    {
        var area = GameObject.Find("Area");
        if (area == null) { Debug.LogWarning("请先打开 Chapter2Title 场景，且存在 Area 对象。"); return; }
        if (area.transform.Find("NextChapterButton") != null) { Debug.Log("NextChapterButton 已存在。"); return; }

        var nextBtn = CreateChild(area.transform, "NextChapterButton");
        var nextBtnRect = nextBtn.AddComponent<RectTransform>();
        nextBtnRect.anchorMin = new Vector2(0.5f, 0.1f);
        nextBtnRect.anchorMax = new Vector2(0.5f, 0.1f);
        nextBtnRect.pivot = new Vector2(0.5f, 0.5f);
        nextBtnRect.anchoredPosition = Vector2.zero;
        nextBtnRect.sizeDelta = new Vector2(320, 60);
        nextBtn.AddComponent<Image>().color = new Color(0.4f, 0.35f, 0.3f);
        var nextBtnComp = nextBtn.AddComponent<Button>();
        var nextBtnTxtGo = CreateChild(nextBtn.transform, "Text");
        SetFullRect(nextBtnTxtGo);
        var nextBtnTxt = nextBtnTxtGo.AddComponent<Text>();
        nextBtnTxt.text = "进入下一站章节";
        nextBtnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nextBtnTxt.fontSize = 32;
        nextBtnTxt.alignment = TextAnchor.MiddleCenter;
        nextBtnTxt.color = Color.white;

        var ctrl = Object.FindObjectOfType<Chapter2TitleController>();
        if (ctrl != null)
        {
            SetField(ctrl, "nextChapterButton", nextBtnComp);
            SetField(ctrl, "nextChapterButtonText", nextBtnTxt);
            nextBtnComp.onClick.AddListener(ctrl.OnClickArea);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("已添加「进入下一站章节」按钮。请保存场景。");
    }

    [MenuItem("Tools/创建第二章场景/1.开场")]
    public static void CreateChapter2Intro()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var area = CreateChild(canvas.transform, "Area");
        SetFullRect(area);

        var bg = CreateChild(area.transform, "Background");
        SetFullRect(bg);
        bg.AddComponent<Image>().color = Color.gray;

        var arrow = CreateChild(area.transform, "Arrow");
        var arRect = arrow.AddComponent<RectTransform>();
        arRect.anchorMin = new Vector2(0.3f, 0.5f);
        arRect.anchorMax = new Vector2(0.35f, 0.55f);
        arRect.offsetMin = arRect.offsetMax = Vector2.zero;
        arrow.AddComponent<Image>().color = Color.yellow;

        var hintText = CreateChild(area.transform, "HintText");
        var htRect = hintText.AddComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0.35f, 0.48f);
        htRect.anchorMax = new Vector2(0.6f, 0.52f);
        htRect.offsetMin = htRect.offsetMax = Vector2.zero;
        var ht = hintText.AddComponent<Text>();
        ht.text = "向前进入晋祠";
        ht.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ht.fontSize = 28;
        ht.color = Color.white;

        var player = CreateChild(area.transform, "Player");
        var pr = player.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0, 0.2f);
        pr.pivot = new Vector2(0.5f, 0);
        pr.anchoredPosition = new Vector2(200, 0);
        pr.sizeDelta = new Vector2(80, 120);
        player.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f);
        CharacterSetupUtils.AddFootShadowAndWarmTint(player, 80, 120);
        player.AddComponent<QiaoQiaoPlayerController>();

        var doorZone = CreateChild(area.transform, "DoorZone");
        var dzRect = doorZone.AddComponent<RectTransform>();
        dzRect.anchorMin = new Vector2(0.65f, 0.25f);
        dzRect.anchorMax = new Vector2(0.95f, 0.95f);
        dzRect.offsetMin = dzRect.offsetMax = Vector2.zero;

        var eprompt = CreateChild(area.transform, "PressEPrompt");
        var epRect = eprompt.AddComponent<RectTransform>();
        epRect.anchorMin = epRect.anchorMax = new Vector2(0.5f, 0.1f);
        epRect.pivot = new Vector2(0.5f, 0);
        epRect.sizeDelta = new Vector2(200, 40);
        var epTxt = eprompt.AddComponent<Text>();
        epTxt.text = "按 E 进入";
        epTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epTxt.fontSize = 28;
        epTxt.alignment = TextAnchor.MiddleCenter;
        epTxt.color = Color.white;
        eprompt.SetActive(false);

        var ctrl = canvas.AddComponent<Chapter2IntroController>();
        SetField(ctrl, "playerRect", pr);
        SetField(ctrl, "doorZone", dzRect);
        SetField(ctrl, "pressEPrompt", eprompt);
        SetField(ctrl, "arrowImage", arrow.GetComponent<Graphic>());
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "nextSceneName", "Chapter2Shuijing");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2Intro.scene");
        Debug.Log("第二章开场已创建。请替换背景图、箭头图。");
    }

    [MenuItem("Tools/创建第二章场景/2.水镜台对话")]
    public static void CreateChapter2Dialog()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var bg = CreateChild(canvas.transform, "Background");
        SetFullRect(bg);
        bg.AddComponent<Image>().color = Color.gray;

        var dialogBox = CreateChild(canvas.transform, "DialogBox");
        var dbRect = dialogBox.AddComponent<RectTransform>();
        dbRect.anchorMin = new Vector2(0.2f, 0.1f);
        dbRect.anchorMax = new Vector2(0.8f, 0.4f);
        dbRect.offsetMin = dbRect.offsetMax = Vector2.zero;
        var dbImg = dialogBox.AddComponent<Image>();
        dbImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var dialogText = CreateChild(dialogBox.transform, "Text");
        SetFullRect(dialogText);
        var dt = dialogText.AddComponent<Text>();
        dt.text = "对话内容";
        dt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dt.fontSize = 24;
        dt.color = Color.white;

        var contBtn = CreateChild(canvas.transform, "ContinueButton");
        var cbRect = contBtn.AddComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(1, 0);
        cbRect.anchorMax = new Vector2(1, 0);
        cbRect.pivot = new Vector2(1, 0);
        cbRect.anchoredPosition = new Vector2(-30, 30);
        cbRect.sizeDelta = new Vector2(180, 50);
        contBtn.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
        var btn = contBtn.AddComponent<Button>();
        var cbTxt = CreateChild(contBtn.transform, "Text");
        SetFullRect(cbTxt);
        var cbt = cbTxt.AddComponent<Text>();
        cbt.text = "继续";
        cbt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cbt.fontSize = 24;
        cbt.alignment = TextAnchor.MiddleCenter;
        cbt.color = Color.white;

        var voice = canvas.AddComponent<AudioSource>();
        voice.playOnAwake = false;

        var ctrl = canvas.AddComponent<Chapter2DialogController>();
        SetField(ctrl, "dialogBoxImage", dbImg);
        SetField(ctrl, "dialogText", dt);
        SetField(ctrl, "continueButton", btn);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "voiceAudioSource", voice);
        SetField(ctrl, "nextSceneName", "Chapter2Scroll");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2Shuijing.scene");
        Debug.Log("第二章水镜台对话已创建。请设置 dialogContents、对话框素材；dialogVoiceClips 与每句下标对应（可留空）。");
    }

    [MenuItem("Tools/创建第二章场景/3.卷轴")]
    public static void CreateChapter2Scroll()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;
        var vd = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(vd);
        vd.AddComponent<RawImage>().color = Color.white;
        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var clickOverlay = CreateChild(videoPanel.transform, "ClickOverlay");
        clickOverlay.SetActive(false);
        var coRect = clickOverlay.AddComponent<RectTransform>();
        coRect.anchorMin = Vector2.zero;
        coRect.anchorMax = Vector2.one;
        coRect.offsetMin = coRect.offsetMax = Vector2.zero;
        var coImg = clickOverlay.AddComponent<Image>();
        coImg.color = new Color(0, 0, 0, 0);
        coImg.raycastTarget = true;
        clickOverlay.AddComponent<Button>();

        var clickPrompt = CreateChild(clickOverlay.transform, "ClickPrompt");
        var cpRect = clickPrompt.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.5f, 0);
        cpRect.anchorMax = new Vector2(0.5f, 0);
        cpRect.pivot = new Vector2(0.5f, 0);
        cpRect.anchoredPosition = new Vector2(0, 80);
        cpRect.sizeDelta = new Vector2(400, 50);
        var cpTxt = clickPrompt.AddComponent<Text>();
        cpTxt.text = "点击以解开卷轴";
        cpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cpTxt.fontSize = 28;
        cpTxt.alignment = TextAnchor.MiddleCenter;
        cpTxt.color = Color.white;

        var drawingPanel = CreateChild(canvas.transform, "DrawingPanel");
        drawingPanel.SetActive(false);
        SetFullRect(drawingPanel);
        var dpImg = drawingPanel.AddComponent<Image>();
        dpImg.color = Color.white;

        var contBtn = CreateChild(canvas.transform, "ContinueButton");
        contBtn.SetActive(false);
        var cbRect = contBtn.AddComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(1, 0);
        cbRect.anchorMax = new Vector2(1, 0);
        cbRect.pivot = new Vector2(1, 0);
        cbRect.anchoredPosition = new Vector2(-30, 30);
        cbRect.sizeDelta = new Vector2(180, 50);
        contBtn.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
        var btn = contBtn.AddComponent<Button>();

        var ctrl = canvas.AddComponent<Chapter2ScrollController>();
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", vd.GetComponent<RawImage>());
        SetField(ctrl, "clickOverlay", clickOverlay);
        SetField(ctrl, "drawingPanel", drawingPanel);
        SetField(ctrl, "drawingImage", dpImg);
        SetField(ctrl, "continueButton", btn);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "nextSceneName", "Chapter2Platformer");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2Scroll.scene");
        Debug.Log("第二章卷轴已创建。请设置 scrollFlyClip、unfurlClip（解卷轴动画）、drawingSprite（图四）。最后一帧上叠有可点击区域和「点击以解开卷轴」文字。");
    }

    [MenuItem("Tools/创建第二章场景/卷轴场景-添加点击区域")]
    public static void AddScrollClickOverlay()
    {
        var videoPanel = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform?.Find("VideoPanel");
        if (videoPanel == null) { Debug.LogWarning("请先打开 Chapter2Scroll 场景，且存在 VideoPanel。"); return; }
        if (videoPanel.Find("ClickOverlay") != null) { Debug.Log("ClickOverlay 已存在。"); return; }
        var clickOverlay = CreateChild(videoPanel, "ClickOverlay");
        clickOverlay.SetActive(false);
        var coRect = clickOverlay.AddComponent<RectTransform>();
        coRect.anchorMin = Vector2.zero;
        coRect.anchorMax = Vector2.one;
        coRect.offsetMin = coRect.offsetMax = Vector2.zero;
        var coImg = clickOverlay.AddComponent<Image>();
        coImg.color = new Color(0, 0, 0, 0);
        coImg.raycastTarget = true;
        clickOverlay.AddComponent<Button>();
        var clickPrompt = CreateChild(clickOverlay.transform, "ClickPrompt");
        var cpRect = clickPrompt.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.5f, 0);
        cpRect.anchorMax = new Vector2(0.5f, 0);
        cpRect.pivot = new Vector2(0.5f, 0);
        cpRect.anchoredPosition = new Vector2(0, 80);
        cpRect.sizeDelta = new Vector2(400, 50);
        var cpTxt = clickPrompt.AddComponent<Text>();
        cpTxt.text = "点击以解开卷轴";
        cpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cpTxt.fontSize = 28;
        cpTxt.alignment = TextAnchor.MiddleCenter;
        cpTxt.color = Color.white;
        var ctrl = UnityEngine.Object.FindObjectOfType<Chapter2ScrollController>();
        if (ctrl != null)
        {
            var so = new UnityEditor.SerializedObject(ctrl);
            so.FindProperty("clickOverlay").objectReferenceValue = clickOverlay;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        UnityEditor.Selection.activeGameObject = clickOverlay;
        Debug.Log("已添加 ClickOverlay。选中 Canvas，将 Click Overlay 拖入 Chapter2ScrollController。");
    }

    [MenuItem("Tools/创建第二章场景/4.平台游戏")]
    public static void CreateChapter2Platformer()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var gameRoot = CreateChild(canvas.transform, "GameRoot");
        SetFullRect(gameRoot);

        var bg = CreateChild(gameRoot.transform, "Background");
        SetFullRect(bg);
        bg.AddComponent<Image>().color = new Color(0.3f, 0.4f, 0.3f);

        var player = CreateChild(gameRoot.transform, "Player");
        var pr = player.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0, 0);
        pr.pivot = new Vector2(0.5f, 0); // 脚底为锚点，拖到哪里出生点就在哪里
        pr.anchoredPosition = new Vector2(200, 60); // 站在 Floor 顶部
        pr.sizeDelta = new Vector2(60, 80);
        player.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f);
        CharacterSetupUtils.AddFootShadowAndWarmTint(player, 60, 80);

        var wood1 = CreateWoodItem(gameRoot.transform, "Wood1", 400, 150);
        var wood2 = CreateWoodItem(gameRoot.transform, "Wood2", 600, 200);
        var wood3 = CreateWoodItem(gameRoot.transform, "Wood3", 800, 150);

        var spike1 = CreateSpikeZone(gameRoot.transform, "Spike1", 500, 80);

        var floor = CreatePlatform(gameRoot.transform, "Floor", 960, 40, 1920, 40);
        var plat1 = CreatePlatform(gameRoot.transform, "Platform1", 400, 120, 200, 30);
        var plat2 = CreatePlatform(gameRoot.transform, "Platform2", 700, 180, 200, 30);

        var exit = CreateChild(gameRoot.transform, "Exit");
        var exRect = exit.AddComponent<RectTransform>();
        exRect.anchorMin = new Vector2(1, 0);
        exRect.anchorMax = new Vector2(1, 0);
        exRect.pivot = new Vector2(0.5f, 0);
        exRect.anchoredPosition = new Vector2(-150, 120);
        exRect.sizeDelta = new Vector2(80, 100);
        exit.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);

        var sprompt = CreateChild(canvas.transform, "PressSPrompt");
        var spRect = sprompt.AddComponent<RectTransform>();
        spRect.anchorMin = spRect.anchorMax = new Vector2(0.5f, 0.1f);
        spRect.sizeDelta = new Vector2(200, 40);
        var spTxt = sprompt.AddComponent<Text>();
        spTxt.text = "按 S 通关";
        spTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        spTxt.fontSize = 28;
        spTxt.alignment = TextAnchor.MiddleCenter;
        spTxt.color = Color.white;
        sprompt.SetActive(false);

        var goPanel = CreateChild(canvas.transform, "GameOverPanel");
        goPanel.SetActive(false);
        SetFullRect(goPanel);
        goPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        var goText = CreateChild(goPanel.transform, "Text");
        var goTr = goText.AddComponent<RectTransform>();
        goTr.anchorMin = goTr.anchorMax = new Vector2(0.5f, 0.6f);
        goTr.sizeDelta = new Vector2(400, 60);
        var got = goText.AddComponent<Text>();
        got.text = "游戏失败";
        got.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        got.fontSize = 48;
        got.alignment = TextAnchor.MiddleCenter;
        got.color = Color.red;
        var restartBtn = CreateChild(goPanel.transform, "RestartButton");
        var rbRect = restartBtn.AddComponent<RectTransform>();
        rbRect.anchorMin = rbRect.anchorMax = new Vector2(0.5f, 0.4f);
        rbRect.sizeDelta = new Vector2(200, 50);
        restartBtn.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
        var rb = restartBtn.AddComponent<Button>();

        var collBar = CreateChild(canvas.transform, "CollectionBar");
        var collBarRt = collBar.AddComponent<RectTransform>();
        collBarRt.anchorMin = new Vector2(0f, 1f);
        collBarRt.anchorMax = new Vector2(0f, 1f);
        collBarRt.pivot = new Vector2(0f, 1f);
        collBarRt.anchoredPosition = new Vector2(16f, -16f);
        collBarRt.sizeDelta = new Vector2(268f, 72f);
        var collBarImg = collBar.AddComponent<Image>();
        collBarImg.color = new Color(0f, 0f, 0f, 0.48f);
        collBarImg.raycastTarget = false;
        var collectionSlots = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            var slotGo = CreateChild(collBar.transform, "Slot" + (i + 1));
            var srt = slotGo.AddComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(44f + i * 80f, 0f);
            srt.sizeDelta = new Vector2(56f, 56f);
            var sim = slotGo.AddComponent<Image>();
            sim.raycastTarget = false;
            sim.color = new Color(0.75f, 0.75f, 0.75f, 0.28f);
            var iconGo = CreateChild(slotGo.transform, Chapter2PlatformerController.CollectionOverlayChildName);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(48f, 48f);
            var iimg = iconGo.AddComponent<Image>();
            iimg.raycastTarget = false;
            iimg.enabled = false;
            collectionSlots[i] = srt;
        }
        var fadeTr = canvas.transform.Find("FadeOverlay");
        if (fadeTr != null)
            collBar.transform.SetSiblingIndex(fadeTr.GetSiblingIndex());

        var ctrl = canvas.AddComponent<Chapter2PlatformerController>();
        SetField(ctrl, "playerRect", pr);
        SetField(ctrl, "platforms", new RectTransform[] { floor, plat1, plat2 });
        SetField(ctrl, "woodItems", new RectTransform[] { wood1, wood2, wood3 });
        SetField(ctrl, "collectionSlotTargets", collectionSlots);
        SetField(ctrl, "spikeZones", new RectTransform[] { spike1 });
        SetField(ctrl, "exitZone", exRect);
        SetField(ctrl, "pressSPrompt", sprompt);
        SetField(ctrl, "gameOverPanel", goPanel);
        SetField(ctrl, "restartButton", rb);
        SetField(ctrl, "nextSceneName", "Chapter2VideoEnd");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2Platformer.scene");
        Debug.Log("第二章平台游戏已创建。人物拖到哪里，出生点就在哪里。请替换背景、木构件、尖刺图，调整位置。");
    }

    static RectTransform CreatePlatform(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = CreateChild(parent, name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        go.AddComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f);
        return rt;
    }

    [MenuItem("Tools/创建第二章场景/添加平台（显示原图）")]
    public static void AddPlatformWithImage()
    {
        var parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform?.Find("GameRoot");
        if (parent == null) parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform;
        if (parent == null) { Debug.LogWarning("请先打开 Chapter2Platformer 场景。"); return; }
        var sel = UnityEditor.Selection.activeObject as Sprite;
        var go = CreateChild(parent, "Platform_原图");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(400, 80);
        rt.sizeDelta = sel != null ? new Vector2(sel.rect.width, sel.rect.height) : new Vector2(600, 80);
        var img = go.AddComponent<Image>();
        img.sprite = sel;
        img.color = sel != null ? Color.white : new Color(0.5f, 0.45f, 0.35f);
        go.AddComponent<PlatformHeightMap>();
        UnityEditor.Selection.activeGameObject = go;
        Debug.Log("已创建平台。拖入 Platforms 后，在 Inspector 点「烘焙高度图（从 Sprite）」即可按原图轮廓碰撞。");
    }

    [MenuItem("Tools/创建第二章场景/为选中平台添加高度图并烘焙")]
    public static void AddAndBakeHeightMap()
    {
        var go = UnityEditor.Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("请选中平台物体。"); return; }
        var hm = go.GetComponent<PlatformHeightMap>();
        if (hm == null) hm = go.AddComponent<PlatformHeightMap>();
        var img = go.GetComponent<Image>();
        if (img == null) img = go.GetComponentInChildren<Image>();
        if (img == null || img.sprite == null) { Debug.LogWarning("平台需有 Image 且已拖入 Sprite。"); return; }
        PlatformHeightMapEditor.BakeStatic(hm, img.sprite);
        UnityEditor.EditorUtility.SetDirty(hm);
        Debug.Log("已烘焙高度图。");
    }

    [MenuItem("Tools/创建第二章场景/设置选中图片可读（平台原图用）")]
    public static void SetTextureReadable()
    {
        var path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
        if (string.IsNullOrEmpty(path)) { Debug.LogWarning("请选中一张图片。"); return; }
        var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (imp == null) { Debug.LogWarning("选中的不是图片。"); return; }
        if (!imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); Debug.Log("已勾选 Read/Write Enabled。"); }
        else { Debug.Log("该图片已可读。"); }
    }

    [MenuItem("Tools/创建第二章场景/添加平台（覆盖背景地面）")]
    public static void AddPlatformForGround()
    {
        var sel = UnityEditor.Selection.activeTransform;
        Transform parent = sel != null ? sel : UnityEngine.Object.FindObjectOfType<Canvas>()?.transform?.Find("GameRoot");
        if (parent == null) parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform;
        if (parent == null) { Debug.LogWarning("请先打开 Chapter2Platformer 场景，或选中 GameRoot。"); return; }
        string name = "Platform_地面";
        int idx = 1;
        while (parent.Find(name + idx) != null) idx++;
        var plat = CreatePlatform(parent, name + idx, 500, 60, 800, 40);
        var img = plat.GetComponent<Image>();
        if (img != null) img.color = new Color(0.5f, 0.45f, 0.35f, 0.3f);
        UnityEditor.Selection.activeGameObject = plat.gameObject;
        Debug.Log("已添加平台。请拖到地面位置，调整 RectTransform 使顶部对齐地面表面，然后拖入 Canvas 的 Chapter2PlatformerController → Platforms 数组。可设 Image 透明度为 0 做成隐形平台。");
    }

    [MenuItem("Tools/创建第二章场景/拼接平台链（3段重叠）")]
    public static void AddPlatformChain()
    {
        var parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform?.Find("GameRoot");
        if (parent == null) parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform;
        if (parent == null) { Debug.LogWarning("请先打开 Chapter2Platformer 场景。"); return; }
        const float overlap = 40f;
        const float segW = 400f;
        const float h = 40f;
        const float centerY = 70f;
        var list = new System.Collections.Generic.List<RectTransform>();
        for (int i = 0; i < 3; i++)
        {
            float centerX = segW / 2f + i * (segW - overlap);
            var p = CreatePlatform(parent, "Platform_链" + (i + 1), centerX, centerY, segW, h);
            p.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 0f);
            list.Add(p);
        }
        var ctrl = UnityEngine.Object.FindObjectOfType<Chapter2PlatformerController>();
        if (ctrl != null)
        {
            var so = new UnityEditor.SerializedObject(ctrl);
            var arr = so.FindProperty("platforms");
            if (arr != null)
            {
                int start = arr.arraySize;
                arr.arraySize = start + 3;
                for (int i = 0; i < 3; i++) arr.GetArrayElementAtIndex(start + i).objectReferenceValue = list[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        UnityEditor.Selection.activeGameObject = list[0].gameObject;
        Debug.Log("已创建 3 段平台，每段重叠 40px。可拖拽调整位置以贴合背景，再复制/添加更多段。");
    }

    [MenuItem("Tools/创建第二章场景/添加简单平台（一整块）")]
    public static void AddSimplePlatform()
    {
        var parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform?.Find("GameRoot");
        if (parent == null) parent = UnityEngine.Object.FindObjectOfType<Canvas>()?.transform;
        if (parent == null) { Debug.LogWarning("请先打开 Chapter2Platformer 场景。"); return; }
        var plat = CreatePlatform(parent, "Platform_Simple", 960, 50, 1920, 80);
        var img = plat.GetComponent<Image>();
        if (img != null) img.color = new Color(0.5f, 0.45f, 0.35f, 0f);
        var ctrl = UnityEngine.Object.FindObjectOfType<Chapter2PlatformerController>();
        if (ctrl != null)
        {
            var so = new UnityEditor.SerializedObject(ctrl);
            var arr = so.FindProperty("platforms");
            if (arr != null) { arr.arraySize = 1; arr.GetArrayElementAtIndex(0).objectReferenceValue = plat; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        UnityEditor.Selection.activeGameObject = plat.gameObject;
        Debug.Log("已创建一整块隐形平台，并已加入 Platforms。把人物拖到平台上方即可运行。");
    }

    [MenuItem("Tools/创建第二章场景/为木构件添加发光特效")]
    public static void AddGlowToWoodItems()
    {
        var ctrl = UnityEngine.Object.FindObjectOfType<Chapter2PlatformerController>();
        if (ctrl == null) { Debug.LogWarning("请先打开 Chapter2Platformer 场景。"); return; }
        var woodField = ctrl.GetType().GetField("woodItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (woodField == null) { Debug.LogWarning("未找到 woodItems 字段"); return; }
        var woodItems = woodField.GetValue(ctrl) as RectTransform[];
        if (woodItems == null || woodItems.Length == 0) { Debug.LogWarning("woodItems 为空，请先在 Inspector 中拖入木构件"); return; }
        int added = 0;
        foreach (var w in woodItems)
        {
            if (w == null) continue;
            if (w.GetComponent<CollectibleGlow>() != null) continue;
            w.gameObject.AddComponent<CollectibleGlow>();
            added++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"已为 {added} 个木构件添加发光特效。");
    }

    static RectTransform CreateWoodItem(Transform parent, string name, float x, float y)
    {
        var go = CreateChild(parent, name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(50, 50);
        go.AddComponent<Image>().color = new Color(0.6f, 0.4f, 0.2f);
        go.AddComponent<CollectibleGlow>();
        return rt;
    }

    static RectTransform CreateSpikeZone(Transform parent, string name, float x, float y)
    {
        var go = CreateChild(parent, name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(100, 40);
        go.AddComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f);
        return rt;
    }

    [MenuItem("Tools/创建第二章场景/5.通关视频")]
    public static void CreateChapter2VideoEnd()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;
        var vd = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(vd);
        vd.AddComponent<RawImage>().color = Color.white;
        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var subGo = CreateChild(canvas.transform, "PostVideoDialog");
        var subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.38f);
        subRt.offsetMin = new Vector2(56f, 36f);
        subRt.offsetMax = new Vector2(-56f, 20f);
        var subImg = subGo.AddComponent<Image>();
        subImg.color = new Color(1f, 1f, 1f, 0f);
        subImg.preserveAspect = true;
        subImg.raycastTarget = false;

        var knowGo = CreateChild(canvas.transform, "KnowledgePanel");
        knowGo.SetActive(false);
        SetFullRect(knowGo);
        var dim = CreateChild(knowGo.transform, "Dim");
        SetFullRect(dim);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        var illuGo = CreateChild(knowGo.transform, "Illustration");
        var illuRt = illuGo.AddComponent<RectTransform>();
        illuRt.anchorMin = new Vector2(0.06f, 0.14f);
        illuRt.anchorMax = new Vector2(0.94f, 0.8f);
        illuRt.offsetMin = Vector2.zero;
        illuRt.offsetMax = Vector2.zero;
        var illuImg = illuGo.AddComponent<Image>();
        illuImg.color = Color.white;
        illuImg.preserveAspect = true;

        var kBtnGo = CreateChild(knowGo.transform, "KnowledgeContinue");
        var kbrt = kBtnGo.AddComponent<RectTransform>();
        kbrt.anchorMin = new Vector2(0.5f, 0f);
        kbrt.anchorMax = new Vector2(0.5f, 0f);
        kbrt.pivot = new Vector2(0.5f, 0f);
        kbrt.anchoredPosition = new Vector2(0f, 40f);
        kbrt.sizeDelta = new Vector2(220f, 52f);
        kBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.52f, 0.82f);
        var kbtn = kBtnGo.AddComponent<Button>();
        var ktGo = CreateChild(kBtnGo.transform, "Text");
        SetFullRect(ktGo);
        var ktt = ktGo.AddComponent<Text>();
        ktt.text = "继续";
        ktt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ktt.fontSize = 26;
        ktt.alignment = TextAnchor.MiddleCenter;
        ktt.color = Color.white;

        var voice = canvas.AddComponent<AudioSource>();
        voice.playOnAwake = false;

        var ctrl = canvas.AddComponent<Chapter2VideoEndController>();
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", vd.GetComponent<RawImage>());
        SetField(ctrl, "videoPanel", videoPanel);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "postVideoDialogImage", subImg);
        SetField(ctrl, "postVideoSubtitle", (Text)null);
        SetField(ctrl, "knowledgePanel", knowGo);
        SetField(ctrl, "knowledgeIllustration", illuImg);
        SetField(ctrl, "knowledgeContinueButton", kbtn);
        SetField(ctrl, "subtitleVoiceSource", voice);
        SetField(ctrl, "subtitleMessage", "");
        SetField(ctrl, "nextSceneName", "Chapter3Bridge");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        SaveScene("Assets/Scenes/Chapter2VideoEnd.scene");
        Debug.Log("第二章通关视频已创建。请设置 videoClip 或 StreamingAssets/chapter2_end.mp4；PostVideoDialog 上拖对白 Sprite（Image）；knowledgeSprite 为小知识插图；subtitleVoiceClip 可选；「继续」仅在知识页。未挂 Image 时仍可用旧版 Post Video Subtitle Text。");
    }

    [MenuItem("Tools/创建第二章场景/全部")]
    public static void CreateAll()
    {
        CreateChapter2Title();
        CreateChapter2Intro();
        CreateChapter2Dialog();
        CreateChapter2Scroll();
        CreateChapter2Platformer();
        CreateChapter2VideoEnd();

        var scenes = new[] { "Chapter2Title", "Chapter2Intro", "Chapter2Shuijing", "Chapter2Scroll", "Chapter2Platformer", "Chapter2VideoEnd" };
        var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            var path = "Assets/Scenes/" + s + ".scene";
            if (!list.Exists(x => x.path == path))
                list.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
        }
        UnityEditor.EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("第二章全部场景已创建。");
    }

    static void SaveScene(string path)
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Scenes"))
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
    }
}
