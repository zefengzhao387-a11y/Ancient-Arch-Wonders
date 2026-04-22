using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// 第三章 - 卢沟桥：桥上行走、对话、罗盘游戏（热胀冷缩）
/// </summary>
public class Chapter3BridgeController : MonoBehaviour
{
    [Header("桥面行走")]
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private RectTransform[] platforms;
    [Tooltip("碰到平台边缘自动踏上最大高度(px)，0=禁用，建议 60-80")]
    [SerializeField] private float stepUpHeight = 80f;
    [SerializeField] private RectTransform bridgeCenterZone;
    [SerializeField] private float centerZoneWidth = 150f;
    [Tooltip("静止时的站立图")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float frameInterval = 0.1f;

    [Header("对话")]
    [SerializeField] private GameObject dialogPanel;
    [Tooltip("每段对话对应一张背景 Image，顺序与 bridgeDialogs 一致；数量可少于段数，此时多出的段会复用最后一张背景（避免第三段时全部隐藏）")]
    [SerializeField] private Image[] dialogBoxImages;
    [SerializeField] private Text dialogText;
    [SerializeField] private Button dialogContinueBtn;
    [SerializeField] private string[] bridgeDialogs = new[] { "第一段对话内容", "第二段对话内容", "第三段对话内容" };
    [Tooltip("与 bridgeDialogs 下标一一对应，可留空")]
    [SerializeField] private AudioClip[] bridgeDialogVoiceClips;
    [SerializeField] private AudioSource bridgeDialogVoiceSource;

    [Header("人物切换")]
    [SerializeField] private Image playerImage;
    [SerializeField] private Sprite compassSprite;

    [Header("按E进入")]
    [SerializeField] private GameObject pressEPrompt;

    [Header("玩法介绍")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private Image introImage;
    [SerializeField] private Sprite introSprite;
    [SerializeField] private Button introContinueBtn;

    [Header("罗盘游戏")]
    [SerializeField] private GameObject compassPanel;
    [SerializeField] private RectTransform compassPointer;
    [SerializeField] private Image[] seasonImages;
    [SerializeField] private Sprite[] seasonSprites;
    [Tooltip("春夏秋冬对应视频，指针指向该季节时在右侧播放")]
    [SerializeField] private VideoClip[] seasonVideos;
    [Tooltip("春夏秋冬对应 StreamingAssets 文件名（当对应 seasonVideos 为空时生效）")]
    [SerializeField] private string[] seasonStreamingVideoNames = new[] { "video/chapter3_spring.mp4", "video/chapter3_summer.mp4", "video/chapter3_autumn.mp4", "video/chapter3_winter.mp4" };
    [SerializeField] private RawImage[] seasonVideoDisplays;
    [SerializeField] private VideoPlayer[] seasonVideoPlayers;
    [Tooltip("春夏秋冬范围(度)，每季 X=起始 Y=结束。默认 春0-90 夏270-360 秋90-180 冬180-270")]
    [SerializeField] private Vector2 seasonRangeSpring = new Vector2(0f, 90f);
    [SerializeField] private Vector2 seasonRangeSummer = new Vector2(270f, 360f);
    [SerializeField] private Vector2 seasonRangeAutumn = new Vector2(90f, 180f);
    [SerializeField] private Vector2 seasonRangeWinter = new Vector2(180f, 270f);
    [Tooltip("指针旋转动画时长(秒)，建议 2～3 秒让指针转得更久")]
    [SerializeField] private float pointerRotationDuration = 2.5f;
    [Tooltip("停下前至少多转的圈数（1=至少转一圈再停）")]
    [SerializeField] private int pointerExtraRotations = 1;
    [Tooltip("勾选则反转修复时旋转方向：夏季↑顺时针、冬季↓逆时针")]
    [SerializeField] private bool invertRepairRotation;
    [SerializeField] private Text countdownText;
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int repairsToWin = 3;

    [Header("罗盘音效（可选）")]
    [Tooltip("点击罗盘后指针自动旋转：转起来时循环播此条，到角度后停止（见下一条落定音）")]
    [SerializeField] private AudioClip compassAutoSpinLoopClip;
    [Tooltip("留空则手动转也用上面那条；否则修复模式按住 ↑/↓ 时用此条循环")]
    [SerializeField] private AudioClip pointerManualSpinLoopClip;
    [Tooltip("自动旋转停止后可选播一次（与循环为不同 Clip 时常用）")]
    [SerializeField] private AudioClip compassAutoSpinSettleClip;
    [Tooltip("指针对准目标季节、修复成功瞬间（单次）")]
    [SerializeField] private AudioClip pointerTargetMatchClip;
    [Tooltip("指针旋转循环音（自动转罗盘 + 修复模式按住 ↑/↓ 手动转）")]
    [SerializeField] [Range(0f, 3f)] private float compassSpinLoopVolume = 2f;
    [Tooltip("自动旋转结束、指针落定瞬间（compassAutoSpinSettleClip）")]
    [SerializeField] [Range(0f, 3f)] private float compassAutoSpinSettleVolume = 2f;
    [Tooltip("其它罗盘单次音（如指针对准季节的「成功」音）")]
    [SerializeField] [Range(0f, 3f)] private float compassSfxVolume = 1f;
    [Header("键盘交互音效（可选，与罗盘单次音共用 CompassSfx 声源）")]
    [Tooltip("按 E 进介绍、桥面跳跃 W/空格、修复模式 ↑/↓ 首次按下时播放")]
    [SerializeField] private AudioClip keyboardInteractClip;
    [SerializeField] [Range(0f, 3f)] private float keyboardSfxVolume = 1f;

    [Header("弹窗")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Text popupText;
    [SerializeField] private Button popupContinueBtn;

    [Header("知识补充")]
    [SerializeField] private GameObject knowledgePanel;
    [SerializeField] private Image knowledgeImage;
    [SerializeField] private Sprite[] knowledgeSprites;
    [SerializeField] private Button knowledgeNextBtn;

    [Header("罗盘通关后")]
    [SerializeField] private GameObject postWinVideoRoot;
    [SerializeField] private VideoPlayer postWinVideoPlayer;
    [SerializeField] private RawImage postWinVideoDisplay;
    [Tooltip("通关后播放的短片；也可仅用 StreamingAssets 文件名")]
    [SerializeField] private VideoClip postWinVideoClip;
    [Tooltip("若未拖 clip，则尝试 StreamingAssets 下此文件名，例如 video/chapter3_post_compass.mp4")]
    [SerializeField] private string postWinStreamingVideoName = "video/chapter3_post_compass.mp4";
    [SerializeField] private float postWinFadeToBlackDuration = 0.85f;
    [SerializeField] private float postWinVideoRevealDuration = 1f;
    [SerializeField] private GameObject postWinDialogPanel;
    [SerializeField] private Image postWinDialogImage;
    [SerializeField] private Sprite postWinDialogSprite;
    [SerializeField] private Button postWinDialogContinueBtn;
    [Tooltip("通关对话整块（图+按钮）渐显/渐隐时长")]
    [SerializeField] private float postWinDialogFadeDuration = 0.28f;
    [Tooltip("通关后视频上对话图对应的配音，可选")]
    [SerializeField] private AudioClip postWinDialogVoiceClip;
    [Tooltip("留空则使用 bridgeDialogVoiceSource 播放")]
    [SerializeField] private AudioSource postWinDialogVoiceSource;

    [Header("渐变")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private string nextSceneName;

    private enum Phase { Walking, Dialog, WaitE, Intro, Compass, Popup, PostWin, Knowledge }
    private Phase _phase;
    private int _dialogIndex;
    private int _knowledgeIndex;
    private float _gameTime;
    private int _repairCount;
    private bool _inRepairMode;
    private int _repairTargetSeason;
    private bool _compassGameOver;
    private float _pointerAngle;
    private bool _pointerAnimating;
    private float _pointerTargetAngle;
    private int _playingSeasonVideo = -1;
    private Coroutine _prepareVideoCoroutine;
    private CompassSeasonVideoGlow[] _seasonGlows;
    private Transform _parent;
    private Image _playerImg;
    private float _frameTimer;
    private int _frameIndex;
    private Vector2 _spawnPos;
    private Vector2 _velocity;
    private float _gravity = 200f, _moveSpeed = 200f, _jumpForce = 160f, _groundTol = 10f;
    private float _lastGroundedTime;
    private readonly Vector3[] _corners = new Vector3[4];
    private bool _postWinHidBridgeUi;
    private bool _knowledgeNextClicked;
    private AudioSource _compassSfx;
    private AudioSource _compassSpinLoop;

    void Start()
    {
        _parent = playerRect != null ? playerRect.parent : null;
        _spawnPos = playerRect != null ? playerRect.anchoredPosition : Vector2.zero;
        _playerImg = playerImage != null ? playerImage : (playerRect != null ? FootShadow.GetCharacterImage(playerRect) : null);
        if (idleSprite != null && _playerImg != null) _playerImg.sprite = idleSprite;
        if (fadeOverlay == null) fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (bridgeDialogVoiceSource == null) bridgeDialogVoiceSource = GetComponent<AudioSource>();
        if (bridgeDialogVoiceSource == null) bridgeDialogVoiceSource = gameObject.AddComponent<AudioSource>();
        bridgeDialogVoiceSource.playOnAwake = false;

        EnsureCompassSfxSource();

        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
        if (compassPanel != null) compassPanel.SetActive(false);
        if (popupPanel != null) popupPanel.SetActive(false);
        if (knowledgePanel != null) knowledgePanel.SetActive(false);
        if (postWinVideoRoot != null) postWinVideoRoot.SetActive(false);
        if (postWinDialogPanel != null) postWinDialogPanel.SetActive(false);
        if (postWinDialogImage != null) postWinDialogImage.enabled = false;
        if (fadeOverlay != null) { fadeOverlay.transform.SetAsLastSibling(); fadeOverlay.color = new Color(0f, 0f, 0f, 1f); }

        if (dialogText != null)
        {
            SubtitleStyleUtility.RemoveSubtitleBackdrop(dialogText);
            SubtitleStyleUtility.ApplySubtitleFont(dialogText);
        }

        _phase = Phase.Walking;
        StartCoroutine(StartFadeIn());
    }

    IEnumerator StartFadeIn()
    {
        yield return new WaitForSeconds(0.2f);
        yield return Fade(1f, 0f, 1f);
    }

    void Update()
    {
        if (_phase == Phase.Walking) UpdateWalking();
        else if (_phase == Phase.WaitE && Input.GetKeyDown(KeyCode.E))
        {
            PlayKeyboardSfx();
            if (pressEPrompt != null) pressEPrompt.SetActive(false);
            ShowIntro();
        }
        else if (_phase == Phase.Compass && !_inRepairMode) UpdateCompassGame();
        else if (_phase == Phase.Compass && _inRepairMode) UpdateRepairMode();
    }

    void UpdateWalking()
    {
        float h = Input.GetAxisRaw("Horizontal");
        bool grounded = IsGrounded();
        if (grounded && _velocity.y <= 0) _lastGroundedTime = Time.time;
        bool standing = grounded && _velocity.y <= 0;
        if (standing)
        {
            _velocity.y = 0;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                _velocity.y = _jumpForce;
                PlayKeyboardSfx();
            }
        }
        else _velocity.y -= _gravity * Time.deltaTime;
        _velocity.x = h * _moveSpeed;
        if (h != 0) { var s = playerRect.localScale; s.x = h > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x); playerRect.localScale = s; UpdateWalkFrame(); }
        else if (idleSprite != null && _playerImg != null) _playerImg.sprite = idleSprite;
        var pos = playerRect.anchoredPosition;
        pos += _velocity * Time.deltaTime;
        playerRect.anchoredPosition = pos;
        ResolveCollisions();
        if (InCenterZone()) StartDialogs();
    }

    void UpdateWalkFrame()
    {
        if (_playerImg == null || walkFrames == null || walkFrames.Length == 0) return;
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= frameInterval) { _frameTimer = 0; _frameIndex = (_frameIndex + 1) % walkFrames.Length; if (walkFrames[_frameIndex] != null) _playerImg.sprite = walkFrames[_frameIndex]; }
    }

    bool IsGrounded()
    {
        GetLocalBounds(playerRect, out float pl, out float pr, out float pb, out float pt);
        float cx = (pl + pr) * 0.5f;
        if (platforms == null) return false;
        foreach (var p in platforms)
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            GetLocalBounds(p, out float tl, out float tr, out float tb, out float tt);
            if (pr <= tl || pl >= tr) continue;
            float sy = tt;
            var hm = p.GetComponent<PlatformHeightMap>();
            if (hm != null && hm.HasData && _parent != null && hm.TryGetSurfaceY(p, _parent, cx, out float s)) sy = s;
            if (pb >= sy - _groundTol && pb <= sy + _groundTol) return true;
        }
        return false;
    }

