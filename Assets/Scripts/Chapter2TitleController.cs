using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章插图：显示「第二章」标题图，下方「进入下一站章节」按钮，点击进入第二章开场
/// </summary>
public class Chapter2TitleController : MonoBehaviour
{
    [Header("插图")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private Sprite illustrationSprite;

    [Header("标题文字（可选）")]
    [SerializeField] private Text titleText;

    [Header("进入下一站章节按钮")]
    [SerializeField] private Button nextChapterButton;
    [SerializeField] private Text nextChapterButtonText;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [SerializeField] private string nextSceneName = "Chapter2Intro";

    private bool _going;

    public void OnClickArea()
    {
        if (!_going) StartCoroutine(GoNext());
    }

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (illustrationImage != null && illustrationSprite != null) illustrationImage.sprite = illustrationSprite;
        if (titleText != null) titleText.text = "第二章";
        if (nextChapterButtonText != null) nextChapterButtonText.text = "进入下一站章节";
        if (nextChapterButton != null)
            nextChapterButton.onClick.AddListener(OnClickArea);

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
            OnClickArea();
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
