using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏开头分镜序列播放器：
/// 支持图片点击推进、帧动画、短视频、黑屏转场；全部步骤结束后黑场→衔接视频→黑场→下一关。
/// </summary>
public class OpeningVideoController : MonoBehaviour
{
    private enum StepType
    {
        Image,
        FrameAnimation,
        Video,
        BlackTransition,
        /// <summary>已弃用：序列中若仍保留该类型将跳过且无画面，请删除该步并在「衔接视频」中配置过渡视频。</summary>
        TravelEffect
    }

    /// <summary>同一分镜内多句字幕（画面不变，按顺序播；不配则用顶栏单段 text/voice 逻辑）。</summary>
    [System.Serializable]
    public class OpeningSubtitleBeat
    {
        [TextArea(2, 8)] public string text;
        public AudioClip voice;
        [Tooltip("本句最短停留（秒），与配音长度取大（Image/黑场/帧动画；Video 顺序模式时用）")]
        public float minHold = 1f;
        [Tooltip("Image/黑场/帧动画：本句结束后是否需点击再进入下一句。Video 步多字幕时忽略，由整步 waitForClick 控制。")]
        public bool waitForClick = true;
        [Tooltip("仅 Video：该句出现的视频时间（秒），从本条视频开始播放算起。若所有句此项均为 0，则按列表顺序播：上一句 minHold/配音结束后切下一句，视频不暂停。")]
        public float showAtTimeInVideo;
    }

    [System.Serializable]
    private class OpeningStep
    {
        public StepType type = StepType.Image;
        [TextArea(2, 8)]
        [Tooltip("未使用下方「多句字幕」时作为本步唯一字幕；使用多句时首句可留空（以第一句 beat 为准）。")]
        public string text;
        [Tooltip("同一画面内多句：Image / 黑场 / 帧动画 / Video。Video 时在「Show At Time In Video」填出现时间；全 0 则按顺序随播放逐句切。")]
        public OpeningSubtitleBeat[] subtitleBeats;
        [Tooltip("图片步显示的图片")]
        public Sprite image;
        [Tooltip("帧动画步使用的序列帧")]
        public Sprite[] animationFrames;
        [Tooltip("帧动画播放间隔(秒)")]
        public float frameInterval = 0.12f;
        [Tooltip("视频步使用的视频")]
        public VideoClip video;
        [Tooltip("未拖 VideoClip 时尝试此 StreamingAssets 文件名（或传完整 URL）")]
        public string videoStreamingName;
        [Tooltip("视频步画面渐显时长(秒)，仅 Video 类型生效")]
        public float videoFadeInDuration = 0.2f;
        [Tooltip("独立配音，可留空")]
        public AudioClip voice;
        [Tooltip("最短停留时间(秒)")]
        public float minDuration = 0.1f;
        [Tooltip("本步骤结束后是否需要点击推进")]
        public bool waitForClick = true;
        [Tooltip("额外停顿(秒)")]
        public float extraDelay = 0f;
        [Tooltip("Video：播完后暂停在最后一帧（默认开）")]
        public bool pauseVideoOnLastFrame = true;
    }

