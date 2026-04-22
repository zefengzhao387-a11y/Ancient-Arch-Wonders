using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 结尾演出：黑场渐显 → 首图+字幕（可配音）→ 点击继续 → 首图与视频区交叉渐隐/渐显 → 视频段间渐隐+渐显 → 遮罩渐隐/渐显接插图与结尾 → 回主菜单。
/// 视频可拖 VideoClip，或 StreamingAssets：ending_part1.mp4 / ending_part2.mp4。
/// </summary>
public class GameEndingController : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.85f;
    [SerializeField] private float crossSubtitleFade = 0.35f;

    [Header("全局")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private Button clickToAdvance;
    [SerializeField] private string nextSceneName = "GameMenu";

    [Header("1 首图 + 字幕（点击任意处进入视频1）")]
    [SerializeField] private GameObject openingPhase;
    [SerializeField] private Image openingImage;
    [SerializeField] private Sprite openingSprite;
    [SerializeField] private Text openingSubtitle;
    [Tooltip("首图底部字幕浅灰条；留空则从 openingSubtitle 向上查找 Image")]
    [SerializeField] private Image openingSubtitleBar;
    [TextArea(2, 5)]
    [SerializeField] private string openingSubtitleText = "（片头说明文字）";
    [SerializeField] private AudioClip openingSubtitleVoice;
    [SerializeField] private float openingSubtitleMinHold = 2f;

    [Header("2 第一段视频 + 下方两段字幕")]
    [SerializeField] private GameObject videoPhaseRoot;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private VideoClip videoPart1Clip;
    [SerializeField] private Text videoSubtitleText;
    [Tooltip("视频阶段底部字幕浅灰条；留空则从 videoSubtitleText 向上查找 Image")]
    [SerializeField] private Image videoSubtitleBar;
    [TextArea(1, 4)]
    [SerializeField] private string part1SubtitleLine1 = "（第一段视频字幕一）";
    [SerializeField] private AudioClip part1SubtitleVoice1;
    [SerializeField] private float part1SubtitleMinHold1 = 2f;
    [TextArea(1, 4)]
    [SerializeField] private string part1SubtitleLine2 = "（第一段视频字幕二）";
    [SerializeField] private AudioClip part1SubtitleVoice2;
    [SerializeField] private float part1SubtitleMinHold2 = 2f;

    [Header("3 第二段视频 + 下方一段字幕")]
    [SerializeField] private VideoClip videoPart2Clip;
    [TextArea(1, 4)]
    [SerializeField] private string part2SubtitleLine = "（第二段视频字幕）";
    [SerializeField] private AudioClip part2SubtitleVoice;
    [SerializeField] private float part2SubtitleMinHold = 2f;

    [Header("4 中间插图")]
    [SerializeField] private GameObject middlePhase;
    [SerializeField] private Image middleImage;
    [SerializeField] private Sprite middleSprite;

    [Header("5 最后插图 + 可选字幕")]
    [SerializeField] private GameObject finalPhase;
    [SerializeField] private Image finalImage;
    [SerializeField] private Sprite finalSprite;
    [SerializeField] private Text finalSubtitle;
    [TextArea(2, 6)]
    [SerializeField] private string finalSubtitleText = "";
    [SerializeField] private AudioClip finalSubtitleVoice;
    [SerializeField] private float finalSubtitleMinHold = 2f;

    private RenderTexture _rt;
    private bool _clickFlag;
    private bool _videoFailed;
    private float _subtitleBarOpaqueAlpha = 0.92f;
    private float _openingBarOpaqueAlpha = 0.92f;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>(true);
        openingSubtitleBar = SubtitleStyleUtility.ApplyToSubtitle(openingSubtitle, openingSubtitleBar);
        videoSubtitleBar = SubtitleStyleUtility.ApplyToSubtitle(videoSubtitleText, videoSubtitleBar);
        SubtitleStyleUtility.ApplyToSubtitle(finalSubtitle, null);
        ApplyEndingSubtitleTextColors();
        CacheSubtitleBarOpaque();
        CacheOpeningBarOpaque();
        if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;

        if (clickToAdvance != null)
        {
            clickToAdvance.onClick.RemoveAllListeners();
            clickToAdvance.onClick.AddListener(() => _clickFlag = true);
            clickToAdvance.gameObject.SetActive(false);
        }

        HideAllPhases();
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        StartCoroutine(RunSequence());
    }

    private void HideAllPhases()
    {
        if (openingPhase != null) openingPhase.SetActive(false);
        if (videoPhaseRoot != null) videoPhaseRoot.SetActive(false);
        if (middlePhase != null) middlePhase.SetActive(false);
        if (finalPhase != null) finalPhase.SetActive(false);
    }

    private IEnumerator RunSequence()
    {
        // 黑场下先布置首图与字幕，再渐显（图与字同时出现）
        if (openingPhase != null)
        {
            if (openingImage != null)
            {
                if (openingSprite != null) openingImage.sprite = openingSprite;
                openingImage.enabled = true;
            }
            if (openingSubtitle != null)
            {
                openingSubtitle.text = openingSubtitleText ?? "";
                SetTextAlpha(openingSubtitle, 1f);
            }
            openingPhase.SetActive(true);
        }

        yield return new WaitForSeconds(0.15f);
        yield return Fade(1f, 0f, fadeInDuration);

        if (openingPhase != null)
        {
            PlayVoice(openingSubtitleVoice);
            yield return new WaitForSeconds(SubtitleWait(openingSubtitleMinHold, openingSubtitleVoice));
            StopVoice();
        }

        SetClickable(true);
        _clickFlag = false;
        yield return new WaitUntil(() => _clickFlag);
        SetClickable(false);

        bool canPlayPart1 = videoPhaseRoot != null && videoPlayer != null && videoDisplay != null &&
                            (videoPart1Clip != null || HasStreamingClip("ending_part1.mp4"));
        bool canPlayPart2 = videoPhaseRoot != null && videoPlayer != null && videoDisplay != null &&
                            (videoPart2Clip != null || HasStreamingClip("ending_part2.mp4"));
        bool willPlayAnyVideo = canPlayPart1 || canPlayPart2;
        bool crossFadeOpeningIntoVideo = openingPhase != null && willPlayAnyVideo;

        if (openingPhase != null && !willPlayAnyVideo)
        {
            yield return FadeOpeningPhaseOut(fadeOutDuration);
            openingPhase.SetActive(false);
            ResetOpeningPhaseAlphas();
        }

        // —— 2 视频1 + 两段字幕 ——
        if (canPlayPart1)
        {
            videoPhaseRoot.SetActive(true);
            if (crossFadeOpeningIntoVideo) StackOpeningAboveVideoForCrossfade();
            yield return PlayVideoSegment(videoPart1Clip, "ending_part1.mp4", ShowTwoVideoSubtitles,
                holdLastFrameUntilSubtitlesEnd: true, fadeDisplayInFromZero: true, crossFadeFromOpening: crossFadeOpeningIntoVideo);

            CloseOpeningPhaseIfStillVisible();

            if (canPlayPart2)
            {
                yield return FadeVideoPhaseOut(fadeOutDuration);
                TeardownVideo();
                yield return PlayVideoSegment(videoPart2Clip, "ending_part2.mp4", ShowOneVideoSubtitle,
                    holdLastFrameUntilSubtitlesEnd: false, fadeDisplayInFromZero: true, crossFadeFromOpening: false);
                CloseOpeningPhaseIfStillVisible();
                yield return Fade(0f, 1f, fadeOutDuration);
            }
            else
                yield return Fade(0f, 1f, fadeOutDuration);

            TeardownVideo();
        }
        else if (canPlayPart2)
        {
            videoPhaseRoot.SetActive(true);
            if (crossFadeOpeningIntoVideo) StackOpeningAboveVideoForCrossfade();
            yield return PlayVideoSegment(videoPart2Clip, "ending_part2.mp4", ShowOneVideoSubtitle,
                    holdLastFrameUntilSubtitlesEnd: false, fadeDisplayInFromZero: true, crossFadeFromOpening: crossFadeOpeningIntoVideo);
            CloseOpeningPhaseIfStillVisible();
            yield return Fade(0f, 1f, fadeOutDuration);
            TeardownVideo();
        }

        if (videoPhaseRoot != null) videoPhaseRoot.SetActive(false);

        // 视频在该机解码失败时会提前 yield break，不会走交叉渐隐，首图阶段仍激活会挡死后面插图/黑场。
        CloseOpeningPhaseIfStillVisible();

        // —— 4 中间插图 ——
        if (middlePhase != null)
        {
            if (middlePhase.GetComponent<GameEndingMiddleHint>() == null)
                middlePhase.AddComponent<GameEndingMiddleHint>();
            middlePhase.SetActive(true);
            if (middleImage != null && middleSprite != null) middleImage.sprite = middleSprite;
            yield return Fade(1f, 0f, fadeInDuration);
            SetClickable(true);
            _clickFlag = false;
            yield return new WaitUntil(() => _clickFlag);
            SetClickable(false);
            yield return Fade(0f, 1f, fadeOutDuration);
            middlePhase.SetActive(false);
        }

        // —— 5 最后插图 + 字幕 ——
        if (finalPhase != null)
        {
            finalPhase.SetActive(true);
            if (finalImage != null && finalSprite != null) finalImage.sprite = finalSprite;
            if (finalSubtitle != null)
            {
                finalSubtitle.text = finalSubtitleText ?? "";
                SetTextAlpha(finalSubtitle, string.IsNullOrEmpty(finalSubtitleText) ? 0f : 1f);
            }
            yield return Fade(1f, 0f, fadeInDuration);
            PlayVoice(finalSubtitleVoice);
            yield return new WaitForSeconds(SubtitleWait(finalSubtitleMinHold, finalSubtitleVoice));
            StopVoice();

            SetClickable(true);
            _clickFlag = false;
            yield return new WaitUntil(() => _clickFlag);
            SetClickable(false);
        }

        yield return Fade(0f, 1f, fadeOutDuration);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator ShowTwoVideoSubtitles()
    {
        if (videoSubtitleText == null) yield break;
        SetTextAlpha(videoSubtitleText, 0f);
        videoSubtitleText.text = part1SubtitleLine1 ?? "";
        yield return FadeText(videoSubtitleText, 0f, 1f, crossSubtitleFade);
        PlayVoice(part1SubtitleVoice1);
        yield return new WaitForSeconds(SubtitleWait(part1SubtitleMinHold1, part1SubtitleVoice1));
        StopVoice();
        yield return FadeText(videoSubtitleText, 1f, 0f, crossSubtitleFade);
        videoSubtitleText.text = part1SubtitleLine2 ?? "";
        yield return FadeText(videoSubtitleText, 0f, 1f, crossSubtitleFade);
        PlayVoice(part1SubtitleVoice2);
        yield return new WaitForSeconds(SubtitleWait(part1SubtitleMinHold2, part1SubtitleVoice2));
        StopVoice();
    }

    private IEnumerator ShowOneVideoSubtitle()
    {
        if (videoSubtitleText == null) yield break;
        SetTextAlpha(videoSubtitleText, 0f);
        videoSubtitleText.text = part2SubtitleLine ?? "";
        yield return FadeText(videoSubtitleText, 0f, 1f, crossSubtitleFade);
        PlayVoice(part2SubtitleVoice);
        yield return new WaitForSeconds(SubtitleWait(part2SubtitleMinHold, part2SubtitleVoice));
        StopVoice();
    }

    /// <param name="holdLastFrameUntilSubtitlesEnd">为 true 时，视频播完后暂停在最后一帧，直到字幕协程跑完再进入下一段（用于第一段视频的两段字幕）。</param>
    /// <param name="fadeDisplayInFromZero">为 true 时 RawImage+字幕条+字幕字从 0 渐显到正常（首图后进视频、两段视频衔接、仅第二段进视频）。</param>
    /// <param name="crossFadeFromOpening">为 true 时与首图阶段并行：首图渐隐 + 视频区渐显，再关闭首图。</param>
    private IEnumerator PlayVideoSegment(VideoClip clip, string streamingName, System.Func<IEnumerator> subtitleFactory, bool holdLastFrameUntilSubtitlesEnd, bool fadeDisplayInFromZero, bool crossFadeFromOpening)
    {
        if (videoPlayer == null || videoDisplay == null) yield break;

        if (fadeDisplayInFromZero)
        {
            SetRawImageAlpha(videoDisplay, 0f);
            ApplyVideoBarAlpha(0f);
            SetTextAlpha(videoSubtitleText, 0f);
        }
        else
        {
            SetRawImageAlpha(videoDisplay, 1f);
            CacheSubtitleBarOpaque();
            ApplyVideoBarAlpha(_subtitleBarOpaqueAlpha);
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        _videoFailed = false;
        VideoPlayer.ErrorEventHandler onErr = (v, msg) =>
        {
            _videoFailed = true;
            VideoPlaybackUtility.LogVideoError(v, msg);
        };
        videoPlayer.errorReceived += onErr;

        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            videoPlayer.url = "";
        }
        else
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, streamingName);
            if (!System.IO.File.Exists(path))
            {
                videoPlayer.errorReceived -= onErr;
                yield break;
            }
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = VideoPlaybackUtility.FileUrlFromPath(path);
            videoPlayer.clip = null;
        }

        VideoPlaybackUtility.ApplyStandardCompat(videoPlayer);
        videoPlayer.Prepare();
        float prep = 0f;
        while (!videoPlayer.isPrepared && !_videoFailed && prep < 15f)
        {
            prep += Time.deltaTime;
            yield return null;
        }
        if (!videoPlayer.isPrepared || _videoFailed)
        {
            videoPlayer.errorReceived -= onErr;
            yield break;
        }

        int w = (int)videoPlayer.width, h = (int)videoPlayer.height;
        if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
        if (_rt != null) _rt.Release();
        _rt = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
        videoPlayer.targetTexture = _rt;
        videoDisplay.texture = _rt;
        videoDisplay.gameObject.SetActive(true);

        if (fadeDisplayInFromZero)
        {
            if (crossFadeFromOpening && openingPhase != null && openingPhase.activeSelf)
            {
                yield return RunParallel(FadeOpeningPhaseOut(fadeInDuration), FadeVideoPhaseIn(fadeInDuration));
                openingPhase.SetActive(false);
                ResetOpeningPhaseAlphas();
            }
            else
                yield return FadeVideoPhaseIn(fadeInDuration);
        }

        bool ended = false;
        void OnEnd(VideoPlayer vp)
        {
            ended = true;
            vp.Pause();
        }
        videoPlayer.loopPointReached += OnEnd;

        bool subtitlesDone = subtitleFactory == null;
        IEnumerator SubtitlesWrapper()
        {
            yield return subtitleFactory();
            subtitlesDone = true;
        }

        Coroutine subCo = subtitleFactory != null ? StartCoroutine(SubtitlesWrapper()) : null;
        videoPlayer.Play();
        yield return VideoPlaybackUtility.CoWaitFirstFrameOrTimeout(videoPlayer, () => _videoFailed, 8f);
        if (_videoFailed || videoPlayer.frame < 0)
        {
            videoPlayer.loopPointReached -= OnEnd;
            videoPlayer.errorReceived -= onErr;
            if (subCo != null) StopCoroutine(subCo);
            TeardownVideo();
            yield break;
        }

        while (!ended) yield return null;

        videoPlayer.loopPointReached -= OnEnd;
        videoPlayer.errorReceived -= onErr;

        if (holdLastFrameUntilSubtitlesEnd)
        {
            while (!subtitlesDone) yield return null;
        }
        else if (subCo != null)
            StopCoroutine(subCo);
    }

    private void TeardownVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.url = "";
        }
        if (videoDisplay != null) videoDisplay.texture = null;
        if (_rt != null) { _rt.Release(); _rt = null; }
    }

    private static bool HasStreamingClip(string fileName)
    {
        return System.IO.File.Exists(System.IO.Path.Combine(Application.streamingAssetsPath, fileName));
    }

    private static float SubtitleWait(float minHold, AudioClip clip)
    {
        float v = clip != null ? clip.length : 0f;
        return Mathf.Max(minHold, v);
    }

    private void SetClickable(bool on)
    {
        if (clickToAdvance != null)
        {
            clickToAdvance.gameObject.SetActive(on);
            clickToAdvance.interactable = on;
            if (on) clickToAdvance.transform.SetAsLastSibling();
            if (on && fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
        }
    }

    private void PlayVoice(AudioClip c)
    {
        if (voiceSource == null) return;
        voiceSource.Stop();
        voiceSource.clip = c;
        if (c != null) voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource != null) voiceSource.Stop();
    }

    private static void SetTextAlpha(Text t, float a)
    {
        if (t == null) return;
        var c = t.color;
        c.a = a;
        t.color = c;
    }

    private IEnumerator FadeText(Text t, float from, float to, float d)
    {
        if (t == null || d <= 0.001f)
        {
            SetTextAlpha(t, to);
            yield break;
        }
        float e = 0f;
        while (e < d)
        {
            e += Time.deltaTime;
            SetTextAlpha(t, Mathf.Lerp(from, to, e / d));
            yield return null;
        }
        SetTextAlpha(t, to);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null || duration <= 0f) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var c = fadeOverlay.color;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }
    }

    private void CacheSubtitleBarOpaque()
    {
        if (videoSubtitleBar != null)
            _subtitleBarOpaqueAlpha = Mathf.Clamp01(videoSubtitleBar.color.a);
    }

    private void CacheOpeningBarOpaque()
    {
        if (openingSubtitleBar != null)
            _openingBarOpaqueAlpha = Mathf.Clamp01(openingSubtitleBar.color.a);
    }

    void ApplyOpeningBarAlpha(float a)
    {
        SetGraphicAlpha(openingSubtitleBar, a);
        SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(openingSubtitleBar, a, _openingBarOpaqueAlpha);
    }

    void ApplyVideoBarAlpha(float a)
    {
        SetGraphicAlpha(videoSubtitleBar, a);
        SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(videoSubtitleBar, a, _subtitleBarOpaqueAlpha);
    }

    private void StackOpeningAboveVideoForCrossfade()
    {
        if (openingPhase != null) openingPhase.transform.SetAsLastSibling();
        if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
    }

    private IEnumerator FadeOpeningPhaseOut(float duration)
    {
        var rootImg = openingPhase != null ? openingPhase.GetComponent<Image>() : null;
        float root0 = rootImg != null ? rootImg.color.a : 1f;
        float img0 = openingImage != null ? openingImage.color.a : 1f;
        float bar0 = openingSubtitleBar != null ? openingSubtitleBar.color.a : 0f;
        float txt0 = openingSubtitle != null ? openingSubtitle.color.a : 1f;
        if (duration <= 0.001f)
        {
            SetGraphicAlpha(rootImg, 0f);
            SetGraphicAlpha(openingImage, 0f);
            ApplyOpeningBarAlpha(0f);
            SetTextAlpha(openingSubtitle, 0f);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Clamp01(e / duration);
            SetGraphicAlpha(rootImg, Mathf.Lerp(root0, 0f, u));
            SetGraphicAlpha(openingImage, Mathf.Lerp(img0, 0f, u));
            ApplyOpeningBarAlpha(Mathf.Lerp(bar0, 0f, u));
            SetTextAlpha(openingSubtitle, Mathf.Lerp(txt0, 0f, u));
            yield return null;
        }
        SetGraphicAlpha(rootImg, 0f);
        SetGraphicAlpha(openingImage, 0f);
        ApplyOpeningBarAlpha(0f);
        SetTextAlpha(openingSubtitle, 0f);
    }

    /// <summary>结局首图与视频交叉渐隐仅在视频成功走到交叉阶段时执行；Prepare/首帧失败时必须关掉，否则玩家一直看到「结尾第一张图」。</summary>
    void CloseOpeningPhaseIfStillVisible()
    {
        if (openingPhase == null || !openingPhase.activeSelf) return;
        openingPhase.SetActive(false);
        ResetOpeningPhaseAlphas();
    }

    private void ResetOpeningPhaseAlphas()
    {
        if (openingPhase == null) return;
        var rootImg = openingPhase.GetComponent<Image>();
        if (rootImg != null)
        {
            var c = rootImg.color;
            c.a = 1f;
            rootImg.color = c;
        }
        if (openingImage != null)
        {
            var c = openingImage.color;
            c.a = 1f;
            openingImage.color = c;
        }
        ApplyOpeningBarAlpha(_openingBarOpaqueAlpha);
        SetTextAlpha(openingSubtitle, 1f);
    }

    private IEnumerator RunParallel(IEnumerator a, IEnumerator b)
    {
        if (a == null)
        {
            if (b != null) yield return b;
            yield break;
        }
        if (b == null)
        {
            yield return a;
            yield break;
        }
        bool aAlive = true, bAlive = true;
        while (aAlive || bAlive)
        {
            if (aAlive && !a.MoveNext()) aAlive = false;
            if (bAlive && !b.MoveNext()) bAlive = false;
            if (aAlive || bAlive) yield return null;
        }
    }

    private static void SetRawImageAlpha(RawImage img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private static void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    /// <summary>视频与底部字幕条、字幕字同步渐显（字幕字保持 0，交给字幕协程）。</summary>
    private IEnumerator FadeVideoPhaseIn(float duration)
    {
        if (duration <= 0.001f)
        {
            SetRawImageAlpha(videoDisplay, 1f);
            ApplyVideoBarAlpha(_subtitleBarOpaqueAlpha);
            SetTextAlpha(videoSubtitleText, 0f);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Clamp01(e / duration);
            SetRawImageAlpha(videoDisplay, u);
            ApplyVideoBarAlpha(Mathf.Lerp(0f, _subtitleBarOpaqueAlpha, u));
            SetTextAlpha(videoSubtitleText, 0f);
            yield return null;
        }
        SetRawImageAlpha(videoDisplay, 1f);
        ApplyVideoBarAlpha(_subtitleBarOpaqueAlpha);
        SetTextAlpha(videoSubtitleText, 0f);
    }

    /// <summary>视频与字幕条、当前字幕字同步渐隐。</summary>
    private IEnumerator FadeVideoPhaseOut(float duration)
    {
        float raw0 = videoDisplay != null ? videoDisplay.color.a : 1f;
        float text0 = videoSubtitleText != null ? videoSubtitleText.color.a : 0f;
        if (duration <= 0.001f)
        {
            SetRawImageAlpha(videoDisplay, 0f);
            ApplyVideoBarAlpha(0f);
            SetTextAlpha(videoSubtitleText, 0f);
            yield break;
        }
        float bar0 = videoSubtitleBar != null ? videoSubtitleBar.color.a : 0f;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Clamp01(e / duration);
            SetRawImageAlpha(videoDisplay, Mathf.Lerp(raw0, 0f, u));
            ApplyVideoBarAlpha(Mathf.Lerp(bar0, 0f, u));
            SetTextAlpha(videoSubtitleText, Mathf.Lerp(text0, 0f, u));
            yield return null;
        }
        SetRawImageAlpha(videoDisplay, 0f);
        ApplyVideoBarAlpha(0f);
        SetTextAlpha(videoSubtitleText, 0f);
    }

    private void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }

    /// <summary>与前面章节对话框/字幕一致：深色渐变条上浅色字。</summary>
    void ApplyEndingSubtitleTextColors()
    {
        var c = new Color(0.98f, 0.99f, 0.97f, 1f);
        if (openingSubtitle != null) openingSubtitle.color = c;
        if (videoSubtitleText != null) videoSubtitleText.color = c;
        if (finalSubtitle != null) finalSubtitle.color = c;
    }
}
