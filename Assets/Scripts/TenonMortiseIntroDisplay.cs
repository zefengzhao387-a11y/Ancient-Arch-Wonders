using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 榫卯完全铆合后展示介绍图：渐显，右下角继续按钮
/// </summary>
public class TenonMortiseIntroDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image introImage;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Button continueButton;

    [Header("渐变")]
    [SerializeField] private float fadeInDuration = 1f;

    /// <summary>点击继续时回调，用于章节结束时跳转</summary>
    public Action OnContinueClicked;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClick);
    }

    /// <summary>显示榫卯介绍图，无图则直接跳过</summary>
    public void Show(Sprite introSprite)
    {
        if (introSprite == null)
        {
            OnContinueClick();
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (introImage != null)
        {
            introImage.sprite = introSprite;
            introImage.color = new Color(1, 1, 1, 0);
        }
        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
        }
        if (continueButton != null) continueButton.gameObject.SetActive(true);

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0) yield break;

        float elapsed = 0;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            if (fadeOverlay != null)
            {
                var c = fadeOverlay.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                fadeOverlay.color = c;
            }
            if (introImage != null)
            {
                var c = introImage.color;
                c.a = t;
                introImage.color = c;
            }
            yield return null;
        }

        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
        }
        if (introImage != null)
        {
            var c = introImage.color;
            c.a = 1f;
            introImage.color = c;
        }
    }

    private void OnContinueClick()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        OnContinueClicked?.Invoke();
    }
}
