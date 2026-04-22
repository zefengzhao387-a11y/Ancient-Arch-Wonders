using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章开场：背景图、虚线箭头闪烁、向前进入晋祠、走到门口按E进入水镜台
/// </summary>
public class Chapter2IntroController : MonoBehaviour
{
    [Header("场景")]
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private RectTransform doorZone;
    [SerializeField] private float doorCenterX = 1200f;
    [SerializeField] private float doorWidth = 200f;
    [SerializeField] private GameObject pressEPrompt;

    [Header("箭头闪烁")]
    [SerializeField] private Graphic arrowImage;
    [SerializeField] private float blinkInterval = 0.5f;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [SerializeField] private string nextSceneName = "Chapter2Shuijing";

    [Header("键盘交互音效（可选）")]
    [SerializeField] private AudioClip keyboardInteractClip;
    [SerializeField] [Range(0f, 3f)] private float keyboardSfxVolume = 1f;

    [Header("门 / 进出场景（可选）")]
    [SerializeField] private AudioClip doorEnterClip;
    [SerializeField] private AudioClip doorExitClip;
    [SerializeField] [Range(0f, 3f)] private float doorSfxVolume = 1f;

    private bool _inDoorZone;
    private bool _entered;
    private AudioSource _keyboardSfx;
    private AudioSource _doorSfx;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (arrowImage != null) StartCoroutine(BlinkArrow());

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

    private IEnumerator BlinkArrow()
    {
        while (arrowImage != null && !_entered)
        {
            arrowImage.enabled = false;
            yield return new WaitForSeconds(blinkInterval);
            if (_entered) yield break;
            arrowImage.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void Update()
    {
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
            StartCoroutine(EnterDoor());
        }
    }

    private IEnumerator EnterDoor()
    {
        _entered = true;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);

        PlayDoorExitSfx();
        yield return Fade(0f, 1f, fadeOutDuration);

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
