using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// 第一章小游戏结束后：黑场遮罩渐隐显露出视频（RawImage 保持正常色）→ 停在最后一帧 → 两段对白（纯图片 Image 优先，否则旧版 Text）→ 遮罩渐隐进下一场景。
/// </summary>
public class Chapter1PostMiniGameController : MonoBehaviour
{
    [Header("视频")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RawImage videoDisplay;

    [Header("对话（全自动：优先纯图片对白图 ×2）")]
    [SerializeField] private Image dialogImage1;
    [SerializeField] private Image dialogImage2;
    [Tooltip("未挂 Dialog Image 时使用（旧场景）")]
    [SerializeField] private Text dialogText1;
    [Tooltip("未挂 Dialog Image 时使用（旧场景）")]
    [SerializeField] private Text dialogText2;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioClip dialogVoice1;
    [SerializeField] private AudioClip dialogVoice2;
    [Tooltip("第一段：渐显后至少停留(秒)，会与配音时长取较大值")]
    [SerializeField] private float minHoldDialog1 = 2f;
    [Tooltip("第二段：渐显后至少停留(秒)，会与配音时长取较大值")]
    [SerializeField] private float minHoldDialog2 = 2f;
    [SerializeField] private float dialogFadeDuration = 0.35f;
    [SerializeField] private float gapAfterVideo = 0.35f;
    [SerializeField] private float gapBetweenDialogs = 0.25f;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float sceneFadeInDuration = 0.6f;
    [SerializeField] private float sceneFadeOutDuration = 0.8f;

    [Header("下一场景")]
    [SerializeField] private string nextSceneName = "Chapter2Intro";

    private RenderTexture _renderTexture;
    private bool _videoFailed;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();

        // 避免场景首帧：Video 在遮罩下层 + RawImage 无纹理发白；遮罩必须盖住全屏直到淡入结束
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
        if (videoDisplay != null)
            videoDisplay.gameObject.SetActive(false);

        if (dialogImage1 != null)
        {
            SetGraphicAlpha(dialogImage1, 0f);
            dialogImage1.gameObject.SetActive(false);
        }
        if (dialogImage2 != null)
        {
            SetGraphicAlpha(dialogImage2, 0f);
            dialogImage2.gameObject.SetActive(false);
        }
        if (dialogText1 != null)
        {
            SetGraphicAlpha(dialogText1, 0f);
            dialogText1.gameObject.SetActive(false);
        }
        if (dialogText2 != null)
        {
            SetGraphicAlpha(dialogText2, 0f);
            dialogText2.gameObject.SetActive(false);
        }

        if (dialogText1 != null && dialogImage1 == null)
            SubtitleStyleUtility.ApplyToSubtitle(dialogText1, null);
        if (dialogText2 != null && dialogImage2 == null)
            SubtitleStyleUtility.ApplyToSubtitle(dialogText2, null);

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // 再等一帧，确保 Canvas 已提交黑遮罩后再做 Prepare（避免首帧闪底图/白 RawImage）
        yield return null;

        if (videoPlayer != null && videoDisplay != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            bool hasSource = false;
            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
                hasSource = true;
            }
            else
            {
                const string streamingName = "chapter1_outro.mp4";
                if (VideoPlaybackUtility.HasStreamingMediaSource(streamingName))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = VideoPlaybackUtility.ResolveStreamingMediaUrl(streamingName);
                    hasSource = true;
                }
            }