    void ResolveCollisions()
    {
        if (_parent == null || playerRect == null) return;
        GetLocalBounds(playerRect, out float pl, out float pr, out float pb, out float pt);
        float cx = (pl + pr) * 0.5f;
        float? snapY = null;
        float? pushLeft = null;
        float? pushRight = null;
        foreach (var p in platforms ?? new RectTransform[0])
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            GetLocalBounds(p, out float tl, out float tr, out float tb, out float tt);
            float sy = tt;
            var hm = p.GetComponent<PlatformHeightMap>();
            if (hm != null && hm.HasData && _parent != null && hm.TryGetSurfaceY(p, _parent, cx, out float s)) sy = s;
            bool overlapX = pr > tl && pl < tr;
            bool overlapY = pb < tt && pt > tb;
            bool standing = pb >= sy - _groundTol && pb <= sy + _groundTol && overlapX;
            if (standing && _velocity.y <= 0 && (!snapY.HasValue || sy > snapY.Value)) snapY = sy;
            if (overlapX && overlapY && !standing)
            {
                float platformLeft = tl, platformRight = tr;
                if (hm != null && hm.HasEdgeData && _parent != null)
                {
                    float charCenterY = (pb + pt) * 0.5f;
                    bool hasLeft = hm.TryGetLeftEdgeX(p, _parent, charCenterY, out float leftEdge);
                    bool hasRight = hm.TryGetRightEdgeX(p, _parent, charCenterY, out float rightEdge);
                    if (!hasLeft || !hasRight || leftEdge >= rightEdge - 2f) continue;
                    platformLeft = leftEdge;
                    platformRight = rightEdge;
                }
                if (_velocity.x > 0 && pr > platformLeft && pl < platformLeft)
                {
                    if (stepUpHeight > 0 && TryGetSurfaceY(p, Mathf.Clamp(platformLeft + 10f, platformLeft, platformRight - 5f), out float edgeSy) && edgeSy - pb > 0 && edgeSy - pb <= stepUpHeight)
                        snapY = (!snapY.HasValue || edgeSy > snapY.Value) ? edgeSy : snapY;
                    else
                        pushLeft = pushLeft.HasValue ? Mathf.Min(pushLeft.Value, platformLeft) : platformLeft;
                }
                if (_velocity.x < 0 && pl < platformRight && pr > platformRight)
                {
                    if (stepUpHeight > 0 && TryGetSurfaceY(p, Mathf.Clamp(platformRight - 10f, platformLeft + 5f, platformRight), out float edgeSy2) && edgeSy2 - pb > 0 && edgeSy2 - pb <= stepUpHeight)
                        snapY = (!snapY.HasValue || edgeSy2 > snapY.Value) ? edgeSy2 : snapY;
                    else
                        pushRight = pushRight.HasValue ? Mathf.Max(pushRight.Value, platformRight) : platformRight;
                }
            }
        }
        if (snapY.HasValue) { var pos = playerRect.anchoredPosition; GetLocalBounds(playerRect, out _, out _, out float curPb, out _); pos.y += snapY.Value - curPb; _velocity.y = 0; playerRect.anchoredPosition = pos; }
        if (pushLeft.HasValue && pl < pushLeft.Value) { var pos = playerRect.anchoredPosition; GetLocalBounds(playerRect, out float curPl, out _, out _, out _); pos.x += pushLeft.Value - curPl; playerRect.anchoredPosition = pos; }
        if (pushRight.HasValue && pr > pushRight.Value) { var pos = playerRect.anchoredPosition; GetLocalBounds(playerRect, out _, out float curPr, out _, out _); pos.x += pushRight.Value - curPr; playerRect.anchoredPosition = pos; }
    }

    bool TryGetSurfaceY(RectTransform p, float x, out float sy)
    {
        sy = 0;
        var hm = p.GetComponent<PlatformHeightMap>();
        if (hm != null && hm.HasData && _parent != null)
        {
            if (hm.TryGetSurfaceY(p, _parent, x, out sy)) return true;
        }
        GetLocalBounds(p, out float _, out float _, out float _, out float top);
        sy = top;
        return true;
    }

    void GetLocalBounds(RectTransform rt, out float l, out float r, out float b, out float t)
    {
        rt.GetWorldCorners(_corners);
        var p0 = _parent != null ? _parent.InverseTransformPoint(_corners[0]) : _corners[0];
        var p2 = _parent != null ? _parent.InverseTransformPoint(_corners[2]) : _corners[2];
        l = p0.x; b = p0.y; r = p2.x; t = p2.y;
    }

    bool InCenterZone()
    {
        if (bridgeCenterZone == null) return false;
        GetLocalBounds(playerRect, out float pl, out float pr, out _, out _);
        GetLocalBounds(bridgeCenterZone, out float cl, out float cr, out _, out _);
        float cx = (pl + pr) * 0.5f;
        return cx >= cl - centerZoneWidth && cx <= cr + centerZoneWidth;
    }

    void StartDialogs()
    {
        _phase = Phase.Dialog;
        _dialogIndex = 0;
        ShowDialog();
    }

    void ShowDialog()
    {
        if (dialogPanel == null) { OnDialogComplete(); return; }
        dialogPanel.SetActive(true);
        if (dialogText != null && bridgeDialogs != null && _dialogIndex < bridgeDialogs.Length)
            dialogText.text = bridgeDialogs[_dialogIndex];
        if (dialogBoxImages != null && dialogBoxImages.Length > 0)
        {
            int showBg = Mathf.Clamp(_dialogIndex, 0, dialogBoxImages.Length - 1);
            for (int i = 0; i < dialogBoxImages.Length; i++)
            {
                if (dialogBoxImages[i] != null)
                    dialogBoxImages[i].gameObject.SetActive(i == showBg);
            }
        }
        if (dialogContinueBtn != null) dialogContinueBtn.onClick.RemoveAllListeners();
        if (dialogContinueBtn != null) dialogContinueBtn.onClick.AddListener(OnDialogContinue);
        PlayBridgeDialogVoice(_dialogIndex);
    }

    void PlayBridgeDialogVoice(int index)
    {
        if (bridgeDialogVoiceSource == null) return;
        bridgeDialogVoiceSource.Stop();
        if (bridgeDialogVoiceClips == null || index < 0 || index >= bridgeDialogVoiceClips.Length)
            return;
        var clip = bridgeDialogVoiceClips[index];
        if (clip == null) return;
        bridgeDialogVoiceSource.clip = clip;
        bridgeDialogVoiceSource.Play();
    }

    void StopBridgeDialogVoice()
    {
        if (bridgeDialogVoiceSource == null) return;
        bridgeDialogVoiceSource.Stop();
        bridgeDialogVoiceSource.clip = null;
    }

    AudioSource ResolvePostWinDialogVoiceSource()
    {
        if (postWinDialogVoiceSource != null) return postWinDialogVoiceSource;
        return bridgeDialogVoiceSource;
    }

    void PlayPostWinDialogVoice()
    {
        var src = ResolvePostWinDialogVoiceSource();
        if (src == null) return;
        src.Stop();
        if (postWinDialogVoiceClip == null) return;
        src.clip = postWinDialogVoiceClip;
        src.Play();
    }

    void StopPostWinDialogVoice()
    {
        var src = ResolvePostWinDialogVoiceSource();
        if (src == null || postWinDialogVoiceClip == null) return;
        if (src.clip != postWinDialogVoiceClip) return;
        src.Stop();
        src.clip = null;
    }

    void OnDialogContinue()
    {
        _dialogIndex++;
        if (_dialogIndex >= (bridgeDialogs?.Length ?? 0)) OnDialogComplete();
        else ShowDialog();
    }

    void OnDialogComplete()
    {
        StopBridgeDialogVoice();
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (playerImage != null && compassSprite != null) playerImage.sprite = compassSprite;
        if (pressEPrompt != null) pressEPrompt.SetActive(true);
        _phase = Phase.WaitE;
    }

    void ShowIntro()
    {
        _phase = Phase.Intro;
        if (introPanel != null) introPanel.SetActive(true);
        if (introImage != null && introSprite != null) introImage.sprite = introSprite;
        if (introContinueBtn != null) { introContinueBtn.onClick.RemoveAllListeners(); introContinueBtn.onClick.AddListener(OnIntroContinue); }
    }

    void OnIntroContinue()
    {
        if (introPanel != null) introPanel.SetActive(false);
        StartCompassGame();
    }

    void StartCompassGame()
    {
        StopSeasonVideo();
        _phase = Phase.Compass;
        _compassGameOver = false;
        if (compassPanel != null) compassPanel.SetActive(true);
        _gameTime = gameDuration;
        _repairCount = 0;
        _inRepairMode = false;
        _pointerAngle = 0;
        _pointerAnimating = false;
        StopCompassSpinLoop();
        if (compassPointer != null) compassPointer.localEulerAngles = new Vector3(0, 0, 0);
        for (int i = 0; i < 4 && i < (seasonImages?.Length ?? 0) && i < (seasonSprites?.Length ?? 0); i++)
            if (seasonImages[i] != null && seasonSprites[i] != null) seasonImages[i].sprite = seasonSprites[i];
        EnsureCompassClickable();
        EnsureSeasonVideoRefs();
        EnsureSeasonVideoGlows();
    }

    void EnsureSeasonVideoGlows()
    {
        if (compassPanel == null) return;
        var area = compassPanel.transform.Find("SeasonArea");
        if (area == null) return;
        if (_seasonGlows == null || _seasonGlows.Length != 4)
            _seasonGlows = new CompassSeasonVideoGlow[4];
        for (int i = 0; i < 4; i++)
        {
            var cell = area.Find("Season" + i);
            if (cell == null)
            {
                _seasonGlows[i] = null;
                continue;
            }
            var g = cell.GetComponent<CompassSeasonVideoGlow>();
            if (g == null) g = cell.gameObject.AddComponent<CompassSeasonVideoGlow>();
            _seasonGlows[i] = g;
        }
    }

    void RefreshSeasonGlows(int activeRegion)
    {
        if (_seasonGlows == null) EnsureSeasonVideoGlows();
        if (_seasonGlows == null) return;
        for (int i = 0; i < _seasonGlows.Length; i++)
        {
            if (_seasonGlows[i] == null) continue;
            _seasonGlows[i].SetPulsing(activeRegion == i);
        }
    }

    void EnsureSeasonVideoRefs()
    {
        if (seasonVideos == null || seasonVideos.Length < 4)
            Debug.LogWarning("Chapter3BridgeController：请在 Inspector 的 Season Videos 中拖入 4 个视频（春、夏、秋、冬）");
        if (seasonVideoDisplays != null && seasonVideoDisplays.Length >= 4 && seasonVideoPlayers != null && seasonVideoPlayers.Length >= 4) return;
        if (compassPanel == null) return;
        var area = compassPanel.transform.Find("SeasonArea");
        if (area == null) return;
        var dispList = new System.Collections.Generic.List<RawImage>();
        var vpList = new System.Collections.Generic.List<VideoPlayer>();
        for (int i = 0; i < 4; i++)
        {
            var t = area.Find("Season" + i)?.Find("VideoDisplay");
            if (t != null)
            {
                var raw = t.GetComponent<RawImage>();
                var vp = t.GetComponent<VideoPlayer>();
                if (raw != null && vp != null) { dispList.Add(raw); vpList.Add(vp); }
            }
        }
        if (dispList.Count >= 4 && vpList.Count >= 4)
        {
            seasonVideoDisplays = dispList.ToArray();
            seasonVideoPlayers = vpList.ToArray();
        }
    }

    void EnsureCompassSfxSource()
    {
        if (compassAutoSpinLoopClip == null && pointerManualSpinLoopClip == null &&
            compassAutoSpinSettleClip == null && pointerTargetMatchClip == null &&
            keyboardInteractClip == null)
            return;
        if (_compassSfx == null)
        {
            var go = new GameObject("CompassSfx");
            go.transform.SetParent(transform, false);
            _compassSfx = go.AddComponent<AudioSource>();
            _compassSfx.playOnAwake = false;
            _compassSfx.loop = false;
            _compassSfx.spatialBlend = 0f;
        }
        if (_compassSpinLoop == null &&
            (compassAutoSpinLoopClip != null || pointerManualSpinLoopClip != null))
        {
            var go2 = new GameObject("CompassSpinLoop");
            go2.transform.SetParent(transform, false);
            _compassSpinLoop = go2.AddComponent<AudioSource>();
            _compassSpinLoop.playOnAwake = false;
            _compassSpinLoop.loop = true;
            _compassSpinLoop.spatialBlend = 0f;
        }
    }

    AudioClip ResolveManualSpinLoopClip() =>
        pointerManualSpinLoopClip != null ? pointerManualSpinLoopClip : compassAutoSpinLoopClip;

    void StartCompassSpinLoop(bool manual)
    {
        var clip = manual ? ResolveManualSpinLoopClip() : compassAutoSpinLoopClip;
        if (clip == null || _compassSpinLoop == null) return;
        if (_compassSpinLoop.isPlaying && _compassSpinLoop.clip == clip) return;
        _compassSpinLoop.Stop();
        _compassSpinLoop.clip = clip;
        _compassSpinLoop.volume = Mathf.Clamp(compassSpinLoopVolume, 0f, 3f);
        _compassSpinLoop.Play();
    }

    void StopCompassSpinLoop()
    {
        if (_compassSpinLoop == null) return;
        _compassSpinLoop.Stop();
        _compassSpinLoop.clip = null;
    }

    void PlayCompassSfx(AudioClip clip, float volumeScale)
    {
        if (clip == null || _compassSfx == null) return;
        _compassSfx.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
    }

    void PlayKeyboardSfx()
    {
        if (keyboardInteractClip == null || _compassSfx == null) return;
        _compassSfx.PlayOneShot(keyboardInteractClip, Mathf.Max(0f, keyboardSfxVolume));
    }

    void EnsureCompassClickable()
    {
        if (compassPointer == null) return;
        var ptrBtn = compassPointer.GetComponent<UnityEngine.UI.Button>();
        if (ptrBtn != null) { ptrBtn.onClick.RemoveAllListeners(); ptrBtn.onClick.AddListener(OnPointerClick); }
        var parent = compassPointer.parent;
        if (parent != null)
        {
            var parentBtn = parent.GetComponent<UnityEngine.UI.Button>();
            if (parentBtn != null && parentBtn != ptrBtn) { parentBtn.onClick.RemoveAllListeners(); parentBtn.onClick.AddListener(OnPointerClick); }
        }
    }

    void UpdateCompassGame()
    {
        if (_compassGameOver) return;
        _gameTime -= Time.deltaTime;
        if (countdownText != null) countdownText.text = Mathf.Max(0, Mathf.CeilToInt(_gameTime)).ToString();
        if (_gameTime <= 0) { _compassGameOver = true; ShowPopup("时间到！游戏结束。", () => { StopSeasonVideo(); if (popupPanel != null) popupPanel.SetActive(false); StartCompassGame(); }); }
    }

    void UpdateRepairMode()
    {
        float step = 60f * Time.deltaTime;
        if (invertRepairRotation) step = -step;
        bool manualTurn = (_repairTargetSeason == 0 && Input.GetKey(KeyCode.UpArrow)) ||
                           (_repairTargetSeason == 2 && Input.GetKey(KeyCode.DownArrow));
        if (_repairTargetSeason == 0 && Input.GetKey(KeyCode.UpArrow)) _pointerAngle -= step;
        if (_repairTargetSeason == 2 && Input.GetKey(KeyCode.DownArrow)) _pointerAngle += step;
        _pointerAngle = Mathf.Repeat(_pointerAngle, 360f);

        if (_repairTargetSeason == 0 && Input.GetKeyDown(KeyCode.UpArrow)) PlayKeyboardSfx();
        if (_repairTargetSeason == 2 && Input.GetKeyDown(KeyCode.DownArrow)) PlayKeyboardSfx();

        if (manualTurn)
            StartCompassSpinLoop(true);
        else
            StopCompassSpinLoop();

        if (compassPointer != null) compassPointer.localEulerAngles = new Vector3(0, 0, _pointerAngle);
        int region = GetPointerRegion();
        if (_playingSeasonVideo != region) PlaySeasonVideo(region);
        if (region == _repairTargetSeason)
        {
            StopCompassSpinLoop();
            PlayCompassSfx(pointerTargetMatchClip, compassSfxVolume);
            _inRepairMode = false;
            _repairCount++;
            ShowPopup("修复成功：安全！伸缩缝已恢复正常范围！", OnRepairSuccessContinue);
        }
    }

    int GetPointerRegion()
    {
        float a = Mathf.Repeat(_pointerAngle, 360f);
        if (InAngleRange(a, seasonRangeSpring.x, seasonRangeSpring.y)) return 0;
        if (InAngleRange(a, seasonRangeSummer.x, seasonRangeSummer.y)) return 1;
        if (InAngleRange(a, seasonRangeAutumn.x, seasonRangeAutumn.y)) return 2;
        if (InAngleRange(a, seasonRangeWinter.x, seasonRangeWinter.y)) return 3;
        return 0;
    }

    bool InAngleRange(float a, float from, float to)
    {
        a = Mathf.Repeat(a, 360f);
        from = Mathf.Repeat(from, 360f);
        to = Mathf.Repeat(to, 360f);
        if (from <= to) return a >= from && a < to;
        return a >= from || a < to;
    }

    public void OnPointerClick()
    {
        if (_phase != Phase.Compass || _inRepairMode || _pointerAnimating) return;
        float targetAngle = Random.Range(0, 360f);
        StartCoroutine(AnimatePointerThenCheck(targetAngle));
    }

    IEnumerator AnimatePointerThenCheck(float targetAngle)
    {
        _pointerAnimating = true;
        StartCompassSpinLoop(false);
        float startAngle = _pointerAngle;
        float startTime = Time.time;
        float duration = Mathf.Max(0.01f, pointerRotationDuration);
        int extra = Mathf.Max(0, pointerExtraRotations);
        float deltaToTarget = Mathf.DeltaAngle(startAngle, targetAngle);
        float totalRotation = 360f * extra + deltaToTarget;
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            t = t * t * (3f - 2f * t);
            _pointerAngle = Mathf.Repeat(startAngle + totalRotation * t, 360f);
            if (compassPointer != null) compassPointer.localEulerAngles = new Vector3(0, 0, _pointerAngle);
            yield return null;
        }
        _pointerAngle = Mathf.Repeat(targetAngle, 360f);
        if (compassPointer != null) compassPointer.localEulerAngles = new Vector3(0, 0, _pointerAngle);
        StopCompassSpinLoop();
        PlayCompassSfx(compassAutoSpinSettleClip, compassAutoSpinSettleVolume);
        _pointerAnimating = false;
        int region = GetPointerRegion();
        PlaySeasonVideo(region);
        yield return new WaitForSeconds(0.15f);
        if (region == 0) ShowPopup("春季：温度适宜，伸缩缝正常。", OnSpringAutumnContinue);
        else if (region == 2) ShowPopup("秋季：温度适宜，伸缩缝正常。", OnSpringAutumnContinue);
        else if (region == 1) { _inRepairMode = true; _repairTargetSeason = 0; ShowPopup("夏季危险：高温膨胀！伸缩缝过窄！按 ↑ 上键 加宽！", OnSpringAutumnContinue); }
        else if (region == 3) { _inRepairMode = true; _repairTargetSeason = 2; ShowPopup("冬季危险：严寒收缩！伸缩缝过宽！按 ↓ 下键 缩窄！", OnSpringAutumnContinue); }
    }

    void PlaySeasonVideo(int region)
    {
        if (_prepareVideoCoroutine != null) { StopCoroutine(_prepareVideoCoroutine); _prepareVideoCoroutine = null; }
        StopSeasonVideo();
        if (region < 0 || region > 3)
            return;
        if (seasonVideoPlayers == null || seasonVideoDisplays == null || region >= seasonVideoPlayers.Length || region >= seasonVideoDisplays.Length ||
            seasonVideoPlayers[region] == null || seasonVideoDisplays[region] == null) return;

        var vp = seasonVideoPlayers[region];
        var disp = seasonVideoDisplays[region];
        if (!TrySetSeasonVideoSource(region, vp, out VideoClip expectedClip, out string expectedUrl))
        {
            Debug.LogWarning($"Chapter3BridgeController：季节{region}(春0夏1秋2冬3)未配置视频源；请在 Season Videos 或 Season Streaming Video Names 中填写。");
            return;
        }

        disp.gameObject.SetActive(true);
        if (!vp.enabled) vp.enabled = true;
        vp.playOnAwake = false;
        vp.isLooping = true;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.skipOnDrop = true;
        vp.waitForFirstFrame = true;
        vp.errorReceived -= OnSeasonVideoError;
        vp.errorReceived += OnSeasonVideoError;
        vp.Prepare();
        _prepareVideoCoroutine = StartCoroutine(PrepareAndPlaySeasonVideo(vp, disp, region, expectedClip, expectedUrl));
    }

    IEnumerator PrepareAndPlaySeasonVideo(VideoPlayer vp, RawImage disp, int region, VideoClip expectedClip, string expectedUrl)
    {
        if (vp == null || disp == null) { _prepareVideoCoroutine = null; yield break; }
        float t = 0;
        while (!vp.isPrepared && t < 5f) { t += Time.deltaTime; yield return null; }
        if (expectedClip != null)
        {
            if (vp.clip != expectedClip) { _prepareVideoCoroutine = null; yield break; }
        }
        else if (!string.IsNullOrEmpty(expectedUrl))
        {
            if (vp.source != VideoSource.Url || vp.url != expectedUrl) { _prepareVideoCoroutine = null; yield break; }
        }
        if (vp.isPrepared)
        {
            if (vp.targetTexture == null)
            {
                int w = Mathf.Max(256, (int)vp.width);
                int h = Mathf.Max(256, (int)vp.height);
                if (w <= 0 || h <= 0) { w = 512; h = 512; }
                vp.targetTexture = new RenderTexture(w, h, 0);
            }
            disp.texture = vp.targetTexture;
            disp.gameObject.SetActive(true);
            if (seasonImages != null && region < seasonImages.Length && seasonImages[region] != null)
                seasonImages[region].enabled = false;
            vp.Play();
            _playingSeasonVideo = region;
            RefreshSeasonGlows(region);
        }
        else
        {
            FallbackToSeasonSprite(region);
        }
        _prepareVideoCoroutine = null;
    }

    bool TrySetSeasonVideoSource(int region, VideoPlayer vp, out VideoClip expectedClip, out string expectedUrl)
    {
        expectedClip = null;
        expectedUrl = "";
        if (vp == null) return false;

        if (seasonVideos != null && region >= 0 && region < seasonVideos.Length && seasonVideos[region] != null)
        {
            expectedClip = seasonVideos[region];
            vp.source = VideoSource.VideoClip;
            vp.clip = expectedClip;
            vp.url = "";
            return true;
        }

        string streamingName = "";
        if (seasonStreamingVideoNames != null && region >= 0 && region < seasonStreamingVideoNames.Length)
            streamingName = seasonStreamingVideoNames[region];
        if (!VideoPlaybackUtility.HasStreamingMediaSource(streamingName))
            return false;

        expectedUrl = VideoPlaybackUtility.ResolveStreamingMediaUrl(streamingName);
        vp.source = VideoSource.Url;
        vp.url = expectedUrl;
        vp.clip = null;
        return true;
    }

    void OnSeasonVideoError(VideoPlayer vp, string msg)
    {
        vp.errorReceived -= OnSeasonVideoError;
        if (_playingSeasonVideo >= 0) FallbackToSeasonSprite(_playingSeasonVideo);
        _playingSeasonVideo = -1;
    }

    void FallbackToSeasonSprite(int region)
    {
        if (seasonVideoDisplays != null && region < seasonVideoDisplays.Length && seasonVideoDisplays[region] != null)
            seasonVideoDisplays[region].gameObject.SetActive(false);
        if (seasonImages != null && region < seasonImages.Length && seasonImages[region] != null)
            seasonImages[region].enabled = true;
        RefreshSeasonGlows(region);
    }

    void StopSeasonVideo()
    {
        if (_prepareVideoCoroutine != null) { StopCoroutine(_prepareVideoCoroutine); _prepareVideoCoroutine = null; }
        if (seasonVideoPlayers != null)
            for (int i = 0; i < seasonVideoPlayers.Length; i++)
                if (seasonVideoPlayers[i] != null)
                {
                    seasonVideoPlayers[i].errorReceived -= OnSeasonVideoError;
                    seasonVideoPlayers[i].Stop();
                }
        if (seasonVideoDisplays != null)
            for (int i = 0; i < seasonVideoDisplays.Length; i++)
                if (seasonVideoDisplays[i] != null) seasonVideoDisplays[i].gameObject.SetActive(false);
        if (_playingSeasonVideo >= 0 && seasonImages != null && _playingSeasonVideo < seasonImages.Length && seasonImages[_playingSeasonVideo] != null)
            seasonImages[_playingSeasonVideo].enabled = true;
        _playingSeasonVideo = -1;
        RefreshSeasonGlows(-1);
    }

    void OnSpringAutumnContinue() { if (popupPanel != null) popupPanel.SetActive(false); }

    void OnRepairSuccessContinue()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (_repairCount >= repairsToWin) { ShowPopup("通关恭喜：你成功守护卢沟桥！掌握古桥与现代桥梁的热胀冷缩知识！", OnWinContinue); }
    }

    void OnWinContinue()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        StopSeasonVideo();
        // 罗盘保持最后一帧，直到 PostWinSequence 里黑场渐隐完成后再关
        StartCoroutine(PostWinSequence());
    }

    private bool HasPostWinVideoSource()
    {
        if (postWinVideoPlayer == null || postWinVideoDisplay == null) return false;
        if (postWinVideoClip != null) return true;
        return VideoPlaybackUtility.HasStreamingMediaSource(postWinStreamingVideoName);
    }

    private IEnumerator PostWinSequence()
    {
        _phase = Phase.PostWin;

        if (HasPostWinVideoSource())
        {
            // 先只叠黑场渐隐：罗盘（及桥下）仍停在最后一帧；全黑后再关罗盘并接 PostWin 视频层，避免通关黑 Panel 盖住罗盘
            if (postWinVideoDisplay != null)
            {
                postWinVideoDisplay.color = Color.black;
                postWinVideoDisplay.gameObject.SetActive(false);
            }
            if (postWinVideoRoot != null && postWinVideoRoot.activeSelf)
                postWinVideoRoot.SetActive(false);
            if (fadeOverlay != null)
            {
                fadeOverlay.transform.SetAsLastSibling();
                SetFadeOverlayAlpha(0f);
            }
            yield return Fade(0f, 1f, postWinFadeToBlackDuration);

            HideBridgeUiForPostWinVideo();
            if (compassPanel != null) compassPanel.SetActive(false);
            if (postWinVideoRoot != null)
            {
                postWinVideoRoot.SetActive(true);
                postWinVideoRoot.transform.SetAsLastSibling();
                HidePostWinDialogUiDuringVideo();
            }
            if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();

            postWinVideoPlayer.playOnAwake = false;
            postWinVideoPlayer.isLooping = false;
            postWinVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            postWinVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            _postWinVideoFailed = false;
            VideoPlayer.ErrorEventHandler onPostWinErr = (v, msg) =>
            {
                _postWinVideoFailed = true;
                VideoPlaybackUtility.LogVideoError(v, msg);
            };
            postWinVideoPlayer.errorReceived += onPostWinErr;

            if (postWinVideoClip != null)
            {
                postWinVideoPlayer.source = VideoSource.VideoClip;
                postWinVideoPlayer.clip = postWinVideoClip;
                postWinVideoPlayer.url = "";
            }
            else
            {
                postWinVideoPlayer.source = VideoSource.Url;
                postWinVideoPlayer.url = VideoPlaybackUtility.ResolveStreamingMediaUrl(postWinStreamingVideoName);
                postWinVideoPlayer.clip = null;
            }

            VideoPlaybackUtility.ApplyStandardCompat(postWinVideoPlayer);
            postWinVideoPlayer.Prepare();
            float prep = 0f;
            while (!postWinVideoPlayer.isPrepared && !_postWinVideoFailed && prep < 15f)
            {
                prep += Time.deltaTime;
                yield return null;
            }
            if (!postWinVideoPlayer.isPrepared || _postWinVideoFailed)
            {
                postWinVideoPlayer.errorReceived -= onPostWinErr;
                if (postWinVideoRoot != null) postWinVideoRoot.SetActive(false);
                yield return Fade(1f, 0f, postWinFadeToBlackDuration);
                RestoreBridgeUiAfterPostWinVideo();
            }
            else
            {
                int w = (int)postWinVideoPlayer.width, h = (int)postWinVideoPlayer.height;
                if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
                ReleasePostWinRenderTexture();
                _postWinRt = VideoPlaybackUtility.CreateVideoRenderTexture(w, h);
                postWinVideoPlayer.targetTexture = _postWinRt;
                postWinVideoDisplay.texture = _postWinRt;
                postWinVideoDisplay.color = Color.black;
                postWinVideoDisplay.gameObject.SetActive(true);
                HidePostWinDialogUiDuringVideo();

                if (fadeOverlay != null) fadeOverlay.transform.SetAsFirstSibling();
                if (postWinVideoRoot != null)
                    postWinVideoRoot.transform.SetAsLastSibling();
                if (postWinVideoDisplay != null)
                    postWinVideoDisplay.transform.SetAsLastSibling();

                bool videoEnded = false;
                void OnPostWinEnd(VideoPlayer vp)
                {
                    videoEnded = true;
                    vp.Pause();
                }
                postWinVideoPlayer.loopPointReached += OnPostWinEnd;
                postWinVideoPlayer.Play();
                yield return VideoPlaybackUtility.CoWaitFirstFrameOrTimeout(postWinVideoPlayer, () => _postWinVideoFailed, 8f);
                if (_postWinVideoFailed || postWinVideoPlayer.frame < 0)
                {
                    postWinVideoPlayer.loopPointReached -= OnPostWinEnd;
                    postWinVideoPlayer.errorReceived -= onPostWinErr;
                    postWinVideoPlayer.Stop();
                    ReleasePostWinRenderTexture();
                    if (postWinVideoDisplay != null)
                    {
                        postWinVideoDisplay.texture = null;
                        postWinVideoDisplay.gameObject.SetActive(false);
                    }
                    if (postWinVideoRoot != null) postWinVideoRoot.SetActive(false);
                    yield return Fade(1f, 0f, postWinFadeToBlackDuration);
                    RestoreBridgeUiAfterPostWinVideo();
                }
                else
                {
                    yield return FadeRawImageFromBlackToWhite(postWinVideoDisplay, postWinVideoRevealDuration);
                    SetFadeOverlayAlpha(0f);
                    while (!videoEnded) yield return null;
                    postWinVideoPlayer.loopPointReached -= OnPostWinEnd;
                    postWinVideoPlayer.errorReceived -= onPostWinErr;
                }
            }
        }

        TeardownPostWinVideo();
        RestoreBridgeUiAfterPostWinVideo();
        yield return RunPostWinKnowledgeThenDialogFlow();
    }

    /// <summary>罗盘通关后：先小知识（多张点继续），再通关对话（整块 Panel 用 CanvasGroup，插图与「继续」同显同隐），最后进下一场景。</summary>
    IEnumerator RunPostWinKnowledgeThenDialogFlow()
    {
        if (compassPanel != null) compassPanel.SetActive(false);

        if (knowledgePanel != null && knowledgeSprites != null && knowledgeSprites.Length > 0)
        {
            _phase = Phase.Knowledge;
            MakePanelRootTransparentShowBackground(knowledgePanel);
            if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
            knowledgePanel.SetActive(true);
            knowledgePanel.transform.SetAsLastSibling();
            _knowledgeIndex = 0;
            ShowKnowledgeImage();
            if (knowledgeNextBtn != null)
            {
                knowledgeNextBtn.onClick.RemoveAllListeners();
                knowledgeNextBtn.onClick.AddListener(() => _knowledgeNextClicked = true);
            }
            int count = knowledgeSprites.Length;
            while (_knowledgeIndex < count)
            {
                _knowledgeNextClicked = false;
                if (knowledgeNextBtn == null) break;
                yield return new WaitUntil(() => _knowledgeNextClicked);
                _knowledgeIndex++;
                if (_knowledgeIndex < count)
                    ShowKnowledgeImage();
            }
            if (knowledgeNextBtn != null) knowledgeNextBtn.onClick.RemoveAllListeners();
            if (knowledgePanel != null) knowledgePanel.SetActive(false);
        }

        yield return RunPostWinDialogSequence();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>通关后插图对话：根节点 CanvasGroup 控制整块渐显/渐隐（底图、插图、「继续」一起）。</summary>
    IEnumerator RunPostWinDialogSequence()
    {
        if (postWinDialogPanel == null) yield break;

        if (compassPanel != null && compassPanel.activeSelf) compassPanel.SetActive(false);
        MakePanelRootTransparentShowBackground(postWinDialogPanel);

        if (postWinDialogContinueBtn != null)
        {
            postWinDialogContinueBtn.onClick.RemoveAllListeners();
            postWinDialogContinueBtn.gameObject.SetActive(true);
        }
        if (postWinDialogImage != null)
        {
            postWinDialogImage.gameObject.SetActive(true);
            postWinDialogImage.enabled = false;
            if (postWinDialogSprite != null)
            {
                postWinDialogImage.sprite = postWinDialogSprite;
                postWinDialogImage.enabled = true;
            }
        }
        var dlgCg = EnsurePostWinDialogCanvasGroup();
        if (dlgCg == null) yield break;
        dlgCg.alpha = 0f;
        dlgCg.interactable = false;
        dlgCg.blocksRaycasts = true;
        postWinDialogPanel.SetActive(true);
        postWinDialogPanel.transform.SetAsLastSibling();
        if (fadeOverlay != null) fadeOverlay.transform.SetAsLastSibling();
        yield return FadeCanvasGroupAlpha(dlgCg, 0f, 1f, postWinDialogFadeDuration);
        dlgCg.interactable = true;
        PlayPostWinDialogVoice();

        bool dialogClicked = false;
        if (postWinDialogContinueBtn != null)
        {
            postWinDialogContinueBtn.onClick.AddListener(() => dialogClicked = true);
            yield return new WaitUntil(() => dialogClicked);
        }
        StopPostWinDialogVoice();
        dlgCg.interactable = false;
        dlgCg.blocksRaycasts = false;
        yield return FadeCanvasGroupAlpha(dlgCg, 1f, 0f, postWinDialogFadeDuration);
        postWinDialogPanel.SetActive(false);
        dlgCg.alpha = 0f;
        if (postWinDialogContinueBtn != null) postWinDialogContinueBtn.onClick.RemoveAllListeners();

        RestoreBridgeUiAfterPostWinVideo();
    }

    /// <summary>通关视频与对话共用同一 Panel 时，播视频期间隐藏插图与「继续」，避免叠在画面上。</summary>
    void HidePostWinDialogUiDuringVideo()
    {
        if (postWinDialogImage != null)
            postWinDialogImage.gameObject.SetActive(false);
        if (postWinDialogContinueBtn != null)
            postWinDialogContinueBtn.gameObject.SetActive(false);
    }

    /// <summary>全屏浮层根 <see cref="Image"/> 改为全透明，仍拦截射线，下层 <c>GameRoot</c>（桥）作为背景可见。</summary>
    static void MakePanelRootTransparentShowBackground(GameObject panel)
    {
        if (panel == null) return;
        var img = panel.GetComponent<Image>();
        if (img == null) return;
        var c = img.color;
        c.a = 0f;
        img.color = c;
        img.raycastTarget = true;
    }

    /// <summary>通关全屏视频时关掉桥下世界与其它面板，避免叠在 RawImage 视频之上。</summary>
    private void HideBridgeUiForPostWinVideo()
    {
        if (_postWinHidBridgeUi) return;
        _postWinHidBridgeUi = true;
        var gr = transform.Find("GameRoot");
        if (gr != null) gr.gameObject.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
    }

    private void RestoreBridgeUiAfterPostWinVideo()
    {
        if (!_postWinHidBridgeUi) return;
        _postWinHidBridgeUi = false;
        var gr = transform.Find("GameRoot");
        if (gr != null) gr.gameObject.SetActive(true);
    }

    private RenderTexture _postWinRt;
    private bool _postWinVideoFailed;

    private void ReleasePostWinRenderTexture()
    {
        if (_postWinRt != null)
        {
            _postWinRt.Release();
            _postWinRt = null;
        }
    }

    private void TeardownPostWinVideo()
    {
        if (postWinVideoPlayer != null)
        {
            postWinVideoPlayer.Stop();
            postWinVideoPlayer.clip = null;
            postWinVideoPlayer.url = "";
        }
        if (postWinVideoDisplay != null)
        {
            postWinVideoDisplay.texture = null;
            postWinVideoDisplay.gameObject.SetActive(false);
        }
        if (postWinVideoRoot != null) postWinVideoRoot.SetActive(false);
        ReleasePostWinRenderTexture();
    }

    private void OnDestroy()
    {
        ReleasePostWinRenderTexture();
    }

    private static void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    private IEnumerator FadeGraphicAlpha(Graphic g, float from, float to, float duration)
    {
        if (g == null || duration <= 0.001f)
        {
            SetGraphicAlpha(g, to);
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            SetGraphicAlpha(g, Mathf.Lerp(from, to, e / duration));
            yield return null;
        }
        SetGraphicAlpha(g, to);
    }

    private static IEnumerator FadeRawImageFromBlackToWhite(RawImage raw, float duration)
    {
        if (raw == null || duration <= 0.001f)
        {
            if (raw != null) raw.color = Color.white;
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float u = Mathf.Clamp01(e / duration);
            u = u * u * (3f - 2f * u);
            raw.color = Color.Lerp(Color.black, Color.white, u);
            yield return null;
        }
        raw.color = Color.white;
    }

    private void SetFadeOverlayAlpha(float a)
    {
        if (fadeOverlay == null) return;
        fadeOverlay.color = new Color(0f, 0f, 0f, a);
    }

    void ShowPopup(string text, System.Action onContinue = null)
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        if (popupText != null) popupText.text = text;
        if (popupContinueBtn != null)
        {
            popupContinueBtn.onClick.RemoveAllListeners();
            popupContinueBtn.gameObject.SetActive(onContinue != null);
            if (onContinue != null) popupContinueBtn.onClick.AddListener(() => onContinue());
        }
    }

    void ShowKnowledgeImage()
    {
        if (knowledgeImage != null && knowledgeSprites != null && _knowledgeIndex < knowledgeSprites.Length && knowledgeSprites[_knowledgeIndex] != null)
            knowledgeImage.sprite = knowledgeSprites[_knowledgeIndex];
        if (knowledgeNextBtn != null) knowledgeNextBtn.gameObject.SetActive(true);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null || duration <= 0) yield break;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);
    }

    private CanvasGroup EnsurePostWinDialogCanvasGroup()
    {
        if (postWinDialogPanel == null) return null;
        var g = postWinDialogPanel.GetComponent<CanvasGroup>();
        return g != null ? g : postWinDialogPanel.AddComponent<CanvasGroup>();
    }

    private IEnumerator FadeCanvasGroupAlpha(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null || duration <= 0.001f)
        {
            if (cg != null) cg.alpha = to;
            yield break;
        }
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, e / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
