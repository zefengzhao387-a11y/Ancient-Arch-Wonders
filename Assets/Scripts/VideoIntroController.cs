using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 进入大院后播放视频，结束后定格最后一帧，右下角「继续」按钮
/// </summary>
public class VideoIntroController : MonoBehaviour
{
    [Header("视频")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RawImage videoDisplay;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("继续按钮")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "RulesVideo";

    private RenderTexture _renderTexture;
    private bool _videoFailed;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.AddListener(OnContinueClick);
            var cg = continueButton.GetComponent<CanvasGroup>();
            if (cg == null) cg = continueButton.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        StartCoroutine(PlayVideo());
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

            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else
            {
                var url = System.IO.Path.Combine(Application.streamingAssetsPath, "intro.mp4");
                if (System.IO.File.Exists(url))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = VideoPlaybackUtility.FileUrlFromPath(url);
                }
            }

            videoPlayer.Prepare();
            float prepareTimeout = 15f;
            float elapsed = 0;
            while (!videoPlayer.isPrepared && !_videoFailed && elapsed < prepareTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            hasVideo = videoPlayer.isPrepared && !_videoFailed;

            if (hasVideo)
            {
                VideoPlaybackUtility.ApplyStandardCompat(videoPlayer);
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                int w = (int)videoPlayer.width;
                int h = (int)videoPlayer.height;
                if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
                _renderTexture = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
                videoPlayer.targetTexture = _renderTexture;
                videoDisplay.texture = _renderTexture;
                videoDisplay.color = Color.white;
                videoDisplay.gameObject.SetActive(true);

                videoPlayer.loopPointReached += OnVideoEnd;
                videoPlayer.Play();
                yield return VideoPlaybackUtility.CoWaitFirstFrameOrTimeout(videoPlayer, () => _videoFailed, 8f);
                if (_videoFailed || videoPlayer.frame < 0)
                {
                    videoPlayer.loopPointReached -= OnVideoEnd;
                    videoPlayer.Stop();
                    if (_renderTexture != null)
                    {
                        _renderTexture.Release();
                        _renderTexture = null;
                    }
                    videoDisplay.texture = null;
                    hasVideo = false;
                }
            }

            videoPlayer.errorReceived -= onErr;
        }

        yield return FadeOverlayAndButton(1f, 0f, fadeInDuration);

        if (!hasVideo)
        {
            yield return new WaitForSeconds(0.5f);
            yield return FadeOverlay(0f, 1f, fadeOutDuration);
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoEnd;
        vp.Pause();
    }

    private void OnContinueClick()
    {
        StartCoroutine(FadeOutThenLoad());
    }

    private IEnumerator FadeOutThenLoad()
    {
        if (continueButton != null) continueButton.interactable = false;
        yield return FadeOverlay(0f, 1f, fadeOutDuration);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null || duration <= 0) yield break;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var c = fadeOverlay.color;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }
    }

    private IEnumerator FadeOverlayAndButton(float from, float to, float duration)
    {
        if (duration <= 0) yield break;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (fadeOverlay != null)
            {
                var c = fadeOverlay.color;
                c.a = Mathf.Lerp(from, to, t);
                fadeOverlay.color = c;
            }
            if (continueButton != null)
            {
                var cg = continueButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = t;
            }
            yield return null;
        }
        if (continueButton != null)
        {
            var cg = continueButton.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
            _renderTexture.Release();
    }
}
