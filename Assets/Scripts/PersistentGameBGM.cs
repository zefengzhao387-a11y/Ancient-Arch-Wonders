using UnityEngine;

/// <summary>
/// 全游戏跨场景常驻 BGM。挂在首个流程场景（默认 GameMenu）的根物体上，Awake 时 DontDestroyOnLoad。
/// 再次进入主菜单时若场景里又放了一份，多余实例会自毁，音乐不中断。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PersistentGameBGM : MonoBehaviour
{
    public static PersistentGameBGM Instance { get; private set; }

    [SerializeField] private AudioClip bgmClip;
    [Tooltip("未拖 bgmClip 时尝试 Resources.Load，路径相对 Assets/Resources（无扩展名），例如 GameBGM/MainTheme")]
    [SerializeField] private string resourcesFallbackPath = "";

    [SerializeField] [Range(0f, 1f)] private float volume = 0.55f;

    AudioSource _audio;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 勿 Destroy(gameObject)：若挂在 Canvas 上会删掉整块主菜单。
            Destroy(this);
            return;
        }

        Instance = this;

        // AI辅助：Google Gemini（2026-04，同 GameUISfxHub：Canvas 子树与 MarkPersistRoot 讨论）— 以下为人工实现的父链判断与提示语。
        // 挂在 Canvas 子树（含与 Canvas 同物体）上时不能 DDoL 本物体：否则会整块 Canvas 或单个按钮被 MarkPersistRoot 挪到 DontDestroyOnLoad。
        var hostCanvas = GetComponentInParent<Canvas>(true);
        if (hostCanvas != null && hostCanvas.gameObject == gameObject)
            Debug.LogWarning(
                "[PersistentGameBGM] 与 Canvas 挂在同一物体上，已跳过 DontDestroyOnLoad（避免 UI 错位）。"
                + "请将 BGM 挪到与 Canvas 平级的空物体上，再保存场景。");
        else if (hostCanvas != null)
            Debug.LogWarning(
                "[PersistentGameBGM] 挂在 Canvas 下的 UI 上，已跳过 DontDestroyOnLoad（否则会整块进 DontDestroyOnLoad）。"
                + "请将 BGM 挪到与 Canvas 平级的空物体上。");
        else
            VideoPlaybackUtility.MarkPersistRoot(gameObject);

        _audio = GetComponent<AudioSource>();
        _audio.loop = true;
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        _audio.volume = volume;

        var clip = bgmClip;
        if (clip == null && !string.IsNullOrWhiteSpace(resourcesFallbackPath))
            clip = Resources.Load<AudioClip>(resourcesFallbackPath.Trim());

        if (clip != null)
        {
            _audio.clip = clip;
            _audio.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_audio != null)
            _audio.volume = volume;
    }

    public void StopBgm()
    {
        if (_audio == null) return;
        _audio.Stop();
    }

    public void PlayBgm()
    {
        if (_audio == null || _audio.clip == null) return;
        if (!_audio.isPlaying)
            _audio.Play();
    }
}
