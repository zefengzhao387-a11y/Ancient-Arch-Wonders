using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景加载后黑屏渐显，用于衔接上一场景的渐隐
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1f;

    private void Start()
    {
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (fadeOverlay == null) return;

        fadeOverlay.transform.SetAsLastSibling();
        var c = fadeOverlay.color;
        c.a = 1f;
        fadeOverlay.color = c;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadeDuration <= 0) yield break;
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            var c = fadeOverlay.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }
        var final = fadeOverlay.color;
        final.a = 0f;
        fadeOverlay.color = final;
    }
}
