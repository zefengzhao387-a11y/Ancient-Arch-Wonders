using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 章节过渡：显示插图，下方「进入下一站章节」按钮，点击后渐隐并加载下一章
/// </summary>
public class ChapterTransitionController : MonoBehaviour
{
    [Header("插图")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private Sprite illustrationSprite;

    [Header("标题文字（可选）")]
    [SerializeField] private Text titleText;
    [SerializeField] private string titleString = "";

    [Header("进入下一站章节按钮")]
    [SerializeField] private Button nextChapterButton;
    [SerializeField] private Text nextChapterButtonText;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [SerializeField] private string nextSceneName = "";

    private bool _going;

    public void OnNextChapterClick()
    {
        if (!_going) StartCoroutine(GoNext());
    }

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (illustrationImage != null && illustrationSprite != null) illustrationImage.sprite = illustrationSprite;
        if (titleText != null && !string.IsNullOrEmpty(titleString)) titleText.text = titleString;
        if (nextChapterButtonText != null) nextChapterButtonText.text = "进入下一站章节";

        if (nextChapterButton != null)
            nextChapterButton.onClick.AddListener(OnNextChapterClick);

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
        if (!_going && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            OnNextChapterClick();
    }

    private IEnumerator GoNext()
    {
        _going = true;
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
}