            if (!hasSource)
            {
                videoDisplay.gameObject.SetActive(false);
                if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
                yield return FadeOverlay(1f, 0f, sceneFadeInDuration);
            }
            else
            {
                _videoFailed = false;
                VideoPlayer.ErrorEventHandler onErr = (v, msg) =>
                {
                    _videoFailed = true;
                    VideoPlaybackUtility.LogVideoError(v, msg);
                };
                videoPlayer.errorReceived += onErr;

                videoPlayer.Prepare();
                float timeout = 15f;
                float t = 0f;
                while (!videoPlayer.isPrepared && !_videoFailed && t < timeout)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                videoPlayer.errorReceived -= onErr;

                if (videoPlayer.isPrepared && !_videoFailed)
                {
                    VideoPlaybackUtility.ApplyStandardCompat(videoPlayer);
                    int w = (int)videoPlayer.width;
                    int h = (int)videoPlayer.height;
                    if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
                    if (_renderTexture != null) _renderTexture.Release();
                    _renderTexture = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
                    videoPlayer.targetTexture = _renderTexture;
                    videoDisplay.texture = _renderTexture;
                    videoDisplay.color = Color.white;
                    videoDisplay.gameObject.SetActive(true);
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

                    var videoRoot = videoDisplay.transform.parent;
                    if (videoRoot != null) videoRoot.SetAsLastSibling();
                    // 黑屏渐显：全屏黑遮罩盖在视频上再渐隐，避免 RawImage 黑→白 tint 乘法产生发白过渡
                    if (fadeOverlay != null)
                    {
                        fadeOverlay.transform.SetAsLastSibling();
                        SetOverlayAlpha(1f);
                    }

                    bool finished = false;
                    void OnEnd(VideoPlayer vp)
                    {
                        finished = true;
                        vp.Pause();
                    }
                    videoPlayer.errorReceived += onErr;
                    videoPlayer.loopPointReached += OnEnd;
                    videoPlayer.Play();
                    yield return VideoPlaybackUtility.CoWaitFirstFrameOrTimeout(videoPlayer, () => _videoFailed, 8f);
                    if (_videoFailed || videoPlayer.frame < 0)
                    {
                        videoPlayer.loopPointReached -= OnEnd;
                        videoPlayer.errorReceived -= onErr;
                        videoPlayer.Stop();
                        if (_renderTexture != null)
                        {
                            _renderTexture.Release();
                            _renderTexture = null;
                        }
                        videoDisplay.texture = null;
                        videoDisplay.gameObject.SetActive(false);
                        if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
                        yield return FadeOverlay(1f, 0f, sceneFadeInDuration);
                    }
                    else
                    {
                        yield return FadeOverlay(1f, 0f, sceneFadeInDuration);
                        if (fadeOverlay != null)
                        {
                            fadeOverlay.transform.SetAsFirstSibling();
                            SetOverlayAlpha(0f);
                        }
                        while (!finished) yield return null;
                        videoPlayer.loopPointReached -= OnEnd;
                        videoPlayer.errorReceived -= onErr;
                        // 播片时 VideoRoot 在最上层；不降下来则两段对白永远被全屏视频板挡住
                        if (videoRoot != null && fadeOverlay != null)
                            videoRoot.SetSiblingIndex(fadeOverlay.transform.GetSiblingIndex() + 1);
                        else if (videoRoot != null)
                            videoRoot.SetAsFirstSibling();
                    }
                }
                else
                {
                    videoDisplay.gameObject.SetActive(false);
                    if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
                    yield return FadeOverlay(1f, 0f, sceneFadeInDuration);
                }
            }
        }
        else
        {
            if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
            yield return FadeOverlay(1f, 0f, sceneFadeInDuration);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, gapAfterVideo));

        yield return AutoDialogSlot(dialogImage1, dialogText1, dialogVoice1, minHoldDialog1);
        yield return new WaitForSeconds(Mathf.Max(0f, gapBetweenDialogs));
        yield return AutoDialogSlot(dialogImage2, dialogText2, dialogVoice2, minHoldDialog2);

        StopVoice();
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            SetOverlayAlpha(0f);
        }
        yield return FadeOverlay(0f, 1f, sceneFadeOutDuration);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator AutoDialogSlot(Image img, Text txt, AudioClip voice, float minHold)
    {
        if (img != null)
        {
            img.gameObject.SetActive(true);
            SetGraphicAlpha(img, 0f);
            yield return AutoDialog(img, null, voice, minHold);
            yield break;
        }
        if (txt != null)
        {
            txt.gameObject.SetActive(true);
            SetGraphicAlpha(txt, 0f);
            yield return AutoDialog(txt, FindSubtitleBackdrop(txt), voice, minHold);
            yield break;
        }
    }

    private IEnumerator AutoDialog(Graphic g, Image subtitleBar, AudioClip voice, float minHold)
    {
        if (g == null) yield break;
        yield return FadeGraphicsAlpha(g, subtitleBar, 0f, 1f, dialogFadeDuration);
        PlayVoice(voice);
        float vLen = voice != null ? voice.length : 0f;
        float wait = Mathf.Max(minHold, vLen);
        if (wait > 0f) yield return new WaitForSeconds(wait);
        yield return FadeGraphicsAlpha(g, subtitleBar, 1f, 0f, dialogFadeDuration);
    }

    static Image FindSubtitleBackdrop(Graphic g)
    {
        if (g == null) return null;
        if (g.transform.parent != null)
        {
            var tr = g.transform.parent.Find(SubtitleStyleUtility.BackdropChildName);
            if (tr != null) return tr.GetComponent<Image>();
        }
        var canvas = g.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var root = canvas.transform.Find(SubtitleStyleUtility.BackdropChildName);
        return root != null ? root.GetComponent<Image>() : null;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        if (clip != null) voiceAudioSource.Play();
    }

    private void StopVoice()
    {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
    }

    private static void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    private IEnumerator FadeGraphicsAlpha(Graphic g, Image bar, float from, float to, float duration)
    {
        Image solidUnder = null;
        if (g != null && g.transform.parent != null)
        {
            var su = g.transform.parent.Find(SubtitleStyleUtility.SolidBackdropChildName);
            if (su != null) solidUnder = su.GetComponent<Image>();
        }

        if (duration <= 0.001f)
        {
            SetGraphicAlpha(g, to);
            if (bar != null) SetGraphicAlpha(bar, to);
            SetSolidUnderlayFade(solidUnder, to);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Lerp(from, to, Mathf.Clamp01(e / duration));
            SetGraphicAlpha(g, u);
            if (bar != null) SetGraphicAlpha(bar, u);
            SetSolidUnderlayFade(solidUnder, u);
            yield return null;
        }
        SetGraphicAlpha(g, to);
        if (bar != null) SetGraphicAlpha(bar, to);
        SetSolidUnderlayFade(solidUnder, to);
    }

    static void SetSolidUnderlayFade(Image solid, float u)
    {
        if (solid == null) return;
        var c = solid.color;
        c.a = SubtitleStyleUtility.SolidUnderlayTargetAlpha * Mathf.Clamp01(u);
        solid.color = c;
    }

    private void SetOverlayAlpha(float a)
    {
        if (fadeOverlay == null) return;
        fadeOverlay.color = new Color(0f, 0f, 0f, a);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null || duration <= 0f) yield break;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float a = Mathf.Lerp(from, to, e / duration);
            fadeOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
    }
}
