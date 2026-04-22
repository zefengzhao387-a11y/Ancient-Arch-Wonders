using UnityEngine;

/// <summary>
/// 全局 UI 点击音：单例 + DontDestroyOnLoad，一条 AudioSource 播所有 UIButtonSfx。
/// 必须挂在单独空物体上（与 Canvas 平级），不要挂在带 Canvas 的同一物体上，
/// 否则 DontDestroyOnLoad 会把整块 UI 根节点挪走，容易出现主菜单无画面。
/// 若未放 Hub，首次点击时会自动创建空物体（仍须在 Inspector 给默认音）。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameUISfxHub : MonoBehaviour
{
    static GameUISfxHub _instance;

    /// <summary>首场景 Hub 在 Inspector 里设的默认点击音；切场景后自动新建的 Hub 会复用，避免「后面按钮没声」。</summary>
    static AudioClip _cachedDefaultClip;

    static float _cachedVolumeScale = 1.75f;

    public static GameUISfxHub Instance => _instance;

    [SerializeField] private AudioClip defaultButtonClick;

    [Tooltip("PlayOneShot 音量倍率")]
    [SerializeField] [Range(0f, 3f)] private float buttonClickVolumeScale = 1.75f;

    AudioSource _sfx;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (defaultButtonClick != null)
        {
            _cachedDefaultClip = defaultButtonClick;
            _cachedVolumeScale = buttonClickVolumeScale;
        }
        else if (_cachedDefaultClip != null)
        {
            defaultButtonClick = _cachedDefaultClip;
            buttonClickVolumeScale = _cachedVolumeScale;
        }

        // AI辅助：Google Gemini（2026-04，DontDestroyOnLoad 与 UI 子树误判讨论）— 以下为人工实现的 GetComponentInParent 分支与警告文案。
        // 挂在任意 Canvas 子树（含与 Canvas 同物体）上时绝不能 DDoL 本物体：MarkPersistRoot 会把整块 UI 或单个按钮从层级里扯走。
        var hostCanvas = GetComponentInParent<Canvas>(true);
        if (hostCanvas != null && hostCanvas.gameObject == gameObject)
            Debug.LogWarning(
                "[GameUISfxHub] 与 Canvas 挂在同一物体上，已跳过 DontDestroyOnLoad。请挪到与 Canvas 平级的空物体上，再保存场景。");
        else if (hostCanvas != null)
            Debug.LogWarning(
                "[GameUISfxHub] 挂在 Canvas 下的 UI 上，已跳过 DontDestroyOnLoad（否则会整块进 DontDestroyOnLoad、按钮跑出 Canvas）。"
                + "请使用菜单 Tools→游戏性→当前场景添加 GameUISfxHub，或与 Canvas 平级的空物体。");
        else
            VideoPlaybackUtility.MarkPersistRoot(gameObject);

        _sfx = GetComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
        // 勿改 loop：常与 PersistentGameBGM 共用同一 AudioSource（都挂在 Canvas 上），设 false 会关掉 BGM 循环。
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>若场景里没有 Hub，则创建并挂 AudioSource（默认音仍须在 Inspector 或代码里设）。</summary>
    public static void EnsureExists()
    {
        if (_instance != null) return;
        var existing = Object.FindObjectsOfType<GameUISfxHub>();
        if (existing != null && existing.Length > 0)
        {
            _instance = existing[0];
            return;
        }

        var go = new GameObject("GameUISfxHub");
        go.AddComponent<AudioSource>();
        go.AddComponent<GameUISfxHub>();
    }

    /// <summary>供 UIButtonSfx 调用：保证有 Hub 后播放（override 非空则播 override，否则播默认）。</summary>
    public static void PlayShared(AudioClip optionalOverride)
    {
        EnsureExists();
        _instance?.PlayButtonClick(optionalOverride);
    }

    public void PlayButtonClick(AudioClip optionalOverride)
    {
        var clip = optionalOverride != null ? optionalOverride : defaultButtonClick;
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, Mathf.Max(0f, buttonClickVolumeScale));
    }

    public void SetDefaultButtonClick(AudioClip clip) => defaultButtonClick = clip;

    public void SetButtonClickVolumeScale(float scale) => buttonClickVolumeScale = Mathf.Clamp(scale, 0f, 3f);
}
