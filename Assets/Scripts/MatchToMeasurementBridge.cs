using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 配对游戏与测量仪联动：每次配对后开始移动指针，零度按确认键将对应槽变为完全铆合，并展示榫卯介绍图
/// </summary>
public class MatchToMeasurementBridge : MonoBehaviour
{
    [Header("音效（可选）")]
    [Tooltip("两个榫卯拖对、微微铆合时")]
    [SerializeField] private AudioClip pairMatchedClip;
    [Tooltip("指针在零度区按确认键后彻底铆合时（请拖与上一条不同的音频）")]
    [SerializeField] private AudioClip fullyRivetedClip;
    [Tooltip("可选：配对专用声道；留空则自动创建子物体 TenonSfx_PairMatch")]
    [SerializeField] private AudioSource pairMatchAudioSource;
    [Tooltip("可选：铆合专用声道；留空则自动创建子物体 TenonSfx_FullRivet")]
    [SerializeField] private AudioSource fullyRivetAudioSource;
    [Tooltip("可选：不用于播放；若填写，仅把 Output Audio Mixer Group 拷到两条声道")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("两条声道的 AudioSource.volume")]
    [SerializeField] [Range(0f, 1f)] private float audioSourceLevel = 1f;
    [Tooltip("PlayOneShot 倍率，可大于 1")]
    [SerializeField] [Range(0f, 3f)] private float pairMatchedVolume = 1.75f;
    [SerializeField] [Range(0f, 3f)] private float fullyRivetedVolume = 1.75f;

    [SerializeField] private float introDisplayDelay = 0.5f;
    [SerializeField] private DropZone[] dropZones;
    [SerializeField] private MeasurementBarController measurementBar;
    [SerializeField] private RectTransform glowPositionTarget;
    [SerializeField] private TenonMortiseIntroDisplay introDisplay;
    [Tooltip("尝试在「待彻底铆合」期间再次配对时的提示（可留空，运行时自动创建）")]
    [SerializeField] private TenonMortisePairingBlockToast pairingBlockToast;
    [Tooltip("第一章完成后加载的场景，如 Chapter1Outro（视频+对话后再进第二章）")]
    [SerializeField] private string nextSceneName = "Chapter1Outro";

    /// <summary>微微铆合后、尚未在绿色区按确认键彻底铆合前为 true。</summary>
    public bool IsAwaitingFullRivet =>
        measurementBar != null && measurementBar.IsActive && !measurementBar.IsCompleted;

    public const string PairingBlockedMessage =
        "请先在绿色区域按回车将正在配对的榫卯进行彻底铆合！";

    private DropZone _currentDropZone;

    private void Awake()
    {
        // 早于 Start，避免首帧拖拽时 MatchReactionBridge.bridge 仍为未赋值
        EnsureDropZoneBridgeLinks();
    }

    private void Start()
    {
        EnsureDropZoneBridgeLinks();
        if (measurementBar == null)
            measurementBar = FindObjectOfType<MeasurementBarController>();
        if (introDisplay == null)
            introDisplay = FindObjectOfType<TenonMortiseIntroDisplay>();

        if (measurementBar != null)
            measurementBar.OnSuccess += OnEmbeddingComplete;

        if (introDisplay != null)
            introDisplay.OnContinueClicked += OnIntroContinueClicked;

        EnsurePairingBlockToast();
        EnsureSfxChannels();

#if UNITY_EDITOR
        if (!Application.isBatchMode)
        {
            if (pairMatchedClip == null)
                Debug.LogWarning("[MatchToMeasurementBridge] 未拖 Pair Matched Clip → 配对成功时无音。");
            if (fullyRivetedClip == null)
                Debug.LogWarning("[MatchToMeasurementBridge] 未拖 Fully Riveted Clip → 彻底铆合时无音。");
            if (pairMatchedClip != null && fullyRivetedClip != null && pairMatchedClip == fullyRivetedClip)
                Debug.LogWarning("[MatchToMeasurementBridge] Pair / Fully Riveted 指向同一条 AudioClip，两处会听起来一样；请拖两条不同音效。");
        }
#endif
    }

