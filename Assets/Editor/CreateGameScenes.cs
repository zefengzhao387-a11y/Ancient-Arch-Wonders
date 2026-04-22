using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键创建完整游戏流程场景
/// 菜单：Tools -> 创建乔家大院游戏场景
/// </summary>
public static class CreateGameScenes
{
    [MenuItem("Tools/创建乔家大院游戏场景/0.游戏菜单场景")]
    public static void CreateGameMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();
        EnsurePersistentGameBGM();
        EnsureGameUISfxHub();

        var canvas = CreateCanvas();

        var bg = CreateChild(canvas.transform, "Background");
        SetFullRect(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.2f, 0.25f);

        var startBtn = CreateMenuButton(canvas.transform, "游戏开始", new Vector2(0, 60));
        var exitBtn = CreateMenuButton(canvas.transform, "退出游戏", new Vector2(0, -60));

        var ctrl = canvas.AddComponent<GameMenuController>();
        SetField(ctrl, "startGameButton", startBtn.GetComponent<Button>());
        SetField(ctrl, "exitGameButton", exitBtn.GetComponent<Button>());
        SetField(ctrl, "nextSceneName", "OpeningVideo");

        SaveScene("Assets/Scenes/GameMenu.scene");
        UpdateBuildSettings();
        Debug.Log("游戏菜单场景已创建。可将背景图拖到 Background 的 Image 组件；PersistentGameBGM 拖 BGM；GameUISfxHub 拖 Default Button Click（全游戏共用）；其它场景可用 Tools→游戏性→当前场景所有 Button 添加 UIButtonSfx。");
    }

    [MenuItem("Tools/游戏性/当前场景添加 GameUISfxHub（若无）")]
    public static void AddGameUISfxHubIfMissing()
    {
        if (Object.FindObjectOfType<GameUISfxHub>() != null)
        {
            Debug.Log("GameUISfxHub 已存在。");
            return;
        }
        var go = new GameObject("GameUISfxHub");
        go.AddComponent<AudioSource>();
        go.AddComponent<GameUISfxHub>();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("已添加 GameUISfxHub：拖入 Default Button Click，保存场景。");
    }

    [MenuItem("Tools/游戏性/当前场景所有 Button 添加 UIButtonSfx")]
    public static void AddUIButtonSfxToAllButtonsInOpenScene()
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        int added = 0;
        foreach (var b in buttons)
        {
            if (b.GetComponent<UIButtonSfx>() != null) continue;
            b.gameObject.AddComponent<UIButtonSfx>();
            added++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"UIButtonSfx：新增 {added} 个（已有则跳过）。点击音共用 GameUISfxHub 的 Default Button Click；无 Hub 时运行中会自动创建（仍须在 Hub 上设默认音）。");
    }

    [MenuItem("Tools/游戏性/当前场景添加 PersistentGameBGM（若无）")]
    public static void AddPersistentGameBGMIfMissing()
    {
        if (Object.FindObjectOfType<PersistentGameBGM>() != null)
        {
            Debug.Log("PersistentGameBGM 已存在。");
            return;
        }
        var go = new GameObject("PersistentGameBGM");
        go.AddComponent<AudioSource>();
        go.AddComponent<PersistentGameBGM>();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("已添加 PersistentGameBGM：在 Inspector 拖入 BGM 或填写 Resources Fallback Path，保存场景。");
    }

    static void EnsurePersistentGameBGM()
    {
        if (Object.FindObjectOfType<PersistentGameBGM>() != null) return;
        var go = new GameObject("PersistentGameBGM");
        go.AddComponent<AudioSource>();
        go.AddComponent<PersistentGameBGM>();
    }

    static void EnsureGameUISfxHub()
    {
        if (Object.FindObjectOfType<GameUISfxHub>() != null) return;
        var go = new GameObject("GameUISfxHub");
        go.AddComponent<AudioSource>();
        go.AddComponent<GameUISfxHub>();
    }

    [MenuItem("Tools/创建乔家大院游戏场景/1.开场视频场景")]
    public static void CreateOpeningVideoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;

        var videoDisplay = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(videoDisplay);
        var fitter = videoDisplay.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        var rawImg = videoDisplay.AddComponent<RawImage>();
        rawImg.color = Color.white;

        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var continueBtn = CreateButton(canvas.transform, "继续", new Vector2(1, 0), new Vector2(-150, 80), new Vector2(200, 50));

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();
        var fadeImg = canvas.transform.Find("FadeOverlay")?.GetComponent<Image>();
        var ctrl = canvas.AddComponent<OpeningVideoController>();
        SetField(ctrl, "fadeOverlay", fadeImg);
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", rawImg);
        SetField(ctrl, "continueButton", continueBtn.GetComponent<Button>());
        SetField(ctrl, "nextSceneName", "Outdoor");

        SaveScene("Assets/Scenes/OpeningVideo.scene");
        UpdateBuildSettings();
        Debug.Log("开场视频场景已创建。请将视频拖到 OpeningVideoController.videoClip，或放入 StreamingAssets/opening.mp4");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/2.户外场景")]
    public static void CreateOutdoorScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var area = new GameObject("户外区域");
        area.transform.SetParent(canvas.transform, false);
        var aRect = area.AddComponent<RectTransform>();
        aRect.anchorMin = Vector2.zero;
        aRect.anchorMax = Vector2.one;
        aRect.offsetMin = Vector2.zero;
        aRect.offsetMax = Vector2.zero;

        var bg = CreateChild(area.transform, "Background");
        SetFullRect(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.gray;

        var doorZone = CreateChild(area.transform, "DoorZone");
        var dzRect = doorZone.AddComponent<RectTransform>();
        dzRect.anchorMin = new Vector2(0.65f, 0.25f);
        dzRect.anchorMax = new Vector2(0.95f, 0.95f);
        dzRect.offsetMin = Vector2.zero;
        dzRect.offsetMax = Vector2.zero;

        var player = CreateChild(area.transform, "Player");
        var pr = player.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 0.2f);
        pr.anchorMax = new Vector2(0, 0.2f);
        pr.pivot = new Vector2(0.5f, 0);
        pr.anchoredPosition = new Vector2(200, 0);
        pr.sizeDelta = new Vector2(80, 120);
        var pi = player.AddComponent<Image>();
        pi.color = new Color(0.3f, 0.5f, 0.8f);
        CharacterSetupUtils.AddFootShadowAndWarmTint(player, 80, 120);

        var moveHint = CreateChild(player.transform, "MoveHint");
        var mhRect = moveHint.AddComponent<RectTransform>();
        mhRect.anchorMin = new Vector2(0.5f, 1f);
        mhRect.anchorMax = new Vector2(0.5f, 1f);
        mhRect.pivot = new Vector2(0.5f, 0);
        mhRect.anchoredPosition = new Vector2(0, 15);
        mhRect.sizeDelta = new Vector2(180, 30);
        var mhTxt = moveHint.AddComponent<Text>();
        mhTxt.text = "按AD进行移动";
        mhTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mhTxt.fontSize = 20;
        mhTxt.alignment = TextAnchor.MiddleCenter;
        mhTxt.color = Color.white;

        var qiaoCtrl = player.AddComponent<QiaoQiaoPlayerController>();
        SetField(qiaoCtrl, "moveHint", moveHint);

        var prompt = CreateChild(area.transform, "PressEPrompt");
        var prRect = prompt.AddComponent<RectTransform>();
        prRect.anchorMin = new Vector2(0.5f, 0.1f);
        prRect.anchorMax = new Vector2(0.5f, 0.1f);
        prRect.pivot = new Vector2(0.5f, 0);
        prRect.anchoredPosition = Vector2.zero;
        prRect.sizeDelta = new Vector2(200, 40);
        var prTxt = prompt.AddComponent<Text>();
        prTxt.text = "按 E 进入";
        prTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        prTxt.fontSize = 28;
        prTxt.alignment = TextAnchor.MiddleCenter;
        prTxt.color = Color.white;

        var fadeImg = canvas.transform.Find("FadeOverlay")?.GetComponent<Image>();
        var ctrl = canvas.AddComponent<OutdoorSceneController>();
        SetField(ctrl, "areaOutside", area);
        SetField(ctrl, "playerRect", pr);
        SetField(ctrl, "doorZone", dzRect);
        SetField(ctrl, "pressEPrompt", prompt);
        SetField(ctrl, "fadeOverlay", fadeImg);
        SetField(ctrl, "nextSceneName", "RulesVideo");

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();

        SaveScene("Assets/Scenes/Outdoor.scene");
        Debug.Log("户外场景已创建。请将乔家大院外部图拖到 Background，将站立图拖到 Idle Sprite、行走帧拖到 Walk Frames");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/3.视频开场场景")]
    public static void CreateVideoIntroScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;

        var videoDisplay = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(videoDisplay);
        var fitter = videoDisplay.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        var rawImg = videoDisplay.AddComponent<RawImage>();
        rawImg.color = Color.white;

        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var continueBtn = CreateButton(canvas.transform, "继续", new Vector2(1, 0), new Vector2(-150, 80), new Vector2(200, 50));

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();
        var fadeImg = canvas.transform.Find("FadeOverlay")?.GetComponent<Image>();
        var ctrl = canvas.AddComponent<VideoIntroController>();
        SetField(ctrl, "fadeOverlay", fadeImg);
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", rawImg);
        SetField(ctrl, "continueButton", continueBtn.GetComponent<Button>());
        SetField(ctrl, "nextSceneName", "RulesVideo");

        SaveScene("Assets/Scenes/VideoIntro.scene");
        Debug.Log("视频开场场景已创建。请将视频拖到 VideoIntroController.videoClip，或放入 StreamingAssets/intro.mp4");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/4.规则视频场景")]
    public static void CreateRulesVideoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;

        var videoDisplay = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(videoDisplay);
        var fitter = videoDisplay.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        var rawImg = videoDisplay.AddComponent<RawImage>();
        rawImg.color = Color.white;

        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var startBtn = CreateButton(canvas.transform, "开始游戏", new Vector2(1, 0), new Vector2(-150, 80), new Vector2(200, 50));

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();
        var fadeImg = canvas.transform.Find("FadeOverlay")?.GetComponent<Image>();
        var ctrl = canvas.AddComponent<RulesVideoController>();
        SetField(ctrl, "fadeOverlay", fadeImg);
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", rawImg);
        SetField(ctrl, "startGameButton", startBtn.GetComponent<Button>());
        SetField(ctrl, "nextSceneName", "TenonMortiseGame");

        SaveScene("Assets/Scenes/RulesVideo.scene");
        Debug.Log("规则视频场景已创建。请将规则视频拖到 RulesVideoController.videoClip，或放入 StreamingAssets/rules.mp4");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/5.第一章结束过渡(视频+自动对话)", false, 205)]
    public static void CreateChapter1OutroScene()
    {
        CreateChapter1OutroSceneInternal();
    }

    /// <summary>与上一项相同，若子菜单里找不到请用此项（顶层 Tools 下）。</summary>
    [MenuItem("Tools/创建 Chapter1Outro 场景", false, 1)]
    public static void CreateChapter1OutroScene_ToolsRoot()
    {
        CreateChapter1OutroSceneInternal();
    }

    /// <summary>若该脚本的 .meta 损坏导致 GUID 无法解析，执行一次；会删除 .meta 并由编辑器重新生成。</summary>
    [MenuItem("Tools/修复 Chapter1PostMiniGameController 的 .meta", false, 2)]
    public static void RegenerateChapter1PostScriptMeta()
    {
        const string scriptPath = "Assets/Scripts/Chapter1PostMiniGameController.cs";
        var metaPath = scriptPath + ".meta";
        if (!System.IO.File.Exists(scriptPath))
        {
            Debug.LogError("找不到脚本：" + scriptPath);
            return;
        }
        if (System.IO.File.Exists(metaPath))
            System.IO.File.Delete(metaPath);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log("已删除旧 .meta 并重新导入 " + scriptPath + "。请等待编译完成。");
    }

    /// <summary>生成第一章小游戏结束后的过渡场景（视频停最后一帧 + 两段自动对话）。</summary>
    static void CreateChapter1OutroSceneInternal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);

        var videoPanel = CreateChild(canvas.transform, "VideoPanel");
        SetFullRect(videoPanel);
        videoPanel.AddComponent<Image>().color = Color.black;

        var videoDisplayGo = CreateChild(videoPanel.transform, "VideoDisplay");
        SetFullRect(videoDisplayGo);
        var fitter = videoDisplayGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        var rawImg = videoDisplayGo.AddComponent<RawImage>();
        rawImg.color = Color.white;

        var vp = videoPanel.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;

        var d1Go = CreateChild(canvas.transform, "Dialog1");
        SetFullRect(d1Go);
        var d1Rt = d1Go.GetComponent<RectTransform>();
        d1Rt.offsetMin = new Vector2(48, 100);
        d1Rt.offsetMax = new Vector2(-48, -100);
        var d1Img = d1Go.AddComponent<Image>();
        d1Img.color = new Color(1f, 1f, 1f, 0f);
        d1Img.preserveAspect = true;
        d1Img.raycastTarget = false;

        var d2Go = CreateChild(canvas.transform, "Dialog2");
        SetFullRect(d2Go);
        var d2Rt = d2Go.GetComponent<RectTransform>();
        d2Rt.offsetMin = new Vector2(48, 100);
        d2Rt.offsetMax = new Vector2(-48, -100);
        var d2Img = d2Go.AddComponent<Image>();
        d2Img.color = new Color(1f, 1f, 1f, 0f);
        d2Img.preserveAspect = true;
        d2Img.raycastTarget = false;

        var audio = canvas.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();
        var fadeImg = canvas.transform.Find("FadeOverlay")?.GetComponent<Image>();
        var ctrlType = System.Type.GetType("Chapter1PostMiniGameController, Assembly-CSharp")
            ?? System.Type.GetType("Chapter1PostMiniGameController, Assembly-CSharp-firstpass");
        if (ctrlType == null)
        {
            Debug.LogError("未找到 Chapter1PostMiniGameController（脚本可能未进程序集）。请先执行菜单 Tools -> 修复 Chapter1PostMiniGameController 的 .meta，等待编译通过后再生成场景。");
            return;
        }
        var ctrl = canvas.AddComponent(ctrlType);
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", rawImg);
        SetField(ctrl, "dialogImage1", d1Img);
        SetField(ctrl, "dialogImage2", d2Img);
        SetField(ctrl, "dialogText1", (Text)null);
        SetField(ctrl, "dialogText2", (Text)null);
        SetField(ctrl, "voiceAudioSource", audio);
        SetField(ctrl, "fadeOverlay", fadeImg);
        SetField(ctrl, "nextSceneName", "Chapter2Intro");

        SaveScene("Assets/Scenes/Chapter1Outro.scene");
        UpdateBuildSettings();
        Debug.Log("第一章结束过渡场景已创建：Assets/Scenes/Chapter1Outro.scene。请将短视频拖到 Chapter1PostMiniGameController.videoClip，或放入 StreamingAssets/chapter1_outro.mp4；对白为 Dialog1/Dialog2 上的 Image（拖对白 Sprite，Preserve Aspect）；可选 dialogVoice1/2。未挂 Image 时仍可用旧版 Text。");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/6.榫卯游戏场景")]
    public static void CreateTenonMortiseGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EnsureEventSystem();

        var canvas = CreateCanvas();
        CreateFadeOverlay(canvas.transform);
        var fadeIn = canvas.AddComponent<SceneFadeIn>();
        SetField(fadeIn, "fadeOverlay", canvas.transform.Find("FadeOverlay")?.GetComponent<Image>());

        var gameRoot = CreateChild(canvas.transform, "TenonMortiseGame");
        SetFullRect(gameRoot);

        var bg = CreateChild(gameRoot.transform, "Background");
        SetFullRect(bg);
        bg.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f);

        var topRow = CreateChild(gameRoot.transform, "TopSlots");
        var trRect = topRow.AddComponent<RectTransform>();
        trRect.anchorMin = new Vector2(0.1f, 0.55f);
        trRect.anchorMax = new Vector2(0.75f, 0.9f);
        trRect.offsetMin = Vector2.zero;
        trRect.offsetMax = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            var slot = CreateDropSlot(trRect.transform, i);
            slot.name = "DropZone_" + i;
        }

        var bottomRow = CreateChild(gameRoot.transform, "BottomItems");
        var brRect = bottomRow.AddComponent<RectTransform>();
        brRect.anchorMin = new Vector2(0.1f, 0.05f);
        brRect.anchorMax = new Vector2(0.75f, 0.4f);
        brRect.offsetMin = Vector2.zero;
        brRect.offsetMax = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            var item = CreateDraggableItem(brRect.transform, i);
            item.name = "Draggable_" + i;
        }

        var measurementBar = CreateMeasurementBar(canvas.transform);
        var glowRoot = CreateTenonMortiseGlow(canvas.transform);
        var ctrl = measurementBar.GetComponent<MeasurementBarController>();
        SetField(ctrl, "tenonMortiseGlow", glowRoot);
        glowRoot.AddComponent<TenonMortiseClickZone>();

        var introPanel = CreateTenonMortiseIntroPanel(canvas.transform);
        var introDisplayGo = CreateChild(canvas.transform, "TenonMortiseIntroController");
        var introDisplay = introDisplayGo.AddComponent<TenonMortiseIntroDisplay>();
        SetField(introDisplay, "panelRoot", introPanel);
        SetField(introDisplay, "introImage", introPanel.transform.Find("IntroImage")?.GetComponent<Image>());
        SetField(introDisplay, "fadeOverlay", introPanel.transform.Find("FadeOverlay")?.GetComponent<Image>());
        SetField(introDisplay, "continueButton", introPanel.transform.Find("Button")?.GetComponent<Button>());

        var dropZones = topRow.GetComponentsInChildren<DropZone>();
        var bridgeGo = new GameObject("MatchToMeasurementBridge");
        bridgeGo.transform.SetParent(canvas.transform, false);
        var bridge = bridgeGo.AddComponent<MatchToMeasurementBridge>();
        var pairingToast = bridgeGo.AddComponent<TenonMortisePairingBlockToast>();
        SetField(bridge, "dropZones", dropZones);
        SetField(bridge, "measurementBar", ctrl);
        SetField(bridge, "introDisplay", introDisplay);
        SetField(bridge, "pairingBlockToast", pairingToast);
        SetField(bridge, "nextSceneName", "Chapter1Outro");

        foreach (var dz in dropZones)
        {
            var br = dz.gameObject.AddComponent<MatchReactionBridge>();
            SetField(br, "bridge", bridge);
        }

        canvas.transform.Find("FadeOverlay")?.SetAsLastSibling();

        SaveScene("Assets/Scenes/TenonMortiseGame.scene");
        UpdateBuildSettings();

        Debug.Log("榫卯游戏场景已创建。请设置：\n" +
            "1. Background 图片\n" +
            "2. 每个 DropZone 的 Matched Sprite、Fully Riveted Sprite、Intro Sprite(榫卯介绍图)\n" +
            "3. 每个 DraggableItem 的 Item Id 和图片\n" +
            "4. 测量仪素材（替换 Bar 和 Needle 的图片）\n" +
            "5. MeasurementBarController 的 minValue/maxValue 调节指针范围");
    }

    [MenuItem("Tools/创建乔家大院游戏场景/全部场景")]
    public static void CreateAllScenes()
    {
        CreateGameMenuScene();
        CreateOpeningVideoScene();
        CreateOutdoorScene();
        CreateRulesVideoScene();
        CreateChapter1OutroSceneInternal();
        CreateTenonMortiseGameScene();
        UpdateBuildSettings();
        Debug.Log("全部场景已创建。请在 Build Settings 中确认场景顺序。");
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    static GameObject CreateCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.localScale = Vector3.one;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    static void CreateFadeOverlay(Transform parent)
    {
        var fadeObj = CreateChild(parent, "FadeOverlay");
        SetFullRect(fadeObj);
        var fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 1);
        fadeImg.raycastTarget = false;
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
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateButton(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = CreateChild(parent, "Button");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
        var btn = go.AddComponent<Button>();

        var txtGo = CreateChild(go.transform, "Text");
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return go;
    }

    static GameObject CreateMenuButton(Transform parent, string text, Vector2 offsetFromCenter)
    {
        var go = CreateChild(parent, "Button");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offsetFromCenter;
        rt.sizeDelta = new Vector2(280, 56);
        go.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.7f);
        var btn = go.AddComponent<Button>();

        var txtGo = CreateChild(go.transform, "Text");
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        go.AddComponent<UIButtonSfx>();
        return go;
    }

    static GameObject CreateDropSlot(Transform parent, int expectedId)
    {
        var go = CreateChild(parent, "Slot_" + expectedId);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f + 0.3f * expectedId, 0);
        rect.anchorMax = new Vector2(0.05f + 0.3f * (expectedId + 1), 1);
        rect.offsetMin = new Vector2(5, 5);
        rect.offsetMax = new Vector2(-5, -5);
        var slotImg = go.AddComponent<Image>();
        slotImg.color = new Color(1, 1, 1, 0.3f);
        slotImg.raycastTarget = true;

        var display = CreateChild(go.transform, "DisplayImage");
        SetFullRect(display);
        var dispImg = display.AddComponent<Image>();
        dispImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        dispImg.raycastTarget = false;

        var dz = go.AddComponent<DropZone>();
        SetField(dz, "expectedItemId", expectedId);
        SetField(dz, "targetImage", dispImg);

        return go;
    }

    static GameObject CreateDraggableItem(Transform parent, int itemId)
    {
        var go = CreateChild(parent, "Item_" + itemId);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f + 0.3f * itemId, 0);
        rect.anchorMax = new Vector2(0.05f + 0.3f * (itemId + 1), 1);
        rect.offsetMin = new Vector2(5, 5);
        rect.offsetMax = new Vector2(-5, -5);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.5f, 0.7f, 0.9f);
        img.raycastTarget = true;
        var di = go.AddComponent<DraggableItem>();
        SetField(di, "itemId", itemId);
        return go;
    }

    static GameObject CreateMeasurementBar(Transform parent)
    {
        var root = CreateChild(parent, "MeasurementBar");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1, 0);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.pivot = new Vector2(1, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(80, 0);

        var bar = CreateChild(root.transform, "Bar");
        SetFullRect(bar);
        bar.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        var needle = CreateChild(bar.transform, "Needle");
        var ndRect = needle.AddComponent<RectTransform>();
        ndRect.anchorMin = new Vector2(0.5f, 0.5f);
        ndRect.anchorMax = new Vector2(0.5f, 0.5f);
        ndRect.pivot = new Vector2(0.5f, 0.5f);
        ndRect.anchoredPosition = Vector2.zero;
        ndRect.sizeDelta = new Vector2(50, 8);
        needle.AddComponent<Image>().color = new Color(1f, 0.9f, 0.2f);

        var barRect = bar.GetComponent<RectTransform>();
        var ctrl = root.AddComponent<MeasurementBarController>();
        SetField(ctrl, "barRect", barRect);
        SetField(ctrl, "needleRect", ndRect);
        return root;
    }

    static GameObject CreateTenonMortiseIntroPanel(Transform parent)
    {
        var panel = CreateChild(parent, "TenonMortiseIntroPanel");
        panel.transform.SetAsLastSibling();
        SetFullRect(panel);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.9f);
        panelImg.raycastTarget = true;

        var introImgObj = CreateChild(panel.transform, "IntroImage");
        SetFullRect(introImgObj);
        var introImg = introImgObj.AddComponent<Image>();
        introImg.color = Color.white;
        introImg.raycastTarget = false;
        introImg.preserveAspect = true;

        var fadeObj = CreateChild(panel.transform, "FadeOverlay");
        SetFullRect(fadeObj);
        var fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 1);
        fadeImg.raycastTarget = false;

        CreateButton(panel.transform, "继续", new Vector2(1, 0), new Vector2(-30, 30), new Vector2(180, 50));

        panel.SetActive(false);
        return panel;
    }

    static GameObject CreateTenonMortiseGlow(Transform parent)
    {
        var glowRoot = CreateChild(parent, "TenonMortiseGlow");
        glowRoot.SetActive(false);
        var grRect = glowRoot.AddComponent<RectTransform>();
        grRect.anchorMin = new Vector2(0.3f, 0.3f);
        grRect.anchorMax = new Vector2(0.7f, 0.7f);
        grRect.offsetMin = Vector2.zero;
        grRect.offsetMax = Vector2.zero;
        glowRoot.AddComponent<TenonMortiseGlow>();
        return glowRoot;
    }

    static void SaveScene(string path)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), path);
    }

    static void UpdateBuildSettings()
    {
        var scenes = new[] { "GameMenu", "OpeningVideo", "Outdoor", "RulesVideo", "TenonMortiseGame", "Chapter1Outro" };
        var paths = scenes.Select(s => "Assets/Scenes/" + s + ".scene").ToArray();
        var buildScenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
        var existing = EditorBuildSettings.scenes.ToList();
        foreach (var p in paths)
        {
            existing.RemoveAll(e => e.path == p);
        }
        for (int i = buildScenes.Count - 1; i >= 0; i--)
        {
            existing.Insert(0, buildScenes[i]);
        }
        EditorBuildSettings.scenes = existing.ToArray();
    }

    static void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f?.SetValue(obj, value);
    }
}
