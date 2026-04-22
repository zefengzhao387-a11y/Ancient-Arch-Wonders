using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 户外场景：乔家大院外部，走到门前按 E 进入，渐隐渐显过渡。
/// 进门前可选两张说明图：第一张用「继续」按钮；第二张为点屏幕任意处继续并进入规则场景。
/// </summary>
public class OutdoorSceneController : MonoBehaviour
{
    [Header("进门前提示(可选)")]
    [SerializeField] private GameObject preEnterPanel;
    [SerializeField] private Image preEnterBackgroundImage;
    [SerializeField] private Sprite preEnterBackgroundSprite;
    [SerializeField] private Image preEnterDialogImage;
    [Tooltip("第一张对话框图（叠在背景上渐显）")]
    [SerializeField] private Sprite preEnterDialogSprite;
    [Tooltip("第二张对话框：独立 Image，与第一张不是同一个")]
    [SerializeField] private Image preEnterSecondDialogImage;
    [Tooltip("第二张对话框 Sprite；不填则第一次继续后直接进下一场景")]
    [SerializeField] private Sprite preEnterSecondDialogSprite;
    [SerializeField] private Button preEnterContinueButton;
    [SerializeField] private float preEnterDialogDelay = 0.5f;
    [SerializeField] private float preEnterDialogFadeDuration = 0.35f;
    [SerializeField] private AudioSource preEnterVoiceSource;
    [SerializeField] private AudioClip preEnterVoiceClip;
    [SerializeField] private AudioClip preEnterSecondVoiceClip;

