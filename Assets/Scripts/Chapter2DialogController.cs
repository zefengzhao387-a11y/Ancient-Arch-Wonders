using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章水镜台：3个对话框依次显示，点继续切换下一个
/// </summary>
public class Chapter2DialogController : MonoBehaviour
{
    [Header("对话框")]
    [SerializeField] private Image dialogBoxImage;
    [SerializeField] private Text dialogText;
    [SerializeField] private string[] dialogContents = new string[3];
    [SerializeField] private Sprite[] dialogBoxSprites;  // 每个对话单独的原图像，可设 3 张
    [SerializeField] private Button continueButton;

    [Header("配音（可选，与 dialogContents 下标一一对应）")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioClip[] dialogVoiceClips;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [Tooltip("对话框与「继续」按钮同步渐显/渐隐时长")]
    [SerializeField] private float dialogAndButtonFadeDuration = 0.35f;

    [SerializeField] private string nextSceneName = "Chapter2Scroll";

    private int _currentIndex;
    private CanvasGroup _dialogRootGroup;
    private CanvasGroup _continueButtonGroup;
    private bool _dialogTransitionBusy;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();
        if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
        voiceAudioSource.playOnAwake = false;
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (dialogText != null)
            SubtitleStyleUtility.ApplyToSubtitle(dialogText, null);

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }

        EnsureDialogAndButtonCanvasGroups();
        SetDialogAndButtonAlpha(0f);
        SetContinueInteractable(false);

        _currentIndex = 0;
        ShowDialog(0, playVoice: false);
        StartCoroutine(StartFadeIn());
    }

    private IEnumerator StartFadeIn()
    {
        yield return new WaitForSeconds(0.2f);
        yield return Fade(1f, 0f, fadeInDuration);
        yield return FadeDialogAndButton(0f, 1f, dialogAndButtonFadeDuration);
        PlayDialogVoice(_currentIndex);
        SetContinueInteractable(true);
    }

    private void ShowDialog(int index, bool playVoice = true)
    {
        if (dialogText != null && dialogContents != null && index < dialogContents.Length && dialogContents[index] != null)
            dialogText.text = dialogContents[index];
        else if (dialogText != null)
            dialogText.text = "";
        if (dialogBoxImage != null)
        {
            if (dialogBoxSprites != null && index < dialogBoxSprites.Length && dialogBoxSprites[index] != null)
                dialogBoxImage.sprite = dialogBoxSprites[index];
            dialogBoxImage.gameObject.SetActive(true);
        }

        if (playVoice)
            PlayDialogVoice(index);
    }

    private void PlayDialogVoice(int index)
    {
        if (voiceAudioSource == null) return;
        voiceAudioSource.Stop();
        if (dialogVoiceClips == null || index < 0 || index >= dialogVoiceClips.Length)
            return;
        var clip = dialogVoiceClips[index];
        if (clip == null) return;
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
    }

    private void StopDialogVoice()
    {
        if (voiceAudioSource == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.clip = null;
    }

    private void OnContinue()
    {
        if (_dialogTransitionBusy) return;
        StartCoroutine(OnContinueRoutine());
    }

    private IEnumerator OnContinueRoutine()
    {
        _dialogTransitionBusy = true;
        SetContinueInteractable(false);
        yield return FadeDialogAndButton(1f, 0f, dialogAndButtonFadeDuration);

        _currentIndex++;
        int len = dialogContents != null ? dialogContents.Length : 0;
        if (_currentIndex >= len)
        {
            StopDialogVoice();
            if (dialogBoxImage != null) dialogBoxImage.gameObject.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            StartCoroutine(LoadNextScene());
            _dialogTransitionBusy = false;
            yield break;
        }

        ShowDialog(_currentIndex, playVoice: false);
        yield return FadeDialogAndButton(0f, 1f, dialogAndButtonFadeDuration);
        PlayDialogVoice(_currentIndex);
        SetContinueInteractable(true);
        _dialogTransitionBusy = false;
    }

    private IEnumerator LoadNextScene()
    {
        if (fadeOverlay != null)
        {
            yield return Fade(0f, 1f, 0.8f);
        }
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

    void EnsureDialogAndButtonCanvasGroups()
    {
        if (dialogBoxImage != null)
        {
            _dialogRootGroup = dialogBoxImage.GetComponent<CanvasGroup>();
            if (_dialogRootGroup == null) _dialogRootGroup = dialogBoxImage.gameObject.AddComponent<CanvasGroup>();
        }
        if (continueButton != null)
        {
            _continueButtonGroup = continueButton.GetComponent<CanvasGroup>();
            if (_continueButtonGroup == null) _continueButtonGroup = continueButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void SetDialogAndButtonAlpha(float a)
    {
        a = Mathf.Clamp01(a);
        if (_dialogRootGroup != null) _dialogRootGroup.alpha = a;
        if (_continueButtonGroup != null) _continueButtonGroup.alpha = a;
    }

    void SetContinueInteractable(bool on)
    {
        if (continueButton != null) continueButton.interactable = on;
    }

    IEnumerator FadeDialogAndButton(float from, float to, float duration)
    {
        if (duration <= 0.001f)
        {
            SetDialogAndButtonAlpha(to);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Clamp01(e / duration);
            SetDialogAndButtonAlpha(Mathf.Lerp(from, to, u));
            yield return null;
        }
        SetDialogAndButtonAlpha(to);
    }
}