    void EnsureDropZoneBridgeLinks()
    {
        if (dropZones == null || dropZones.Length == 0)
            dropZones = FindObjectsOfType<DropZone>(true);
        foreach (var dz in dropZones)
        {
            if (dz == null) continue;
            var br = dz.GetComponent<MatchReactionBridge>();
            if (br == null) br = dz.gameObject.AddComponent<MatchReactionBridge>();
            br.bridge = this;
        }
    }

    void EnsurePairingBlockToast()
    {
        if (pairingBlockToast != null) return;
        pairingBlockToast = GetComponent<TenonMortisePairingBlockToast>();
        if (pairingBlockToast == null)
            pairingBlockToast = gameObject.AddComponent<TenonMortisePairingBlockToast>();
    }

    public void NotifyPairingBlocked()
    {
        EnsurePairingBlockToast();
        if (pairingBlockToast != null)
            pairingBlockToast.Show(PairingBlockedMessage);
    }

    void EnsureSfxChannels()
    {
        if (pairMatchAudioSource == null)
            pairMatchAudioSource = CreateOrGetChildSource(transform, "TenonSfx_PairMatch");
        if (fullyRivetAudioSource == null)
            fullyRivetAudioSource = CreateOrGetChildSource(transform, "TenonSfx_FullRivet");

        ConfigureChannel(pairMatchAudioSource);
        ConfigureChannel(fullyRivetAudioSource);

        if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
        {
            pairMatchAudioSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            fullyRivetAudioSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        }
    }

    static AudioSource CreateOrGetChildSource(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(childName);
            go.transform.SetParent(parent, false);
        }
        else
            go = t.gameObject;

        var a = go.GetComponent<AudioSource>();
        if (a == null) a = go.AddComponent<AudioSource>();
        return a;
    }

    static void ConfigureChannel(AudioSource a)
    {
        if (a == null) return;
        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 0f;
        a.pitch = 1f;
        a.mute = false;
    }

    void PlayOn(AudioSource channel, AudioClip clip, float volumeScale)
    {
        if (clip == null || channel == null) return;
        channel.volume = Mathf.Clamp01(audioSourceLevel);
        channel.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
    }

    private void OnDestroy()
    {
        if (measurementBar != null)
            measurementBar.OnSuccess -= OnEmbeddingComplete;
        if (introDisplay != null)
            introDisplay.OnContinueClicked -= OnIntroContinueClicked;
    }

    private void OnIntroContinueClicked()
    {
        if (dropZones == null || dropZones.Length == 0) return;
        foreach (var dz in dropZones)
        {
            if (dz == null || !dz.IsFullyRiveted) return;
        }
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    public void OnPairMatched(DropZone dropZone)
    {
        _currentDropZone = dropZone;
        PlayOn(pairMatchAudioSource, pairMatchedClip, pairMatchedVolume);
        if (measurementBar != null)
        {
            measurementBar.StartMeasurement();
            var target = glowPositionTarget != null ? glowPositionTarget : dropZone.transform as RectTransform;
            measurementBar.SetGlowTarget(target);
        }
    }

    private void OnEmbeddingComplete()
    {
        PlayOn(fullyRivetAudioSource, fullyRivetedClip, fullyRivetedVolume);
        if (_currentDropZone != null)
        {
            _currentDropZone.SetFullyRiveted();
            if (introDisplay != null)
                StartCoroutine(ShowIntroAfterDelay());
        }
    }

    private IEnumerator ShowIntroAfterDelay()
    {
        yield return new WaitForSeconds(introDisplayDelay);
        if (_currentDropZone != null && introDisplay != null)
            introDisplay.Show(_currentDropZone.IntroSprite);
    }
}
