using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章通关视频：播完停在最后一帧 → 视频后对白（优先纯图片 Image；否则 UI Text + 字幕底）可配音 → 黑屏渐显 → 小知识插图 +「继续」进入第三章。
/// </summary>
public class Chapter2VideoEndController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    private RenderTexture _rt;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private GameObject videoPanel;

    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("视频结束后：对白（优先纯图片）")]
    [SerializeField] private Image postVideoDialogImage;
    [Tooltip("未挂 Post Video Dialog Image 时使用（旧场景）")]
    [SerializeField] private Text postVideoSubtitle;
    [TextArea(2, 6)]
    [SerializeField] private string subtitleMessage = "（在这里写视频结束后的说明或过渡语。）";
    [SerializeField] private float subtitleFadeDuration = 0.45f;
    [SerializeField] private float subtitleMinHold = 2f;
    [SerializeField] private AudioSource subtitleVoiceSource;
    [SerializeField] private AudioClip subtitleVoiceClip;

    private Image _postVideoSubtitleBar;
    private float _postVideoSubtitleBarOpaque = 1f;

    [Header("黑场后：小知识插图")]
    [SerializeField] private GameObject knowledgePanel;
    [SerializeField] private Image knowledgeIllustration;
    [SerializeField] private Sprite knowledgeSprite;
    [SerializeField] private Button knowledgeContinueButton;

    [Header("跳转")]
    [SerializeField] private string nextSceneName = "Chapter3Bridge";

    private bool _knowledgeContinueWired;
    private bool _videoFailed;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();
        if (videoPanel == null)
        {
            var t = transform.Find("VideoPanel");
            if (t != null) videoPanel = t.gameObject;
        }
        if (subtitleVoiceSource == null) subtitleVoiceSource = GetComponent<AudioSource>();

        if (postVideoDialogImage != null)
        {
            postVideoDialogImage.gameObject.SetActive(true);
            SetGraphicAlpha(postVideoDialogImage, 0f);
        }
        else if (postVideoSubtitle != null)
        {
            postVideoSubtitle.text = subtitleMessage ?? "";
            _postVideoSubtitleBar = SubtitleStyleUtility.ApplyToSubtitle(postVideoSubtitle, null);
            postVideoSubtitle.gameObject.SetActive(true);
            SetGraphicAlpha(postVideoSubtitle, 0f);
            if (_postVideoSubtitleBar != null)
            {
                _postVideoSubtitleBarOpaque = Mathf.Clamp01(_postVideoSubtitleBar.color.a);
                SetGraphicAlpha(_postVideoSubtitleBar, 0f);
                SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(_postVideoSubtitleBar, 0f, _postVideoSubtitleBarOpaque);
            }
        }

        if (knowledgePanel != null)
            knowledgePanel.SetActive(false);

        if (knowledgeContinueButton != null)
        {
            knowledgeContinueButton.gameObject.SetActive(true);
            knowledgeContinueButton.interactable = false;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        }

        StartCoroutine(PlayVideo());
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoEnd;
        vp.Pause();
        StartCoroutine(PostVideoFlow());
    }

    private IEnumerator PostVideoFlow()
    {
        // 短视频可能在开场渐隐结束前就结束了：遮罩仍半透明会盖住字幕
        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);

        if (postVideoDialogImage != null)
        {
            postVideoDialogImage.transform.SetAsLastSibling();
            postVideoDialogImage.gameObject.SetActive(true);
            yield return FadePostVideoDialogAlpha(postVideoDialogImage, null, 0f, 1f, subtitleFadeDuration);
        }
        else if (postVideoSubtitle != null)
        {
            if (_postVideoSubtitleBar != null)
                _postVideoSubtitleBar.transform.SetSiblingIndex(postVideoSubtitle.transform.GetSiblingIndex());
            postVideoSubtitle.transform.SetAsLastSibling();
            postVideoSubtitle.gameObject.SetActive(true);
            yield return FadePostVideoDialogAlpha(postVideoSubtitle, _postVideoSubtitleBar, 0f, 1f, subtitleFadeDuration);
        }

        PlaySubtitleVoice();
        float vLen = subtitleVoiceClip != null ? subtitleVoiceClip.length : 0f;
        yield return new WaitForSeconds(Mathf.Max(subtitleMinHold, vLen));

        if (postVideoDialogImage != null)
            yield return FadePostVideoDialogAlpha(postVideoDialogImage, null, 1f, 0f, subtitleFadeDuration);
        else if (postVideoSubtitle != null)
            yield return FadePostVideoDialogAlpha(postVideoSubtitle, _postVideoSubtitleBar, 1f, 0f, subtitleFadeDuration);
        StopSubtitleVoice();

        yield return Fade(0f, 1f, fadeOutDuration);

        if (videoPanel != null)
            videoPanel.SetActive(false);
        if (postVideoDialogImage != null)
            postVideoDialogImage.gameObject.SetActive(false);
        if (postVideoSubtitle != null)
            postVideoSubtitle.gameObject.SetActive(false);

        if (knowledgePanel != null)
        {
            knowledgePanel.SetActive(true);
            if (knowledgeIllustration != null && knowledgeSprite != null)
                knowledgeIllustration.sprite = knowledgeSprite;
        }

        WireKnowledgeContinueOnce();

        if (fadeOverlay != null)
            fadeOverlay.transform.SetAsLastSibling();

        yield return Fade(1f, 0f, fadeInDuration);

        if (knowledgeContinueButton != null)
            knowledgeContinueButton.interactable = true;
    }

    private void WireKnowledgeContinueOnce()
    {
        if (_knowledgeContinueWired || knowledgeContinueButton == null) return;
        _knowledgeContinueWired = true;
        knowledgeContinueButton.onClick.AddListener(OnKnowledgeContinueClicked);
    }

    private void OnKnowledgeContinueClicked()
    {
        if (knowledgeContinueButton != null)
            knowledgeContinueButton.interactable = false;
        StartCoroutine(FadeOutThenLoad());
    }

    private IEnumerator FadeOutThenLoad()
    {
        yield return Fade(0f, 1f, fadeOutDuration);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator PlayVideo()
    {
        bool hasVideo = false;
        if (videoPlayer != null && videoDisplay != null)
        {
            _videoFailed = false;
            VideoPlayer.ErrorEventHandler onErr = (v, msg) =>
            {
                _videoFailed = true;
                VideoPlaybackUtility.LogVideoError(v, msg);
            };
            videoPlayer.errorReceived += onErr;

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.loopPointReached += OnVideoEnd;
            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else if (VideoPlaybackUtility.HasStreamingMediaSource("video/chapter2_end.mp4"))
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = VideoPlaybackUtility.ResolveStreamingMediaUrl("video/chapter2_end.mp4");
            }
            videoPlayer.Prepare();
            float t = 0;
            while (!videoPlayer.isPrepared && !_videoFailed && t < 15f) { t += Time.deltaTime; yield return null; }
            hasVideo = videoPlayer.isPrepared && !_videoFailed;
            if (hasVideo)
            {
                VideoPlaybackUtility.ApplyStandardCompat(videoPlayer);
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                int w = (int)videoPlayer.width, h = (int)videoPlayer.height;
                if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
                _rt = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
                videoPlayer.targetTexture = _rt;
                videoDisplay.texture = _rt;
                videoDisplay.gameObject.SetActive(true);
                videoPlayer.Play();
                yield return VideoPlaybackUtility.CoWaitFirstFrameOrTimeout(videoPlayer, () => _videoFailed, 8f);
                if (_videoFailed || videoPlayer.frame < 0)
                {
                    videoPlayer.loopPointReached -= OnVideoEnd;
                    videoPlayer.Stop();
                    if (_rt != null) { _rt.Release(); _rt = null; }
                    videoDisplay.texture = null;
                    hasVideo = false;
                }
            }

            if (!hasVideo)
            {
                videoPlayer.loopPointReached -= OnVideoEnd;
                videoPlayer.errorReceived -= onErr;
                StartCoroutine(PostVideoFlow());
            }
        }
        else
        {
            StartCoroutine(PostVideoFlow());
        }

        yield return Fade(1f, 0f, fadeInDuration);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
        if (_rt != null) _rt.Release();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null || duration <= 0) yield break;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);
    }

    private static void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    IEnumerator FadePostVideoDialogAlpha(Graphic main, Image subtitleBar, float from, float to, float duration)
    {
        if (main == null) yield break;
        Image solidUnder = null;
        if (subtitleBar != null && main.transform.parent != null)
        {
            var su = main.transform.parent.Find(SubtitleStyleUtility.SolidBackdropChildName);
            if (su != null) solidUnder = su.GetComponent<Image>();
        }

        if (duration <= 0.001f)
        {
            SetGraphicAlpha(main, to);
            if (subtitleBar != null)
            {
                SetGraphicAlpha(subtitleBar, to);
                SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(subtitleBar, to, _postVideoSubtitleBarOpaque);
            }
            SetSolidUnderlayFade(solidUnder, to);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Lerp(from, to, Mathf.Clamp01(e / duration));
            SetGraphicAlpha(main, u);
            if (subtitleBar != null)
            {
                SetGraphicAlpha(subtitleBar, u);
                SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(subtitleBar, u, _postVideoSubtitleBarOpaque);
            }
            SetSolidUnderlayFade(solidUnder, u);
            yield return null;
        }
        SetGraphicAlpha(main, to);
        if (subtitleBar != null)
        {
            SetGraphicAlpha(subtitleBar, to);
            SubtitleStyleUtility.SyncSolidUnderlayAlphaFromGradient(subtitleBar, to, _postVideoSubtitleBarOpaque);
        }
        SetSolidUnderlayFade(solidUnder, to);
    }

    static void SetSolidUnderlayFade(Image solid, float u)
    {
        if (solid == null) return;
        var c = solid.color;
        c.a = SubtitleStyleUtility.SolidUnderlayTargetAlpha * Mathf.Clamp01(u);
        solid.color = c;
    }

    private void PlaySubtitleVoice()
    {
        if (subtitleVoiceSource == null) return;
        subtitleVoiceSource.Stop();
        subtitleVoiceSource.clip = subtitleVoiceClip;
        if (subtitleVoiceClip != null)
            subtitleVoiceSource.Play();
    }

    private void StopSubtitleVoice()
    {
        if (subtitleVoiceSource != null)
            subtitleVoiceSource.Stop();
    }
}
