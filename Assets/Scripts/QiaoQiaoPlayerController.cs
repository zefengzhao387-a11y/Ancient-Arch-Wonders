using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 乔乔角色：A/D 左右移动，行走帧切换动画
/// 将行走帧 Sprite 拖入 walkFrames 数组，按顺序切换形成行走效果
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class QiaoQiaoPlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float minX = 100f;
    [SerializeField] private float maxX = 1750f;

    [Header("人物形象")]
    [Tooltip("静止时的站立图")]
    [SerializeField] private Sprite idleSprite;
    [Tooltip("行走帧：设置 Size 后把行走帧图拖入 Element 0、1、2...")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float frameInterval = 0.12f;

    [Header("行走音效（可选）")]
    [Tooltip("可拖多条（如 5 条），走路时按顺序轮流播放")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] [Range(0f, 2f)] private float footstepVolume = 0.85f;
    [Tooltip("两次脚步音最短间隔（秒），避免帧切太快时声音过密")]
    [SerializeField] private float minFootstepInterval = 0.32f;

    [Header("移动提示")]
    [SerializeField] private GameObject moveHint;  // 按AD进行移动，移动后隐藏

    private RectTransform _rect;
    private Image _image;
    private AudioSource _footstepSource;
    private float _frameTimer;
    private int _frameIndex;
    private float _lastMoveDir;  // 1=右, -1=左, 0=静止
    private float _lastFootstepTime = -999f;
    private int _footstepClipIndex;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = FootShadow.GetCharacterImage(transform);
        if (idleSprite != null && _image != null) _image.sprite = idleSprite;

        if (HasAnyFootstepClip())
        {
            _footstepSource = GetComponent<AudioSource>();
            if (_footstepSource == null)
                _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake = false;
            _footstepSource.loop = false;
            _footstepSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (h != 0)
        {
            if (moveHint != null) moveHint.SetActive(false);

            var pos = _rect.anchoredPosition;
            pos.x += h * moveSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            _rect.anchoredPosition = pos;

            // 翻转朝向
            var scale = _rect.localScale;
            scale.x = h > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _rect.localScale = scale;

            _lastMoveDir = h;
            UpdateWalkFrame();
        }
        else
        {
            _lastMoveDir = 0;
            if (idleSprite != null && _image != null) _image.sprite = idleSprite;
        }
    }

    private void UpdateWalkFrame()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= frameInterval)
        {
            _frameTimer = 0;
            _frameIndex = (_frameIndex + 1) % walkFrames.Length;
            if (walkFrames[_frameIndex] != null)
                _image.sprite = walkFrames[_frameIndex];
            TryPlayFootstep();
        }
    }

    bool HasAnyFootstepClip()
    {
        if (footstepClips == null || footstepClips.Length == 0) return false;
        foreach (var c in footstepClips)
            if (c != null) return true;
        return false;
    }

    void TryPlayFootstep()
    {
        if (_footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
        if (Time.time - _lastFootstepTime < minFootstepInterval) return;

        var clip = PickNextFootstepClip();
        if (clip == null) return;

        _lastFootstepTime = Time.time;
        _footstepSource.PlayOneShot(clip, Mathf.Max(0f, footstepVolume));
    }

    AudioClip PickNextFootstepClip()
    {
        for (var attempt = 0; attempt < footstepClips.Length; attempt++)
        {
            var i = _footstepClipIndex % footstepClips.Length;
            _footstepClipIndex = (i + 1) % footstepClips.Length;
            if (footstepClips[i] != null) return footstepClips[i];
        }
        return null;
    }
}
