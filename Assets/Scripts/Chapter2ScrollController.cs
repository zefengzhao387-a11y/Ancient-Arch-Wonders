using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章卷轴：卷轴飞来视频 → 脸上有卷轴+点击提示 → 点击播放解卷轴动画 → 黑场渐显图四（卷轴内画面）→ 继续
/// </summary>
public class Chapter2ScrollController : MonoBehaviour
{
    [Header("卷轴飞来")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip scrollFlyClip;
    [SerializeField] private RawImage videoDisplay;

    [Header("点击区域（叠在视频最后一帧上）")]
    [SerializeField] private GameObject clickOverlay;

    [Header("解卷轴动画")]
    [SerializeField] private VideoClip unfurlClip;

    [Header("图四")]
    [SerializeField] private GameObject drawingPanel;
    [SerializeField] private Image drawingImage;
    [SerializeField] private Sprite drawingSprite;
    [SerializeField] private Button continueButton;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [Tooltip("解卷轴结束后，从黑场渐显图四的时长")]
    [SerializeField] private float revealDrawingDuration = 1f;

    [SerializeField] private string nextSceneName = "Chapter2Platformer";

    private RenderTexture _renderTexture;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();

        if (drawingPanel != null) drawingPanel.SetActive(false);
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueClick);
        }
        if (clickOverlay != null)
        {
            clickOverlay.SetActive(false);
            var btn = clickOverlay.GetComponent<Button>();
            if (btn == null) btn = clickOverlay.AddComponent<Button>();
            btn.onClick.AddListener(OnClickOverlay);
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        bool hasVideo = false;
        if (videoPlayer != null && videoDisplay != null && scrollFlyClip != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = scrollFlyClip;
            videoPlayer.Prepare();
            float t = 0;
            while (!videoPlayer.isPrepared && t < 10f) { t += Time.deltaTime; yield return null; }
            hasVideo = videoPlayer.isPrepared;
            if (hasVideo)
            {
                int w = (int)videoPlayer.width, h = (int)videoPlayer.height;
                if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
                _renderTexture = new RenderTexture(w, h, 0);
                videoPlayer.targetTexture = _renderTexture;
                videoDisplay.texture = _renderTexture;
                videoDisplay.gameObject.SetActive(true);
                videoPlayer.loopPointReached += OnFlyVideoEnd;
                videoPlayer.Play();
            }
        }

        yield return Fade(1f, 0f, fadeInDuration);

        if (!hasVideo)
        {
            if (clickOverlay != null) clickOverlay.SetActive(true);
        }
    }

    private void OnFlyVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnFlyVideoEnd;
        vp.Pause();
        if (clickOverlay != null) clickOverlay.SetActive(true);
    }

    private void OnClickOverlay()
    {
        if (clickOverlay != null) clickOverlay.SetActive(false);
        if (unfurlClip != null && videoPlayer != null)
            StartCoroutine(PlayUnfurlThenShowDrawing());
        else
            OnUnfurlComplete();
    }

    private IEnumerator PlayUnfurlThenShowDrawing()
    {
        videoPlayer.clip = unfurlClip;
        videoPlayer.Prepare();
        float t = 0;
        while (!videoPlayer.isPrepared && t < 10f) { t += Time.deltaTime; yield return null; }
        if (videoDisplay != null) videoDisplay.gameObject.SetActive(true);
        videoPlayer.loopPointReached += OnUnfurlVideoEnd;
        videoPlayer.Play();
    }

    private void OnUnfurlVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnUnfurlVideoEnd;
        vp.Pause();
        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
        StartCoroutine(RevealDrawingFromBlack());
    }

    private void OnUnfurlComplete()
    {
        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
        StartCoroutine(RevealDrawingFromBlack());
    }

    /// <summary>黑屏全不透明盖住画面，底下已显示图四，再渐隐遮罩以「渐显」卷轴内内容。</summary>
    private IEnumerator RevealDrawingFromBlack()
    {
        if (clickOverlay != null) clickOverlay.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var fc = fadeOverlay.color;
            fc.a = 1f;
            fadeOverlay.color = fc;
        }

        if (drawingPanel != null) drawingPanel.SetActive(true);
        if (drawingImage != null && drawingSprite != null) drawingImage.sprite = drawingSprite;

        if (fadeOverlay != null)
            yield return Fade(1f, 0f, revealDrawingDuration > 0f ? revealDrawingDuration : fadeInDuration);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private void OnContinueClick()
    {
        StartCoroutine(LoadNext());
    }

    private IEnumerator LoadNext()
    {
        if (fadeOverlay != null) yield return Fade(0f, 1f, 0.8f);
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
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

    private void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
    }
}