    [Header("户外区域")]
    [SerializeField] private GameObject areaOutside;
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private RectTransform doorZone;
    [SerializeField] private float doorCenterX = 1200f;
    [SerializeField] private float doorWidth = 200f;
    [SerializeField] private GameObject pressEPrompt;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("下一场景")]
    [SerializeField] private string nextSceneName = "RulesVideo";

    [Header("键盘交互音效（可选）")]
    [SerializeField] private AudioClip keyboardInteractClip;
    [SerializeField] [Range(0f, 3f)] private float keyboardSfxVolume = 1f;

    [Header("门 / 进出场景（可选）")]
    [SerializeField] private AudioClip doorEnterClip;
    [SerializeField] private AudioClip doorExitClip;
    [SerializeField] [Range(0f, 3f)] private float doorSfxVolume = 1f;

    private bool _inDoorZone;
    private bool _entered;
    private bool _preEnterShowing;
    private int _preEnterDialogStep;
    /// <summary>第二张说明图：不显示继续按钮，点屏幕任意处进入规则场景。</summary>
    private bool _preEnterSecondAwaitingClickAnywhere;
    private bool _preEnterExiting;
    private AudioSource _keyboardSfx;
    private AudioSource _doorSfx;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (preEnterVoiceSource == null) preEnterVoiceSource = GetComponent<AudioSource>();

        if (preEnterBackgroundImage != null && preEnterBackgroundSprite != null)
            preEnterBackgroundImage.sprite = preEnterBackgroundSprite;
        if (preEnterDialogImage != null && preEnterDialogSprite != null)
            preEnterDialogImage.sprite = preEnterDialogSprite;
        if (preEnterSecondDialogImage != null && preEnterSecondDialogSprite != null)
            preEnterSecondDialogImage.sprite = preEnterSecondDialogSprite;

        if (preEnterPanel != null) preEnterPanel.SetActive(false);
        if (preEnterDialogImage != null)
        {
            preEnterDialogImage.gameObject.SetActive(true);
            SetDialogAlpha(preEnterDialogImage, 0f);
        }
        if (preEnterSecondDialogImage != null)
        {
            preEnterSecondDialogImage.gameObject.SetActive(true);
            SetDialogAlpha(preEnterSecondDialogImage, 0f);
        }

        if (preEnterContinueButton != null)
        {
            preEnterContinueButton.gameObject.SetActive(false);
            preEnterContinueButton.onClick.RemoveAllListeners();
            preEnterContinueButton.onClick.AddListener(OnPreEnterContinue);
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }
        StartCoroutine(StartFadeIn());
    }

    private IEnumerator StartFadeIn()
    {
        yield return new WaitForSeconds(0.2f);
        yield return Fade(1f, 0f, fadeInDuration);
    }

    private void Update()
    {
        if (_preEnterSecondAwaitingClickAnywhere && TryConsumePreEnterSecondAdvanceInput())
        {
            CompletePreEnterSequence();
            return;
        }

        if (_entered) return;
        if (playerRect == null) return;

        float playerX = GetCenterX(playerRect);
        float doorCenter, doorW;
        if (doorZone != null)
        {
            doorCenter = GetCenterX(doorZone);
            doorW = GetWorldWidth(doorZone) * 0.6f;
        }
        else
        {
            doorCenter = doorCenterX;
            doorW = doorWidth;
        }
        bool inZone = Mathf.Abs(playerX - doorCenter) < doorW;

        if (inZone != _inDoorZone)
        {
            _inDoorZone = inZone;
            if (pressEPrompt != null) pressEPrompt.SetActive(inZone);
        }

        if (_inDoorZone && Input.GetKeyDown(KeyCode.E))
        {
            PlayDoorEnterSfx();
            PlayKeyboardSfx();
            StartCoroutine(BeginEnterFlow());
        }
    }

    private IEnumerator BeginEnterFlow()
    {
        _entered = true;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        yield return Fade(0f, 1f, fadeOutDuration);

        if (preEnterPanel != null && preEnterBackgroundImage != null && preEnterDialogImage != null && preEnterContinueButton != null)
        {
            _preEnterShowing = true;
            _preEnterDialogStep = 0;
            preEnterPanel.SetActive(true);
            preEnterPanel.transform.SetAsLastSibling();
            SetDialogAlpha(preEnterDialogImage, 0f);
            if (preEnterDialogSprite != null) preEnterDialogImage.sprite = preEnterDialogSprite;
            if (preEnterSecondDialogImage != null)
            {
                if (preEnterSecondDialogSprite != null) preEnterSecondDialogImage.sprite = preEnterSecondDialogSprite;
                SetDialogAlpha(preEnterSecondDialogImage, 0f);
            }
            preEnterContinueButton.gameObject.SetActive(false);
            preEnterContinueButton.interactable = false;
            yield return new WaitForSeconds(Mathf.Max(0f, preEnterDialogDelay));
            // 配音与第一张图渐显同步，不要绑在「继续」按钮出现瞬间（否则易被当成点击音）
            PlayPreEnterVoice(preEnterVoiceClip);
            yield return FadeDialogAlpha(preEnterDialogImage, 0f, 1f, preEnterDialogFadeDuration);
            preEnterContinueButton.gameObject.SetActive(true);
            preEnterContinueButton.interactable = true;
            yield break;
        }

        LoadNextScene();
    }

    private void OnPreEnterContinue()
    {
        if (!_preEnterShowing) return;
        StartCoroutine(HandlePreEnterContinue());
    }

    private IEnumerator HandlePreEnterContinue()
    {
        if (preEnterSecondDialogSprite != null && _preEnterDialogStep == 0)
        {
            preEnterContinueButton.interactable = false;
            StopPreEnterVoice();
            if (preEnterSecondDialogImage != null)
            {
                yield return FadeDialogAlpha(preEnterDialogImage, 1f, 0f, preEnterDialogFadeDuration);
                PlayPreEnterVoice(preEnterSecondVoiceClip);
                yield return FadeDialogAlpha(preEnterSecondDialogImage, 0f, 1f, preEnterDialogFadeDuration);
            }
            else
            {
                Debug.LogWarning("OutdoorSceneController：已配置 preEnterSecondDialogSprite，但未绑定 preEnterSecondDialogImage，将直接进入下一场景。");
                _preEnterShowing = false;
                if (preEnterPanel != null) preEnterPanel.SetActive(false);
                LoadNextScene();
                yield break;
            }
            _preEnterDialogStep = 1;
            if (preEnterContinueButton != null)
            {
                preEnterContinueButton.gameObject.SetActive(false);
                preEnterContinueButton.interactable = false;
            }
            _preEnterSecondAwaitingClickAnywhere = true;
            yield break;
        }

        CompletePreEnterSequence();
    }

    private static bool TryConsumePreEnterSecondAdvanceInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            return true;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                return true;
        }
        return Input.anyKeyDown;
    }

    private void CompletePreEnterSequence()
    {
        if (_preEnterExiting) return;
        _preEnterExiting = true;
        _preEnterShowing = false;
        _preEnterSecondAwaitingClickAnywhere = false;
        StopPreEnterVoice();
        if (preEnterContinueButton != null)
        {
            preEnterContinueButton.interactable = false;
            preEnterContinueButton.gameObject.SetActive(false);
        }
        if (preEnterPanel != null) preEnterPanel.SetActive(false);
        LoadNextScene();
    }

    private void PlayPreEnterVoice(AudioClip clip)
    {
        if (preEnterVoiceSource == null) return;
        preEnterVoiceSource.Stop();
        preEnterVoiceSource.clip = clip;
        if (clip != null) preEnterVoiceSource.Play();
    }

    private void StopPreEnterVoice()
    {
        if (preEnterVoiceSource != null) preEnterVoiceSource.Stop();
    }

    private static void SetDialogAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private IEnumerator FadeDialogAlpha(Image img, float from, float to, float duration)
    {
        if (img == null || duration <= 0.001f)
        {
            SetDialogAlpha(img, to);
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetDialogAlpha(img, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetDialogAlpha(img, to);
    }

    private void LoadNextScene()
    {
        PlayDoorExitSfx();
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

    private static float GetCenterX(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0].x + corners[2].x) * 0.5f;
    }

    private static float GetWorldWidth(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return corners[2].x - corners[0].x;
    }

    void EnsureKeyboardSfx()
    {
        if (keyboardInteractClip == null) return;
        if (_keyboardSfx != null) return;
        var go = new GameObject("KeyboardSfx");
        go.transform.SetParent(transform, false);
        _keyboardSfx = go.AddComponent<AudioSource>();
        _keyboardSfx.playOnAwake = false;
        _keyboardSfx.loop = false;
        _keyboardSfx.spatialBlend = 0f;
    }

    void PlayKeyboardSfx()
    {
        if (keyboardInteractClip == null) return;
        EnsureKeyboardSfx();
        if (_keyboardSfx == null) return;
        _keyboardSfx.PlayOneShot(keyboardInteractClip, Mathf.Max(0f, keyboardSfxVolume));
    }

    void EnsureDoorSfx()
    {
        if (doorEnterClip == null && doorExitClip == null) return;
        if (_doorSfx != null) return;
        var go = new GameObject("DoorSfx");
        go.transform.SetParent(transform, false);
        _doorSfx = go.AddComponent<AudioSource>();
        _doorSfx.playOnAwake = false;
        _doorSfx.loop = false;
        _doorSfx.spatialBlend = 0f;
    }

    void PlayDoorEnterSfx()
    {
        if (doorEnterClip == null) return;
        EnsureDoorSfx();
        if (_doorSfx == null) return;
        _doorSfx.PlayOneShot(doorEnterClip, Mathf.Max(0f, doorSfxVolume));
    }

    void PlayDoorExitSfx()
    {
        if (doorExitClip == null) return;
        EnsureDoorSfx();
        if (_doorSfx == null) return;
        _doorSfx.PlayOneShot(doorExitClip, Mathf.Max(0f, doorSfxVolume));
    }
}
