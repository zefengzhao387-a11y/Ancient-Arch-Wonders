using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 创建第三章场景：Tools -> 创建第三章场景
/// </summary>
public static class CreateChapter3Scenes
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

    [MenuItem("Tools/创建第三章场景/0.第三章插图")]
    public static void CreateChapter3Title()
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
        bg.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f);

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
        titleTxt.text = "第三章";
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

        var ctrl = canvas.AddComponent<ChapterTransitionController>();
        SetField(ctrl, "illustrationImage", illuImg);
        SetField(ctrl, "illustrationSprite", (Sprite)null);
        SetField(ctrl, "titleText", titleTxt);
        SetField(ctrl, "titleString", "第三章");
        SetField(ctrl, "nextChapterButton", nextBtnComp);
        SetField(ctrl, "nextChapterButtonText", nextBtnTxt);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "nextSceneName", "Chapter3Bridge");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Chapter3Title.scene");
        var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        var path = "Assets/Scenes/Chapter3Title.scene";
        if (!list.Exists(x => x.path == path))
        {
            int idx = list.FindIndex(x => x.path.Contains("Chapter3Bridge"));
            list.Insert(idx >= 0 ? idx : list.Count, new UnityEditor.EditorBuildSettingsScene(path, true));
            UnityEditor.EditorBuildSettings.scenes = list.ToArray();
        }
        Debug.Log("第三章插图已创建。请将插图拖到 Illustration 的 Source Image。将 Chapter2VideoEnd 的 nextSceneName 改为 Chapter3Title。");
    }

    [MenuItem("Tools/创建第三章场景/卢沟桥场景")]
    public static void CreateChapter3Bridge()
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

        var root = CreateChild(canvas.transform, "GameRoot");
        SetFullRect(root);

        var bg = CreateChild(root.transform, "Background");
        SetFullRect(bg);
        bg.AddComponent<Image>().color = new Color(0.4f, 0.35f, 0.3f);

        var bridge = CreateChild(root.transform, "Bridge");
        var brRect = bridge.AddComponent<RectTransform>();
        brRect.anchorMin = new Vector2(0, 0.15f);
        brRect.anchorMax = new Vector2(1, 0.5f);
        brRect.offsetMin = brRect.offsetMax = Vector2.zero;
        bridge.AddComponent<Image>().color = new Color(0.5f, 0.45f, 0.4f);

        var platform = CreateChild(root.transform, "Platform_桥面");
        var plRect = platform.AddComponent<RectTransform>();
        plRect.anchorMin = new Vector2(0, 0.35f);
        plRect.anchorMax = new Vector2(1, 0.42f);
        plRect.offsetMin = plRect.offsetMax = Vector2.zero;
        var plImg = platform.AddComponent<Image>();
        plImg.color = new Color(0.5f, 0.45f, 0.4f, 0f);
        platform.AddComponent<PlatformHeightMap>();

        var centerZone = CreateChild(root.transform, "BridgeCenterZone");
        var czRect = centerZone.AddComponent<RectTransform>();
        czRect.anchorMin = new Vector2(0.45f, 0.3f);
        czRect.anchorMax = new Vector2(0.55f, 0.5f);
        czRect.offsetMin = czRect.offsetMax = Vector2.zero;

        var player = CreateChild(root.transform, "Player");
        var prRect = player.AddComponent<RectTransform>();
        prRect.anchorMin = prRect.anchorMax = new Vector2(0, 0.38f);
        prRect.pivot = new Vector2(0.5f, 0);
        prRect.anchoredPosition = new Vector2(200, 0);
        prRect.sizeDelta = new Vector2(60, 90);
        var pImg = player.AddComponent<Image>();
        pImg.color = new Color(0.3f, 0.5f, 0.8f);
        var charImg = CharacterSetupUtils.AddFootShadowAndWarmTint(player, 60, 90);

        var dialogPanel = CreateChild(canvas.transform, "DialogPanel");
        SetFullRect(dialogPanel);
        var dlgBg = dialogPanel.AddComponent<Image>();
        dlgBg.color = new Color(0, 0, 0, 0.5f);
        var dlgBox = CreateChild(dialogPanel.transform, "DialogBox");
        var dbRect = dlgBox.AddComponent<RectTransform>();
        dbRect.anchorMin = new Vector2(0.2f, 0.3f);
        dbRect.anchorMax = new Vector2(0.8f, 0.7f);
        dbRect.offsetMin = dbRect.offsetMax = Vector2.zero;
        var dlgBg1 = CreateChild(dlgBox.transform, "DialogBg1");
        SetFullRect(dlgBg1);
        dlgBg1.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.15f, 0.95f);
        var dlgBg2 = CreateChild(dlgBox.transform, "DialogBg2");
        SetFullRect(dlgBg2);
        dlgBg2.AddComponent<Image>().color = new Color(0.25f, 0.2f, 0.18f, 0.95f);
        dlgBg2.SetActive(false);
        var dlgBg3 = CreateChild(dlgBox.transform, "DialogBg3");
        SetFullRect(dlgBg3);
        dlgBg3.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.2f, 0.95f);
        dlgBg3.SetActive(false);
        var dlgTxt = CreateChild(dlgBox.transform, "Text");
        var dtRect = dlgTxt.AddComponent<RectTransform>();
        dtRect.anchorMin = Vector2.zero;
        dtRect.anchorMax = Vector2.one;
        dtRect.offsetMin = new Vector2(30, 60);
        dtRect.offsetMax = new Vector2(-30, -80);
        var dt = dlgTxt.AddComponent<Text>();
        dt.text = "对话内容";
        dt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dt.fontSize = 28;
        dt.color = Color.white;
        var dlgBtn = CreateChild(dlgBox.transform, "ContinueBtn");
        var dbBtnRect = dlgBtn.AddComponent<RectTransform>();
        dbBtnRect.anchorMin = new Vector2(0.5f, 0.05f);
        dbBtnRect.anchorMax = new Vector2(0.5f, 0.15f);
        dbBtnRect.pivot = new Vector2(0.5f, 0);
        dbBtnRect.anchoredPosition = Vector2.zero;
        dbBtnRect.sizeDelta = new Vector2(120, 40);
        var dbBtn = dlgBtn.AddComponent<Button>();
        dbBtn.targetGraphic = dlgBtn.AddComponent<Image>();
        ((Image)dbBtn.targetGraphic).color = new Color(0.3f, 0.5f, 0.7f);
        var dbBtnTxt = CreateChild(dlgBtn.transform, "Text");
        SetFullRect(dbBtnTxt);
        var dbBtnT = dbBtnTxt.AddComponent<Text>();
        dbBtnT.text = "继续";
        dbBtnT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dbBtnT.fontSize = 24;
        dbBtnT.alignment = TextAnchor.MiddleCenter;
        dbBtnT.color = Color.white;

        var pressE = CreateChild(canvas.transform, "PressEPrompt");
        var peRect = pressE.AddComponent<RectTransform>();
        peRect.anchorMin = peRect.anchorMax = new Vector2(0.5f, 0.12f);
        peRect.pivot = new Vector2(0.5f, 0);
        peRect.sizeDelta = new Vector2(250, 40);
        var peTxt = pressE.AddComponent<Text>();
        peTxt.text = "按 E 进入游戏";
        peTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        peTxt.fontSize = 28;
        peTxt.alignment = TextAnchor.MiddleCenter;
        peTxt.color = Color.white;

        var introPanel = CreateChild(canvas.transform, "IntroPanel");
        SetFullRect(introPanel);
        introPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        var introImg = CreateChild(introPanel.transform, "IntroImage");
        SetFullRect(introImg);
        introImg.AddComponent<Image>().color = Color.white;
        var introBtn = CreateChild(introPanel.transform, "ContinueBtn");
        var introBtnRect = introBtn.AddComponent<RectTransform>();
        introBtnRect.anchorMin = new Vector2(0.5f, 0.08f);
        introBtnRect.anchorMax = new Vector2(0.5f, 0.15f);
        introBtnRect.pivot = new Vector2(0.5f, 0);
        introBtnRect.sizeDelta = new Vector2(150, 50);
        var introBtnC = introBtn.AddComponent<Button>();
        introBtnC.targetGraphic = introBtn.AddComponent<Image>();
        ((Image)introBtnC.targetGraphic).color = new Color(0.2f, 0.5f, 0.3f);
        var introBtnTxt = CreateChild(introBtn.transform, "Text");
        SetFullRect(introBtnTxt);
        var introBtnT = introBtnTxt.AddComponent<Text>();
        introBtnT.text = "继续";
        introBtnT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        introBtnT.fontSize = 28;
        introBtnT.alignment = TextAnchor.MiddleCenter;
        introBtnT.color = Color.white;

        var compassPanel = CreateChild(canvas.transform, "CompassPanel");
        SetFullRect(compassPanel);
        compassPanel.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.25f);
        var compassBg = CreateChild(compassPanel.transform, "CompassBg");
        var cbRect = compassBg.AddComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(0.25f, 0.2f);
        cbRect.anchorMax = new Vector2(0.55f, 0.8f);
        cbRect.offsetMin = cbRect.offsetMax = Vector2.zero;
        var cbImg = compassBg.AddComponent<Image>();
        cbImg.color = new Color(0.3f, 0.25f, 0.2f);
        var cbBtn = compassBg.AddComponent<Button>();
        cbBtn.targetGraphic = cbImg;
        cbBtn.transition = Selectable.Transition.None;
        var pointer = CreateChild(compassBg.transform, "Pointer");
        var ptrRect = pointer.AddComponent<RectTransform>();
        ptrRect.anchorMin = ptrRect.anchorMax = new Vector2(0.5f, 0.5f);
        ptrRect.pivot = new Vector2(0.5f, 0.2f);
        ptrRect.sizeDelta = new Vector2(20, 120);
        ptrRect.anchoredPosition = Vector2.zero;
        pointer.AddComponent<Image>().color = Color.red;
        var ptrBtn = pointer.AddComponent<Button>();
        ptrBtn.targetGraphic = pointer.GetComponent<Image>();
        ptrBtn.transition = Selectable.Transition.None;
        var seasonArea = CreateChild(compassPanel.transform, "SeasonArea");
        var saRect = seasonArea.AddComponent<RectTransform>();
        saRect.anchorMin = new Vector2(0.6f, 0.2f);
        saRect.anchorMax = new Vector2(0.95f, 0.8f);
        saRect.offsetMin = saRect.offsetMax = Vector2.zero;
        var seasonImgList = new Image[4];
        var seasonVideoList = new RawImage[4];
        var seasonVpList = new VideoPlayer[4];
        for (int i = 0; i < 4; i++)
        {
            var si = CreateChild(seasonArea.transform, "Season" + i);
            var siRect = si.AddComponent<RectTransform>();
            siRect.anchorMin = new Vector2(0, 1f - (i + 1) * 0.25f);
            siRect.anchorMax = new Vector2(1, 1f - i * 0.25f);
            siRect.offsetMin = new Vector2(5, 5);
            siRect.offsetMax = new Vector2(-5, -5);
            seasonImgList[i] = si.AddComponent<Image>();
            seasonImgList[i].color = new Color(0.4f, 0.5f, 0.6f, 0.8f);
            var vd = CreateChild(si.transform, "VideoDisplay");
            SetFullRect(vd);
            vd.transform.SetAsLastSibling();
            var vdRaw = vd.AddComponent<RawImage>();
            vdRaw.color = Color.white;
            vd.SetActive(false);
            var vp = vd.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.renderMode = VideoRenderMode.RenderTexture;
            seasonVideoList[i] = vdRaw;
            seasonVpList[i] = vp;
        }
        var countdown = CreateChild(compassPanel.transform, "Countdown");
        var cdRect = countdown.AddComponent<RectTransform>();
        cdRect.anchorMin = new Vector2(0.02f, 0.92f);
        cdRect.anchorMax = new Vector2(0.15f, 0.98f);
        cdRect.offsetMin = cdRect.offsetMax = Vector2.zero;
        var cdTxt = countdown.AddComponent<Text>();
        cdTxt.text = "60";
        cdTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cdTxt.fontSize = 36;
        cdTxt.color = Color.yellow;

        var popupPanel = CreateChild(canvas.transform, "PopupPanel");
        SetFullRect(popupPanel);
        popupPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        var popupBox = CreateChild(popupPanel.transform, "PopupBox");
        var pbRect = popupBox.AddComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(0.25f, 0.35f);
        pbRect.anchorMax = new Vector2(0.75f, 0.65f);
        pbRect.offsetMin = pbRect.offsetMax = Vector2.zero;
        popupBox.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.15f, 0.95f);
        var popupTxt = CreateChild(popupBox.transform, "Text");
        var ptRect = popupTxt.AddComponent<RectTransform>();
        ptRect.anchorMin = Vector2.zero;
        ptRect.anchorMax = Vector2.one;
        ptRect.offsetMin = new Vector2(30, 60);
        ptRect.offsetMax = new Vector2(-30, -80);
        var pt = popupTxt.AddComponent<Text>();
        pt.text = "弹窗内容";
        pt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pt.fontSize = 26;
        pt.color = Color.white;
        var popupBtn = CreateChild(popupBox.transform, "ContinueBtn");
        var popupBtnRect = popupBtn.AddComponent<RectTransform>();
        popupBtnRect.anchorMin = new Vector2(0.5f, 0.05f);
        popupBtnRect.anchorMax = new Vector2(0.5f, 0.15f);
        popupBtnRect.pivot = new Vector2(0.5f, 0);
        popupBtnRect.sizeDelta = new Vector2(120, 40);
        var popupBtnC = popupBtn.AddComponent<Button>();
        popupBtnC.targetGraphic = popupBtn.AddComponent<Image>();
        ((Image)popupBtnC.targetGraphic).color = new Color(0.3f, 0.5f, 0.7f);
        var popupBtnTxt = CreateChild(popupBtn.transform, "Text");
        SetFullRect(popupBtnTxt);
        var popupBtnT = popupBtnTxt.AddComponent<Text>();
        popupBtnT.text = "继续";
        popupBtnT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupBtnT.fontSize = 24;
        popupBtnT.alignment = TextAnchor.MiddleCenter;
        popupBtnT.color = Color.white;

        var knowledgePanel = CreateChild(canvas.transform, "KnowledgePanel");
        SetFullRect(knowledgePanel);
        var knRootImg = knowledgePanel.AddComponent<Image>();
        knRootImg.color = new Color(0f, 0f, 0f, 0f);
        knRootImg.raycastTarget = true;
        var knowImg = CreateChild(knowledgePanel.transform, "KnowledgeImage");
        SetFullRect(knowImg);
        knowImg.AddComponent<Image>().color = Color.white;
        var knowNext = CreateChild(knowledgePanel.transform, "NextBtn");
        var knRect = knowNext.AddComponent<RectTransform>();
        knRect.anchorMin = new Vector2(0.9f, 0.45f);
        knRect.anchorMax = new Vector2(0.98f, 0.55f);
        knRect.offsetMin = knRect.offsetMax = Vector2.zero;
        var knImg = knowNext.AddComponent<Image>();
        knImg.color = new Color(0.5f, 0.5f, 0.6f);
        var knBtn = knowNext.AddComponent<Button>();
        knBtn.targetGraphic = knImg;
        var knTxt = CreateChild(knowNext.transform, "Text");
        SetFullRect(knTxt);
        var knT = knTxt.AddComponent<Text>();
        knT.text = "继续";
        knT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        knT.fontSize = 48;
        knT.alignment = TextAnchor.MiddleCenter;
        knT.color = Color.white;

        var postWinVideoRoot = CreateChild(canvas.transform, "PostWinVideoPanel");
        postWinVideoRoot.SetActive(false);
        SetFullRect(postWinVideoRoot);
        postWinVideoRoot.AddComponent<Image>().color = Color.black;
        var postWinVd = CreateChild(postWinVideoRoot.transform, "VideoDisplay");
        SetFullRect(postWinVd);
        var postWinRaw = postWinVd.AddComponent<RawImage>();
        postWinRaw.color = Color.black;
        postWinRaw.raycastTarget = false;
        var postWinVp = postWinVd.AddComponent<VideoPlayer>();
        postWinVp.playOnAwake = false;
        postWinVp.renderMode = VideoRenderMode.RenderTexture;

        var postWinDialog = CreateChild(canvas.transform, "PostWinDialogPanel");
        postWinDialog.SetActive(false);
        SetFullRect(postWinDialog);
        var postWinDlgRootImg = postWinDialog.AddComponent<Image>();
        postWinDlgRootImg.color = new Color(0f, 0f, 0f, 0f);
        postWinDlgRootImg.raycastTarget = true;
        var postWinDlgCg = postWinDialog.AddComponent<CanvasGroup>();
        postWinDlgCg.alpha = 0f;
        postWinDlgCg.interactable = false;
        postWinDlgCg.blocksRaycasts = false;
        var postWinDlgImgGo = CreateChild(postWinDialog.transform, "DialogIllustration");
        var pwdImgRt = postWinDlgImgGo.AddComponent<RectTransform>();
        pwdImgRt.anchorMin = new Vector2(0.08f, 0.14f);
        pwdImgRt.anchorMax = new Vector2(0.92f, 0.82f);
        pwdImgRt.offsetMin = pwdImgRt.offsetMax = Vector2.zero;
        var postWinDlgImg = postWinDlgImgGo.AddComponent<Image>();
        postWinDlgImg.color = Color.white;
        postWinDlgImg.preserveAspect = true;
        postWinDlgImg.raycastTarget = false;
        postWinDlgImg.enabled = false;
        var postWinDlgBtn = CreateChild(postWinDialog.transform, "ContinueBtn");
        var pwdBtnRt = postWinDlgBtn.AddComponent<RectTransform>();
        pwdBtnRt.anchorMin = new Vector2(0.5f, 0.06f);
        pwdBtnRt.anchorMax = new Vector2(0.5f, 0.06f);
        pwdBtnRt.pivot = new Vector2(0.5f, 0);
        pwdBtnRt.anchoredPosition = Vector2.zero;
        pwdBtnRt.sizeDelta = new Vector2(200, 48);
        var pwdBtn = postWinDlgBtn.AddComponent<Button>();
        pwdBtn.targetGraphic = postWinDlgBtn.AddComponent<Image>();
        ((Image)pwdBtn.targetGraphic).color = new Color(0.25f, 0.45f, 0.65f);
        var pwdBtnTxt = CreateChild(postWinDlgBtn.transform, "Text");
        SetFullRect(pwdBtnTxt);
        var pwdBtnT = pwdBtnTxt.AddComponent<Text>();
        pwdBtnT.text = "继续";
        pwdBtnT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pwdBtnT.fontSize = 26;
        pwdBtnT.alignment = TextAnchor.MiddleCenter;
        pwdBtnT.color = Color.white;

        var bridgeVoice = canvas.AddComponent<AudioSource>();
        bridgeVoice.playOnAwake = false;
        var postWinDialogVoice = canvas.AddComponent<AudioSource>();
        postWinDialogVoice.playOnAwake = false;

        var ctrl = canvas.AddComponent<Chapter3BridgeController>();
        SetField(ctrl, "playerRect", prRect);
        SetField(ctrl, "platforms", new RectTransform[] { plRect });
        SetField(ctrl, "bridgeCenterZone", czRect);
        SetField(ctrl, "playerImage", charImg);
        SetField(ctrl, "dialogPanel", dialogPanel);
        SetField(ctrl, "dialogBoxImages", new Image[] { dlgBg1.GetComponent<Image>(), dlgBg2.GetComponent<Image>(), dlgBg3.GetComponent<Image>() });
        SetField(ctrl, "dialogText", dt);
        SetField(ctrl, "dialogContinueBtn", dbBtn);
        SetField(ctrl, "bridgeDialogs", new string[] { "第一段对话内容", "第二段对话内容", "第三段对话内容" });
        SetField(ctrl, "bridgeDialogVoiceSource", bridgeVoice);
        SetField(ctrl, "postWinDialogVoiceSource", postWinDialogVoice);
        SetField(ctrl, "pressEPrompt", pressE);
        SetField(ctrl, "introPanel", introPanel);
        SetField(ctrl, "introImage", introImg.GetComponent<Image>());
        SetField(ctrl, "introContinueBtn", introBtnC);
        SetField(ctrl, "compassPanel", compassPanel);
        SetField(ctrl, "compassPointer", ptrRect);
        SetField(ctrl, "seasonImages", seasonImgList);
        SetField(ctrl, "seasonVideoDisplays", seasonVideoList);
        SetField(ctrl, "seasonVideoPlayers", seasonVpList);
        SetField(ctrl, "countdownText", cdTxt);
        SetField(ctrl, "popupPanel", popupPanel);
        SetField(ctrl, "popupText", pt);
        SetField(ctrl, "popupContinueBtn", popupBtnC);
        SetField(ctrl, "knowledgePanel", knowledgePanel);
        SetField(ctrl, "knowledgeImage", knowImg.GetComponent<Image>());
        SetField(ctrl, "knowledgeNextBtn", knBtn);
        SetField(ctrl, "postWinVideoRoot", postWinVideoRoot);
        SetField(ctrl, "postWinVideoPlayer", postWinVp);
        SetField(ctrl, "postWinVideoDisplay", postWinRaw);
        SetField(ctrl, "postWinDialogPanel", postWinDialog);
        SetField(ctrl, "postWinDialogImage", postWinDlgImg);
        SetField(ctrl, "postWinDialogContinueBtn", pwdBtn);
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());

        var ptrClick = canvas.GetComponent<Chapter3BridgeController>();
        ptrBtn.onClick.AddListener(ptrClick.OnPointerClick);
        cbBtn.onClick.AddListener(ptrClick.OnPointerClick);

        SetField(ctrl, "nextSceneName", "GameEnding");

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();

        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Scenes")) UnityEditor.AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Chapter3Bridge.scene");

        var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        if (!list.Exists(x => x.path == "Assets/Scenes/Chapter3Bridge.scene"))
            list.Add(new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Chapter3Bridge.scene", true));
        UnityEditor.EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("第三章卢沟桥场景已创建。请：1) 将桥图拖到 Background；2) 给 Platform_桥面 添加桥面 Sprite 并烘焙高度图；3) 拖入乔乔行走帧、罗盘形象、玩法介绍图、四季图、知识补充图；4) 罗盘通关后：PostWinVideoClip 或 StreamingAssets/chapter3_post_compass.mp4，PostWinDialogSprite 为图对话框，Post Win Dialog Voice Clip 为通关对话配音；5) bridgeDialogVoiceClips 与 bridgeDialogs 每段配音（可留空）。");
    }

    [MenuItem("Tools/创建第三章场景/游戏结尾视频")]
    public static void CreateGameEnding()
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

        var openingPhase = CreateChild(canvas.transform, "OpeningPhase");
        SetFullRect(openingPhase);
        var openingRootImg = openingPhase.AddComponent<Image>();
        openingRootImg.color = new Color(0.08f, 0.08f, 0.1f);
        openingRootImg.raycastTarget = false;
        var openIllu = CreateChild(openingPhase.transform, "OpeningImage");
        var oiRt = openIllu.AddComponent<RectTransform>();
        oiRt.anchorMin = new Vector2(0.06f, 0.18f);
        oiRt.anchorMax = new Vector2(0.94f, 0.82f);
        oiRt.offsetMin = oiRt.offsetMax = Vector2.zero;
        var openImg = openIllu.AddComponent<Image>();
        openImg.color = Color.white;
        openImg.preserveAspect = true;
        openImg.raycastTarget = false;
        // 字幕区：仅 Rect 容器 + Text；渐变底条由 SubtitleStyleUtility 运行时创建（与 Text 同父），避免「条形容器=父物体」导致整条被 Layout 拉到屏幕中线与首图分离。
        var openSubDock = CreateChild(openingPhase.transform, "OpeningSubtitleDock");
        var osdRt = openSubDock.AddComponent<RectTransform>();
        osdRt.anchorMin = new Vector2(0.06f, 0.04f);
        osdRt.anchorMax = new Vector2(0.94f, 0.16f);
        osdRt.offsetMin = osdRt.offsetMax = Vector2.zero;
        var openSub = CreateChild(openSubDock.transform, "OpeningSubtitle");
        SetFullRect(openSub);
        var openSubTxt = openSub.AddComponent<Text>();
        openSubTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        openSubTxt.fontSize = 26;
        openSubTxt.alignment = TextAnchor.MiddleCenter;
        openSubTxt.color = new Color(0.16f, 0.16f, 0.2f);
        openSubTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        openSubTxt.verticalOverflow = VerticalWrapMode.Overflow;
        openSubTxt.raycastTarget = false;
        openSub.GetComponent<RectTransform>().offsetMin = new Vector2(20, 10);
        openSub.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -10);

        var videoPhaseRoot = CreateChild(canvas.transform, "VideoPhase");
        videoPhaseRoot.SetActive(false);
        SetFullRect(videoPhaseRoot);
        var vprBg = videoPhaseRoot.AddComponent<Image>();
        vprBg.color = Color.black;
        vprBg.raycastTarget = false;
        var videoArea = CreateChild(videoPhaseRoot.transform, "VideoArea");
        var vaRt = videoArea.AddComponent<RectTransform>();
        vaRt.anchorMin = new Vector2(0f, 0.3f);
        vaRt.anchorMax = new Vector2(1f, 1f);
        vaRt.offsetMin = new Vector2(24, 12);
        vaRt.offsetMax = new Vector2(-24, -8);
        var vaBg = videoArea.AddComponent<Image>();
        vaBg.color = new Color(0, 0, 0, 0.35f);
        vaBg.raycastTarget = false;
        var vd = CreateChild(videoArea.transform, "VideoDisplay");
        SetFullRect(vd);
        var vdRaw = vd.AddComponent<RawImage>();
        vdRaw.color = Color.white;
        vdRaw.raycastTarget = false;
        var vp = videoArea.AddComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;
        var subDock = CreateChild(videoPhaseRoot.transform, "VideoSubtitleDock");
        var sdRt = subDock.AddComponent<RectTransform>();
        sdRt.anchorMin = new Vector2(0.04f, 0.02f);
        sdRt.anchorMax = new Vector2(0.96f, 0.28f);
        sdRt.offsetMin = sdRt.offsetMax = Vector2.zero;
        var vSub = CreateChild(subDock.transform, "VideoSubtitle");
        SetFullRect(vSub);
        var vSubTxt = vSub.AddComponent<Text>();
        vSubTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        vSubTxt.fontSize = 24;
        vSubTxt.alignment = TextAnchor.MiddleCenter;
        vSubTxt.color = new Color(0.16f, 0.16f, 0.2f);
        vSubTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        vSubTxt.verticalOverflow = VerticalWrapMode.Overflow;
        vSubTxt.raycastTarget = false;
        vSub.GetComponent<RectTransform>().offsetMin = new Vector2(20, 10);
        vSub.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -10);

        var middlePhase = CreateChild(canvas.transform, "MiddleStillPhase");
        middlePhase.SetActive(false);
        SetFullRect(middlePhase);
        var midRoot = middlePhase.AddComponent<Image>();
        midRoot.color = new Color(0.06f, 0.06f, 0.08f);
        midRoot.raycastTarget = false;
        var midImgGo = CreateChild(middlePhase.transform, "MiddleImage");
        SetFullRect(midImgGo);
        var midImg = midImgGo.AddComponent<Image>();
        midImg.color = Color.white;
        midImg.preserveAspect = true;
        midImg.raycastTarget = false;
        middlePhase.AddComponent<GameEndingMiddleHint>();

        var finalPhase = CreateChild(canvas.transform, "FinalStillPhase");
        finalPhase.SetActive(false);
        SetFullRect(finalPhase);
        var finRoot = finalPhase.AddComponent<Image>();
        finRoot.color = new Color(0.05f, 0.05f, 0.07f);
        finRoot.raycastTarget = false;
        var finImgGo = CreateChild(finalPhase.transform, "FinalImage");
        var fiRt = finImgGo.AddComponent<RectTransform>();
        fiRt.anchorMin = new Vector2(0.06f, 0.22f);
        fiRt.anchorMax = new Vector2(0.94f, 0.88f);
        fiRt.offsetMin = fiRt.offsetMax = Vector2.zero;
        var finImg = finImgGo.AddComponent<Image>();
        finImg.color = Color.white;
        finImg.preserveAspect = true;
        finImg.raycastTarget = false;
        var finSubDock = CreateChild(finalPhase.transform, "FinalSubtitleDock");
        var fsdRt = finSubDock.AddComponent<RectTransform>();
        fsdRt.anchorMin = new Vector2(0.06f, 0.04f);
        fsdRt.anchorMax = new Vector2(0.94f, 0.18f);
        fsdRt.offsetMin = fsdRt.offsetMax = Vector2.zero;
        var finSub = CreateChild(finSubDock.transform, "FinalSubtitle");
        SetFullRect(finSub);
        var finSubTxt = finSub.AddComponent<Text>();
        finSubTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        finSubTxt.fontSize = 26;
        finSubTxt.alignment = TextAnchor.MiddleCenter;
        finSubTxt.color = new Color(0.16f, 0.16f, 0.2f);
        finSubTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        finSubTxt.verticalOverflow = VerticalWrapMode.Overflow;
        finSubTxt.raycastTarget = false;
        finSub.GetComponent<RectTransform>().offsetMin = new Vector2(20, 10);
        finSub.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -10);

        var clickGo = CreateChild(canvas.transform, "ClickToAdvance");
        clickGo.SetActive(false);
        SetFullRect(clickGo);
        var cgImg = clickGo.AddComponent<Image>();
        cgImg.color = new Color(0, 0, 0, 0.02f);
        cgImg.raycastTarget = true;
        var clickBtn = clickGo.AddComponent<Button>();
        clickBtn.targetGraphic = cgImg;

        var voice = canvas.AddComponent<AudioSource>();
        voice.playOnAwake = false;

        var ctrl = canvas.AddComponent<GameEndingController>();
        SetField(ctrl, "fadeOverlay", canvas.transform.Find("FadeOverlay").GetComponent<Image>());
        SetField(ctrl, "voiceSource", voice);
        SetField(ctrl, "clickToAdvance", clickBtn);
        SetField(ctrl, "nextSceneName", "GameMenu");
        SetField(ctrl, "openingPhase", openingPhase);
        SetField(ctrl, "openingImage", openImg);
        SetField(ctrl, "openingSubtitle", openSubTxt);
        SetField(ctrl, "openingSubtitleBar", (Image)null);
        SetField(ctrl, "videoPhaseRoot", videoPhaseRoot);
        SetField(ctrl, "videoPlayer", vp);
        SetField(ctrl, "videoDisplay", vdRaw);
        SetField(ctrl, "videoSubtitleText", vSubTxt);
        SetField(ctrl, "videoSubtitleBar", (Image)null);
        SetField(ctrl, "middlePhase", middlePhase);
        SetField(ctrl, "middleImage", midImg);
        SetField(ctrl, "finalPhase", finalPhase);
        SetField(ctrl, "finalImage", finImg);
        SetField(ctrl, "finalSubtitle", finSubTxt);

        canvas.transform.Find("FadeOverlay").SetAsLastSibling();

        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Scenes")) UnityEditor.AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameEnding.scene");

        var list = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        if (!list.Exists(x => x.path == "Assets/Scenes/GameEnding.scene"))
            list.Add(new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/GameEnding.scene", true));
        UnityEditor.EditorBuildSettings.scenes = list.ToArray();

        Debug.Log("GameEnding 多段结尾场景已创建。Inspector：首图/中间图/最后图 Sprite；videoPart1/2 Clip 或 StreamingAssets ending_part1.mp4、ending_part2.mp4；各段字幕文案与配音；菜单 Tools→创建第三章场景→游戏结尾视频 可覆盖重建。");
    }
}
