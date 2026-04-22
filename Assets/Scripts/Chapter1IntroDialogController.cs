using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开场与第一章户外之间：一张背景图 → 约 0.5s 后在背景上渐显对话框（含继续）→
/// 点继续渐显下一张对话框 → 再点继续黑屏渐隐后进入户外（乔乔可走动）。
/// </summary>
public class Chapter1IntroDialogController : MonoBehaviour
{
    [Header("背景（整屏一张图）")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundSprite;

    [Header("对话框（叠在背景上，图里可含字）")]
    [SerializeField] private Image dialogImage;
    [SerializeField] private Sprite firstDialogSprite;
    [SerializeField] private Sprite secondDialogSprite;
    [SerializeField] private Button continueButton;

    [Header("时间")]
    [Tooltip("背景出现后，延迟多久再渐显第一张对话框")]
    [SerializeField] private float dialogAppearDelay = 0.5f;
    [Tooltip("对话框渐显/渐隐时长")]
    [SerializeField] private float dialogFadeDuration = 0.35f;

    [Header("音频（可选）")]
    [SerializeField] private AudioClip firstDialogVoice;
    [SerializeField] private AudioClip secondDialogVoice;
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("全屏渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float sceneFadeInDuration = 0.6f;
    [SerializeField] private float sceneFadeOutDuration = 0.8f;

    [Header("下一场景")]
    [SerializeField] private string nextSceneName = "Outdoor";

    private enum Stage { ShowingBackground, FirstDialog, SecondDialog, Transitioning }
    private Stage _stage = Stage.ShowingBackground;
    private bool _isTransitioning;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();

        if (backgroundImage != null && backgroundSprite != null)
            backgroundImage.sprite = backgroundSprite;

        if (dialogImage != null)
        {
            dialogImage.gameObject.SetActive(true);
            SetDialogAlpha(0f);
            if (firstDialogSprite != null) dialogImage.sprite = firstDialogSprite;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClick);
            continueButton.gameObject.SetActive(false);
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        yield return FadeOverlay(1f, 0f, sceneFadeInDuration);

        yield return new WaitForSeconds(Mathf.Max(0f, dialogAppearDelay));

        _stage = Stage.FirstDialog;
        if (dialogImage != null && firstDialogSprite != null)
            dialogImage.sprite = firstDialogSprite;
        yield return FadeDialogAlpha(0f, 1f, dialogFadeDuration);
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }
        PlayVoice(firstDialogVoice);
    }

    private void OnContinueClick()
    {
        if (_isTransitioning) return;

        if (_stage == Stage.FirstDialog)
        {
            StartCoroutine(AfterFirstContinue());
            return;
        }

        if (_stage == Stage.SecondDialog)
            StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator AfterFirstContinue()
    {
        if (continueButton != null) continueButton.interactable = false;
        StopVoice();

        yield return FadeDialogAlpha(1f, 0f, dialogFadeDuration);

        _stage = Stage.SecondDialog;
        if (dialogImage != null && secondDialogSprite != null)
            dialogImage.sprite = secondDialogSprite;

        yield return FadeDialogAlpha(0f, 1f, dialogFadeDuration);

        if (continueButton != null) continueButton.interactable = true;
        PlayVoice(secondDialogVoice);
    }

    private IEnumerator FadeOutAndLoad()
    {
        _isTransitioning = true;
        _stage = Stage.Transitioning;
        StopVoice();
        if (continueButton != null) continueButton.interactable = false;

        yield return FadeDialogAlpha(1f, 0f, dialogFadeDuration);
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        yield return FadeOverlay(0f, 1f, sceneFadeOutDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private void SetDialogAlpha(float a)
    {
        if (dialogImage == null) return;
        var c = dialogImage.color;
        c.a = a;
        dialogImage.color = c;
    }

    private IEnumerator FadeDialogAlpha(float from, float to, float duration)
    {
        if (dialogImage == null || duration <= 0f)
        {
            SetDialogAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetDialogAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetDialogAlpha(to);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
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
        var end = fadeOverlay.color;
        end.a = to;
        fadeOverlay.color = end;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
    }

    private void StopVoice()
    {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
    }
}