    [Header("UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private Image storyImage;
    [SerializeField] private Text storyText;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("步骤序列")]
    [SerializeField] private OpeningStep[] openingSteps;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [Tooltip("已不使用：第一格不再做黑场渐显，仅保留避免已序列化数据丢失")]
    [SerializeField] private float firstFadeInDuration = 0.6f;
    [SerializeField] private float slideFadeOutDuration = 0.5f;
    [SerializeField] private float slideFadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [Tooltip("已废弃：片间一律黑场渐隐→全黑→换格→黑场渐显，仅用 slideFadeOutDuration / slideFadeInDuration")]
    [SerializeField] private int strictBlackFadeFromStepNumber = 6;
    [SerializeField] private float strictBlackSlideFadeInDuration = 0.55f;

    [Header("开场全部步骤结束后：黑场 → 衔接视频 → 黑场 → 下一关")]
    [Tooltip("拖 VideoClip，或与下方 Streaming 二选一")]
    [SerializeField] private VideoClip bridgeTransitionClip;
    [Tooltip("未拖 Clip 时尝试 StreamingAssets 下此文件，例如 intro_bridge.mp4")]
    [SerializeField] private string bridgeTransitionStreamingName = "";
    [SerializeField] private float bridgeVideoFadeInDuration = 0.45f;
    [Tooltip("衔接视频播完后渐黑再加载场景的时长")]
    [SerializeField] private float bridgeToChapterFadeDuration = 0.85f;

    [Header("场景跳转")]
    [SerializeField] private string nextSceneName = "Chapter1Intro";

    [Header("跳过开场")]
    [Tooltip("右上角「跳过开场白」直接进入的场景；默认第一章开场对话 Chapter1Intro。")]
    [SerializeField] private string skipTargetSceneName = "Chapter1Intro";
    [Tooltip("若已在场景里摆好按钮则拖入；留空则运行时自动生成。")]
    [SerializeField] private Button skipOpeningButton;

    private bool _clicked;
    private RenderTexture _videoRt;
    private static Sprite _solidBlackSprite;
    private bool _videoClipStepEnded;
    private Coroutine _playRoutine;
    private bool _skipUsed;
    private GameObject _runtimeSkipRoot;

    private void Start()
    {
        // 以下字段仅保留序列化兼容，不参与逻辑；读取一次以消除 CS0414
        _ = (firstFadeInDuration, strictBlackFadeFromStepNumber, strictBlackSlideFadeInDuration);

        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();

        EnsureStoryPanelBlackBackdrop();
        if (storyText != null)
            SubtitleStyleUtility.ApplyToSubtitle(storyText, null);

        if (fadeOverlay != null)
        {
            ConfigureFadeOverlayBlack();
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        if (storyPanel != null) storyPanel.SetActive(false);
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
            videoDisplay.color = new Color(1f, 1f, 1f, 0f);
            videoDisplay.texture = null;
        }
        EnsureOpeningSkipButton();
        _playRoutine = StartCoroutine(PlaySlidesThenLoad());
    }

    void EnsureOpeningSkipButton()
    {
        if (string.IsNullOrEmpty(skipTargetSceneName)) return;

        var btn = skipOpeningButton;
        if (btn == null)
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var all = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                foreach (var c in all)
                {
                    if (c != null && c.gameObject.name != "__AspectBarsCanvas")
                    {
                        canvas = c;
                        break;
                    }
                }
            }
            if (canvas == null) return;

            _runtimeSkipRoot = new GameObject("OpeningSkipRoot");
            _runtimeSkipRoot.transform.SetParent(canvas.transform, false);
            var rtRoot = _runtimeSkipRoot.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.offsetMin = Vector2.zero;
            rtRoot.offsetMax = Vector2.zero;
            rtRoot.pivot = new Vector2(0.5f, 0.5f);

            var overlayCanvas = _runtimeSkipRoot.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 9000;
            _runtimeSkipRoot.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("SkipButton");
            btnGo.transform.SetParent(_runtimeSkipRoot.transform, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(200f, 44f);

            var bg = btnGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.62f);
            bg.raycastTarget = true;

            btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.text = "跳过开场白";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = SubtitleStyleUtility.GetSubtitleFont() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = 24;
            skipOpeningButton = btn;
        }

        btn.onClick.RemoveListener(OnSkipOpeningClicked);
        btn.onClick.AddListener(OnSkipOpeningClicked);
        btn.gameObject.SetActive(true);
        btn.transform.SetAsLastSibling();

