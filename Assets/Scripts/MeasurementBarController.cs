using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 测量仪：指针在指定范围内上下移动，零度时榫卯发光，按空格完成嵌入
/// 可配置指针范围：minValue~maxValue，零度容差 zeroTolerance
/// </summary>
public class MeasurementBarController : MonoBehaviour
{
    [Header("测量仪")]
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform needleRect;
    [SerializeField] private float minValue = 0f;       // 指针范围最小值（0~1 对应底部到顶部）
    [SerializeField] private float maxValue = 1f;       // 指针范围最大值
    [SerializeField] private float zeroValue = 0.5f;    // 零度位置（0.5=中间）
    [SerializeField] private float zeroTolerance = 0.05f;

    [Header("指针运动")]
    [SerializeField] private float moveSpeed = 0.3f;
    [SerializeField] private bool reverseAtEdges = true;
    [SerializeField] private float startValue = 0.5f;

    [Header("榫卯联动")]
    [SerializeField] private GameObject tenonMortiseGlow;

    [Header("完成")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;  // 默认 Enter，若空格导致游戏退出多为编辑器冲突
    [Header("键盘交互音效（可选）")]
    [SerializeField] private AudioClip keyboardInteractClip;
    [SerializeField] [Range(0f, 3f)] private float keyboardSfxVolume = 1f;

    public event Action OnSuccess;

    private float _currentValue;
    private float _direction = 1f;
    private bool _completed;
    private bool _isAtZero;
    private bool _isActive;
    private AudioSource _keyboardSfx;

    public float CurrentValue => _currentValue;
    public bool IsAtZero => _isAtZero;
    public bool IsCompleted => _completed;
    public bool IsActive => _isActive;

    public void StartMeasurement()
    {
        _completed = false;
        _isActive = true;
    }

    public void SetGlowTarget(RectTransform target)
    {
        if (tenonMortiseGlow == null || target == null) return;
        var glowRect = tenonMortiseGlow.GetComponent<RectTransform>();
        if (glowRect != null)
        {
            glowRect.SetParent(target, false);
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
        }
    }

    public bool TryCompleteByClick()
    {
        if (_completed || !_isActive || !_isAtZero) return false;
        _completed = true;
        OnSuccess?.Invoke();
        if (tenonMortiseGlow != null) tenonMortiseGlow.SetActive(false);
        return true;
    }

    private void Start()
    {
        if (barRect == null)
        {
            var bar = transform.Find("Bar");
            if (bar != null) barRect = bar as RectTransform;
        }
        if (needleRect == null && barRect != null)
        {
            var needle = barRect.Find("Needle");
            if (needle != null) needleRect = needle as RectTransform;
        }
        _currentValue = startValue;
        _isActive = false;
        if (tenonMortiseGlow != null) tenonMortiseGlow.SetActive(false);
        EnsureKeyboardSfx();
    }

    void EnsureKeyboardSfx()
    {
        if (keyboardInteractClip == null) return;
        if (_keyboardSfx != null) return;
        _keyboardSfx = gameObject.AddComponent<AudioSource>();
        _keyboardSfx.playOnAwake = false;
        _keyboardSfx.loop = false;
        _keyboardSfx.spatialBlend = 0f;
    }

    void PlayKeyboardSfx()
    {
        if (keyboardInteractClip == null || _keyboardSfx == null) return;
        _keyboardSfx.PlayOneShot(keyboardInteractClip, Mathf.Max(0f, keyboardSfxVolume));
    }

    private void Update()
    {
        if (_completed || !_isActive) return;

        _currentValue += _direction * moveSpeed * Time.deltaTime;
        if (reverseAtEdges)
        {
            if (_currentValue >= maxValue) { _currentValue = maxValue; _direction = -1f; }
            if (_currentValue <= minValue) { _currentValue = minValue; _direction = 1f; }
        }
        else
        {
            _currentValue = Mathf.Clamp(_currentValue, minValue, maxValue);
        }

        UpdateNeedlePosition();
        UpdateZeroState();

        if (Input.GetKeyDown(confirmKey))
        {
            PlayKeyboardSfx();
            TryCompleteByClick();
        }
    }

    private void UpdateNeedlePosition()
    {
        if (needleRect == null) return;
        float t = Mathf.InverseLerp(minValue, maxValue, _currentValue);
        needleRect.anchorMin = new Vector2(0.5f, t);
        needleRect.anchorMax = new Vector2(0.5f, t);
        needleRect.anchoredPosition = Vector2.zero;
    }

    private void UpdateZeroState()
    {
        _isAtZero = Mathf.Abs(_currentValue - zeroValue) <= zeroTolerance;
        if (tenonMortiseGlow != null)
            tenonMortiseGlow.SetActive(_isAtZero && !_completed);
    }
}