        var skipLabel = btn.GetComponentInChildren<Text>(true);
        if (skipLabel != null)
            skipLabel.text = "跳过开场白";
        var brt = btn.transform as RectTransform;
        if (brt != null && brt.sizeDelta.x < 200f)
            brt.sizeDelta = new Vector2(200f, Mathf.Max(brt.sizeDelta.y, 44f));
    }

    void OnSkipOpeningClicked()
    {
        if (_skipUsed) return;
        if (string.IsNullOrEmpty(skipTargetSceneName)) return;
        _skipUsed = true;

        StopAllCoroutines();
        _playRoutine = null;

        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
        StopVideo();
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
            videoDisplay.texture = null;
        }
        if (_videoRt != null)
        {
            _videoRt.Release();
            _videoRt = null;
        }

        SceneManager.LoadScene(skipTargetSceneName);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) _clicked = true;
    }

    private IEnumerator PlaySlidesThenLoad()
    {
        if (openingSteps == null || openingSteps.Length == 0)
        {
            ConfigureFadeOverlayBlack();
            SetFadeAlpha(1f);
            yield return PlayBridgeTransitionThenLoad();
            yield break;
        }

        if (storyPanel != null) storyPanel.SetActive(true);
        yield return PrepareStep(openingSteps[0]);
        ConfigureFadeOverlayBlack();
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            SetFadeAlpha(0f);
        }
        if (openingSteps[0].type == StepType.Image || openingSteps[0].type == StepType.FrameAnimation || openingSteps[0].type == StepType.BlackTransition)
        {
            if (storyImage != null && storyImage.gameObject.activeSelf)
                storyImage.color = Color.white;
        }
        yield return RunStepPlayback(openingSteps[0]);

        for (int i = 1; i < openingSteps.Length; i++)
        {
            yield return FadeOverlay(GetFadeOverlayAlpha(), 1f, slideFadeOutDuration);
            SetFadeAlpha(1f);
            ConfigureFadeOverlayBlack();
            if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
            yield return PrepareStep(openingSteps[i]);
            yield return null;
            yield return FadeFromBlackOverCurrentStep(openingSteps[i], slideFadeInDuration);
            yield return RunStepPlayback(openingSteps[i]);
        }

        yield return PlayBridgeTransitionThenLoad();
    }

    bool HasBridgeTransitionSource()
    {
        if (bridgeTransitionClip != null) return true;
        if (string.IsNullOrEmpty(bridgeTransitionStreamingName)) return false;
        return VideoPlaybackUtility.HasStreamingMediaSource(bridgeTransitionStreamingName);
    }

    private IEnumerator PlayBridgeTransitionThenLoad()
    {
        // VideoDisplay 在 StoryPanel 子级时，不能关掉整块 Panel，否则 RawImage 永远不渲染（会只有声音）
        if (storyPanel != null) storyPanel.SetActive(true);
        if (storyImage != null) storyImage.gameObject.SetActive(false);
        if (storyText != null) storyText.gameObject.SetActive(false);
        ShowSubtitle(string.Empty);
        ConfigureFadeOverlayBlack();
        fadeOverlay.transform.SetAsLastSibling();

        if (!HasBridgeTransitionSource())
        {
            yield return FadeOverlay(GetFadeOverlayAlpha(), 1f, fadeOutDuration);
            if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        yield return FadeOverlay(GetFadeOverlayAlpha(), 1f, fadeOutDuration);
        SetFadeAlpha(1f);

        StopVideo();
        HideVisuals();
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
            videoDisplay.color = new Color(1f, 1f, 1f, 0f);
            videoDisplay.texture = null;
        }

        yield return StartCoroutine(PlayBridgeVideoFromSource());

        yield return FadeOverlay(GetFadeOverlayAlpha(), 1f, bridgeToChapterFadeDuration);
        StopVideo();
        if (_videoRt != null)
        {
            _videoRt.Release();
            _videoRt = null;
        }
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator PlayBridgeVideoFromSource()
    {
        if (videoPlayer == null || videoDisplay == null) yield break;

        videoPlayer.Stop();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        if (bridgeTransitionClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = bridgeTransitionClip;
            videoPlayer.url = "";
        }
        else
        {
            var path = VideoPlaybackUtility.ResolveStreamingMediaUrl(bridgeTransitionStreamingName);
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = path;
            videoPlayer.clip = null;
        }

        bool bridgePrepFailed = false;
        VideoPlayer.ErrorEventHandler onBridgeErr = (v, msg) =>
        {
            bridgePrepFailed = true;
            VideoPlaybackUtility.LogVideoError(v, msg);
        };
        videoPlayer.errorReceived += onBridgeErr;
        try
        {
            videoPlayer.Prepare();
            float prep = 0f;
            while (!videoPlayer.isPrepared && !bridgePrepFailed && prep < 12f)
            {
                prep += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            videoPlayer.errorReceived -= onBridgeErr;
        }

        if (!videoPlayer.isPrepared || bridgePrepFailed) yield break;

        int w = (int)videoPlayer.width, h = (int)videoPlayer.height;
        if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
        if (_videoRt != null) _videoRt.Release();
        _videoRt = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
        videoPlayer.targetTexture = _videoRt;
        VideoPlaybackUtility.ApplyStandardCompat(videoPlayer);
        videoDisplay.texture = _videoRt;
        videoDisplay.color = new Color(1f, 1f, 1f, 0f);
        videoDisplay.gameObject.SetActive(true);
        if (videoDisplay.transform.parent != null)
            videoDisplay.transform.SetAsLastSibling();

        if (fadeOverlay != null)
        {
            ConfigureFadeOverlayBlack();
            fadeOverlay.transform.SetAsLastSibling();
        }

        bool ended = false;
        void OnEnd(VideoPlayer vp)
        {
            ended = true;
            vp.Pause();
        }
        videoPlayer.loopPointReached += OnEnd;
        videoPlayer.Play();

        float wait = 0f;
        while (videoPlayer.frame < 0 && wait < 4f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        if (bridgeVideoFadeInDuration > 0.001f)
            yield return CrossFadeOverlayDownAndRawImageUp(bridgeVideoFadeInDuration);
        else
        {
            SetFadeAlpha(0f);
            if (videoDisplay != null) videoDisplay.color = Color.white;
        }

        while (!ended) yield return null;
        videoPlayer.loopPointReached -= OnEnd;
    }

    void SetFadeAlpha(float a)
    {
        if (fadeOverlay == null) return;
        ConfigureFadeOverlayBlack();
        var c = fadeOverlay.color;
        c.a = a;
        fadeOverlay.color = new Color(0f, 0f, 0f, c.a);
    }

    /// <summary>遮罩当前 alpha，用于渐隐/渐显时作为起点，避免「已是全黑却从 0 插值」导致一帧闪透底白。</summary>
    float GetFadeOverlayAlpha()
    {
        if (fadeOverlay == null) return 0f;
        return fadeOverlay.color.a;
    }

    /// <summary>在 StoryPanel 最底层铺一块全屏纯黑，片间遮罩未盖住时也不露白底。</summary>
    void EnsureStoryPanelBlackBackdrop()
    {
        if (storyPanel == null) return;
        const string kName = "_BlackBackdrop";
        if (storyPanel.transform.Find(kName) != null) return;
        var go = new GameObject(kName);
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(storyPanel.transform, false);
        go.transform.SetAsFirstSibling();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = GetSolidBlackSprite();
        img.color = Color.black;
        img.raycastTarget = false;
    }

    static Sprite GetSolidBlackSprite()
    {
        if (_solidBlackSprite != null) return _solidBlackSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.black);
        tex.Apply();
        _solidBlackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _solidBlackSprite;
    }

    void ConfigureFadeOverlayBlack()
    {
        if (fadeOverlay == null) return;
        if (fadeOverlay.sprite == null)
        {
            fadeOverlay.sprite = GetSolidBlackSprite();
            fadeOverlay.type = Image.Type.Simple;
        }
        var c = fadeOverlay.color;
        fadeOverlay.color = new Color(0f, 0f, 0f, c.a);
    }

    /// <summary>黑场渐显到当前步：遮罩与图片/视频前景同步渐隐渐显，避免只揭遮罩时底下露白或空底。</summary>
    IEnumerator FadeFromBlackOverCurrentStep(OpeningStep step, float duration)
    {
        if (step == null)
        {
            yield return FadeOverlay(GetFadeOverlayAlpha(), 0f, Mathf.Max(0.01f, duration));
            yield break;
        }

        // Inspector 里渐显时长若为 0，旧逻辑会误走「只揭遮罩」且图仍 alpha=0，整格只剩黑底
        if (duration <= 0.001f)
            duration = Mathf.Max(0.35f, slideFadeInDuration);

        ConfigureFadeOverlayBlack();
        if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();

        // 视频步：不要在这里单独揭遮罩，否则底下是空 Canvas（常显白），应等 PlayVideo 首帧后再与画面一起从黑渐显
        if (step.type == StepType.Video)
        {
            if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
            yield break;
        }

        if (step.type == StepType.Image || step.type == StepType.FrameAnimation || step.type == StepType.BlackTransition)
        {
            if (storyImage != null && storyImage.gameObject.activeSelf)
            {
                var sc = storyImage.color;
                sc.a = 0f;
                storyImage.color = sc;
            }
            // 先按 u=0 画一帧再累加时间，避免首帧已带 alpha 导致「没有从黑渐显」
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float u = Mathf.Clamp01(elapsed / duration);
                if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 1f - u);
                if (storyImage != null && storyImage.gameObject.activeSelf)
                {
                    var sc = storyImage.color;
                    sc.a = u;
                    storyImage.color = sc;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            if (storyImage != null && storyImage.gameObject.activeSelf) storyImage.color = Color.white;
            yield break;
        }

        yield return FadeOverlay(GetFadeOverlayAlpha(), 0f, duration);
    }

    IEnumerator CrossFadeOverlayDownAndRawImageUp(float duration)
    {
        if (duration <= 0.001f || videoDisplay == null)
        {
            SetFadeAlpha(0f);
            if (videoDisplay != null) videoDisplay.color = Color.white;
            yield break;
        }
        ConfigureFadeOverlayBlack();
        if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float u = Mathf.Clamp01(elapsed / duration);
            if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 1f - u);
            videoDisplay.color = new Color(1f, 1f, 1f, u);
            yield return null;
            elapsed += Time.deltaTime;
        }
        if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        videoDisplay.color = Color.white;
    }

    private IEnumerator PrepareStep(OpeningStep step)
    {
        if (step == null) yield break;
        StopVideo();
        HideVisuals();

        if (step.type == StepType.BlackTransition)
        {
            ShowSubtitle(GetFirstSubtitleText(step));
            if (storyImage != null)
            {
                storyImage.sprite = GetSolidBlackSprite();
                storyImage.color = new Color(1f, 1f, 1f, 0f);
                storyImage.gameObject.SetActive(true);
            }
            yield break;
        }

        if (step.type == StepType.Image)
        {
            ShowSubtitle(GetFirstSubtitleText(step));
            if (storyImage != null)
            {
                storyImage.sprite = step.image;
                var ic = storyImage.color;
                ic.a = 0f;
                storyImage.color = ic;
                storyImage.gameObject.SetActive(true);
            }
        }
        else if (step.type == StepType.FrameAnimation)
        {
            ShowSubtitle(GetFirstSubtitleText(step));
            if (storyImage != null && step.animationFrames != null && step.animationFrames.Length > 0)
            {
                storyImage.sprite = step.animationFrames[0];
                var ic = storyImage.color;
                ic.a = 0f;
                storyImage.color = ic;
                storyImage.gameObject.SetActive(true);
            }
        }
        else if (step.type == StepType.Video)
        {
            ShowSubtitle(GetFirstSubtitleText(step));
        }
        else if (step.type == StepType.TravelEffect)
        {
            ShowSubtitle(string.Empty);
            Debug.LogWarning("OpeningVideoController：步骤类型 TravelEffect 已弃用且无穿越画面，请从 openingSteps 中删除该步；过渡请用「衔接视频」。");
        }
    }

    private IEnumerator RunStepPlayback(OpeningStep step)
    {
        if (step == null) yield break;
        if (step.type == StepType.BlackTransition)
        {
            if (HasSubtitleBeats(step))
                yield return RunSubtitleBeatSequence(step);
            else
            {
                yield return WaitStepTiming(step, 0f);
                if (step.waitForClick) yield return WaitForClick();
            }
            yield break;
        }

        if (step.type == StepType.Image)
        {
            if (HasSubtitleBeats(step))
                yield return RunSubtitleBeatSequence(step);
            else
            {
                float voiceDuration = PlayVoice(step.voice);
                yield return WaitStepTiming(step, voiceDuration);
            }
        }
        else if (step.type == StepType.FrameAnimation)
        {
            if (HasSubtitleBeats(step))
                yield return RunSubtitleBeatSequence(step);
            float voiceDuration = !HasSubtitleBeats(step) ? PlayVoice(step.voice) : 0f;
            float animDuration = 0f;
            if (step.animationFrames != null) animDuration = step.animationFrames.Length * Mathf.Max(0.01f, step.frameInterval);
            yield return StartCoroutine(PlayFrameAnimation(step.animationFrames, Mathf.Max(0.01f, step.frameInterval)));
            float totalTarget = Mathf.Max(Mathf.Max(Mathf.Max(0.1f, step.minDuration), voiceDuration), animDuration) + Mathf.Max(0f, step.extraDelay);
            float remain = Mathf.Max(0f, totalTarget - animDuration);
            if (remain > 0f) yield return new WaitForSeconds(remain);
        }
        else if (step.type == StepType.Video)
        {
            if (!HasSubtitleBeats(step))
                ShowSubtitle(step.text ?? string.Empty);
            float waited = 0f;
            yield return StartCoroutine(PlayVideoForStep(step, v => waited = v));
            float totalTarget = Mathf.Max(Mathf.Max(0.1f, step.minDuration), waited) + Mathf.Max(0f, step.extraDelay);
            float remain = Mathf.Max(0f, totalTarget - waited);
            if (remain > 0f) yield return new WaitForSeconds(remain);
        }
        else if (step.type == StepType.TravelEffect)
        {
            ShowSubtitle(step.text);
            float voiceDuration = PlayVoice(step.voice);
            yield return WaitStepTiming(step, voiceDuration);
        }
        if (step.waitForClick)
        {
            if (!HasSubtitleBeats(step))
                yield return WaitForClick();
            else if (step.type == StepType.FrameAnimation || step.type == StepType.Video)
                yield return WaitForClick();
        }
    }

    static bool HasSubtitleBeats(OpeningStep step) =>
        step != null && step.subtitleBeats != null && step.subtitleBeats.Length > 0;

    static string GetFirstSubtitleText(OpeningStep step)
    {
        if (HasSubtitleBeats(step))
        {
            var t = step.subtitleBeats[0].text;
            if (!string.IsNullOrEmpty(t)) return t;
        }
        return step.text ?? string.Empty;
    }

    IEnumerator RunSubtitleBeatSequence(OpeningStep step)
    {
        if (!HasSubtitleBeats(step)) yield break;
        for (int i = 0; i < step.subtitleBeats.Length; i++)
        {
            var b = step.subtitleBeats[i];
            ShowSubtitle(b.text ?? string.Empty);
            float vd = PlayVoice(b.voice);
            bool last = i == step.subtitleBeats.Length - 1;
            float extra = last ? Mathf.Max(0f, step.extraDelay) : 0f;
            float w = Mathf.Max(Mathf.Max(0.1f, b.minHold), vd) + extra;
            yield return new WaitForSeconds(w);
            if (b.waitForClick)
                yield return WaitForClick();
        }
    }

    private void HideVisuals()
    {
        if (storyImage != null) storyImage.gameObject.SetActive(false);
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
            videoDisplay.color = new Color(1f, 1f, 1f, 0f);
            videoDisplay.texture = null;
        }
    }

    private void ShowSubtitle(string text)
    {
        if (storyText != null) storyText.text = text ?? string.Empty;
    }

    private IEnumerator PlayFrameAnimation(Sprite[] frames, float interval)
    {
        if (storyImage == null || frames == null || frames.Length == 0) yield break;
        storyImage.gameObject.SetActive(true);
        for (int i = 0; i < frames.Length; i++)
        {
            storyImage.sprite = frames[i];
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PlayVideoForStep(OpeningStep step, System.Action<float> onWaited)
    {
        VideoClip clip = step != null ? step.video : null;
        string streamingName = step != null ? step.videoStreamingName : "";
        AudioClip dub = step != null ? step.voice : null;
        float fadeInDuration = step != null ? Mathf.Max(0f, step.videoFadeInDuration) : 0f;
        bool hasBeats = HasSubtitleBeats(step);

        float voiceDuration = !hasBeats ? PlayVoice(dub) : 0f;
        if (videoPlayer == null || videoDisplay == null)
        {
            if (voiceDuration > 0f) yield return new WaitForSeconds(voiceDuration);
            onWaited?.Invoke(voiceDuration);
            yield break;
        }

        videoPlayer.Stop();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            videoPlayer.url = "";
        }
        else if (VideoPlaybackUtility.HasStreamingMediaSource(streamingName))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = VideoPlaybackUtility.ResolveStreamingMediaUrl(streamingName);
            videoPlayer.clip = null;
        }
        else
        {
            if (voiceDuration > 0f) yield return new WaitForSeconds(voiceDuration);
            onWaited?.Invoke(voiceDuration);
            yield break;
        }
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        bool stepPrepFailed = false;
        VideoPlayer.ErrorEventHandler onStepErr = (v, msg) =>
        {
            stepPrepFailed = true;
            VideoPlaybackUtility.LogVideoError(v, msg);
        };
        videoPlayer.errorReceived += onStepErr;
        try
        {
            videoPlayer.Prepare();
            float timeout = 10f;
            float t = 0f;
            while (!videoPlayer.isPrepared && !stepPrepFailed && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            videoPlayer.errorReceived -= onStepErr;
        }

        if (!videoPlayer.isPrepared || stepPrepFailed)
        {
            if (voiceDuration > 0f) yield return new WaitForSeconds(voiceDuration);
            onWaited?.Invoke(voiceDuration);
            yield break;
        }

        int w = (int)videoPlayer.width;
        int h = (int)videoPlayer.height;
        if (w <= 0 || h <= 0) { w = 1280; h = 720; }
        if (_videoRt != null) _videoRt.Release();
        _videoRt = new RenderTexture(w, h, 0);
        videoPlayer.targetTexture = _videoRt;
        videoDisplay.texture = _videoRt;
        videoDisplay.color = new Color(1f, 1f, 1f, 0f);
        videoDisplay.gameObject.SetActive(true);

        _videoClipStepEnded = false;
        void OnClipEnd(VideoPlayer vp)
        {
            _videoClipStepEnded = true;
            if (step != null && step.pauseVideoOnLastFrame)
                vp.Pause();
        }
        videoPlayer.loopPointReached += OnClipEnd;

        videoPlayer.Play();
        float wf = 0f;
        while (videoPlayer.frame < 0 && wf < 4f)
        {
            wf += Time.deltaTime;
            yield return null;
        }
        if (fadeInDuration > 0.001f)
        {
            ConfigureFadeOverlayBlack();
            if (fadeOverlay != null)
            {
                fadeOverlay.transform.SetAsLastSibling();
                fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
            }
            videoDisplay.color = new Color(1f, 1f, 1f, 0f);
            yield return CrossFadeOverlayDownAndRawImageUp(fadeInDuration);
        }
        else
        {
            SetFadeAlpha(0f);
            videoDisplay.color = Color.white;
        }

        float reportedVideoLen = 0f;
        if (clip != null) reportedVideoLen = (float)clip.length;
        else if (videoPlayer != null && videoPlayer.length > 0.001d) reportedVideoLen = (float)videoPlayer.length;
        float videoLen = Mathf.Max(0.01f, reportedVideoLen);
        if (hasBeats)
        {
            if (UsesVideoTimelineCues(step))
            {
                var sorted = BuildSortedVideoBeats(step);
                int ci = 0;
                while (!_videoClipStepEnded)
                {
                    float vt = (float)videoPlayer.time;
                    while (ci < sorted.Length && vt + 0.04f >= sorted[ci].showAtTimeInVideo)
                    {
                        ShowSubtitle(sorted[ci].text ?? string.Empty);
                        PlayVoice(sorted[ci].voice);
                        ci++;
                    }
                    yield return null;
                }
                while (ci < sorted.Length)
                {
                    var b = sorted[ci];
                    ShowSubtitle(b.text ?? string.Empty);
                    PlayVoice(b.voice);
                    float beatHold = Mathf.Max(0.1f, b.minHold);
                    if (b.voice != null) beatHold = Mathf.Max(beatHold, b.voice.length);
                    float endWall = Time.time + beatHold;
                    while (Time.time < endWall)
                        yield return null;
                    ci++;
                }
            }
            else
            {
                // 顺序字幕：每句配音必须播完，不能因「视频已到结尾」提前 break，
                // 否则会出现「前半段有声、后面几句全没声」（视频比配音短时常发）。
                foreach (var b in step.subtitleBeats)
                {
                    ShowSubtitle(b.text ?? string.Empty);
                    PlayVoice(b.voice);
                    float beatHold = Mathf.Max(0.1f, b.minHold);
                    if (b.voice != null) beatHold = Mathf.Max(beatHold, b.voice.length);
                    float endWall = Time.time + beatHold;
                    while (Time.time < endWall)
                        yield return null;
                }
                while (!_videoClipStepEnded)
                    yield return null;
            }
        }
        else
        {
            while (!_videoClipStepEnded)
                yield return null;
        }

        videoPlayer.loopPointReached -= OnClipEnd;
        if (step != null && step.pauseVideoOnLastFrame && videoPlayer.isPrepared)
            videoPlayer.Pause();

        float reported = hasBeats ? videoLen : Mathf.Max(videoLen, voiceDuration);
        onWaited?.Invoke(reported);
    }

    static bool UsesVideoTimelineCues(OpeningStep step)
    {
        if (!HasSubtitleBeats(step)) return false;
        foreach (var b in step.subtitleBeats)
        {
            if (b.showAtTimeInVideo > 0.0001f)
                return true;
        }
        return false;
    }

    static OpeningSubtitleBeat[] BuildSortedVideoBeats(OpeningStep step)
    {
        int n = step.subtitleBeats.Length;
        var order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;
        System.Array.Sort(order, (a, b) =>
        {
            float ta = step.subtitleBeats[a].showAtTimeInVideo;
            float tb = step.subtitleBeats[b].showAtTimeInVideo;
            int c = ta.CompareTo(tb);
            return c != 0 ? c : a.CompareTo(b);
        });
        var arr = new OpeningSubtitleBeat[n];
        for (int i = 0; i < n; i++)
            arr[i] = step.subtitleBeats[order[i]];
        return arr;
    }

    private void StopVideo()
    {
        if (videoPlayer == null) return;
        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.url = "";
    }

    private float PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null) return 0f;
        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        return clip.length;
    }

    private IEnumerator WaitStepTiming(OpeningStep step, float mediaDuration)
    {
        float minDuration = Mathf.Max(0.1f, step.minDuration);
        float total = Mathf.Max(minDuration, mediaDuration) + Mathf.Max(0f, step.extraDelay);
        yield return new WaitForSeconds(total);
    }

    private IEnumerator WaitForClick()
    {
        _clicked = false;
        while (!_clicked) yield return null;
        _clicked = false;
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;
        ConfigureFadeOverlayBlack();
        fadeOverlay.transform.SetAsLastSibling();
        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);
        if (duration <= 0f)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, to);
            yield break;
        }
        if (Mathf.Abs(from - to) < 0.001f)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, to);
            yield break;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(from, to, u);
            fadeOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);
    }

    private void OnDestroy()
    {
        StopVideo();
        if (_videoRt != null) _videoRt.Release();
    }
}
