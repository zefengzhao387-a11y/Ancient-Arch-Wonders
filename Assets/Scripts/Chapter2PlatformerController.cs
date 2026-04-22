using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 第二章平台游戏 - 从零实现
/// 尖刺、平台、碰撞、跳跃、构件收集、出口、人物；可选跳跃/拾取构件音效
/// 出生点 = 人物在场景中的初始位置（拖到哪里就是哪里）
/// </summary>
public class Chapter2PlatformerController : MonoBehaviour
{
    [Header("人物")]
    [SerializeField] private RectTransform playerRect;
    [Tooltip("静止时的站立图")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float frameInterval = 0.1f;

    [Header("平台")]
    [SerializeField] private RectTransform[] platforms;

    [Header("构件（收集物）")]
    [SerializeField] private RectTransform[] woodItems;
    [SerializeField] private int totalToCollect = 3;
    [Header("收集栏（拾取后飞向左上角槽位；不拖则保持原地直接消失）")]
    [Tooltip("与拾取顺序一一对应，通常 3 个槽排在收集条内")]
    [SerializeField] private RectTransform[] collectionSlotTargets;
    [SerializeField] private float collectFlyDuration = 0.48f;
    [SerializeField] private float collectFlyArcHeight = 80f;
    [SerializeField] private AnimationCurve collectFlyCurve;
    [Tooltip("飞行中图标最大边长(px)，避免原图过大")]
    [SerializeField] private float flyingIconMaxSize = 88f;

    /// <summary>槽位下方块常驻；木图标落在此子物体上盖住方框。</summary>
    public const string CollectionOverlayChildName = "CollectedIcon";

    [Header("尖刺")]
    [SerializeField] private RectTransform[] spikeZones;

    [Header("出口")]
    [SerializeField] private RectTransform exitZone;
    [SerializeField] private GameObject pressSPrompt;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("物理")]
    [SerializeField] private float moveSpeed = 200f;
    [SerializeField] private float gravity = 200f;
    [SerializeField] private float jumpForce = 160f;
    [SerializeField] private float maxFallSpeed = 300f;
    [Tooltip("脚底与平台顶的容差(px)，越大越容易站稳，建议 8-15")]
    [SerializeField] private float groundedTolerance = 10f;
    [Tooltip("碰到平台边缘时自动踏上平台的最大高度(px)，0=禁用，建议 60-80")]
    [SerializeField] private float stepUpHeight = 80f;
    [Tooltip("勾选后在 Console 打印平台碰撞调试信息")]
    [SerializeField] private bool debugPlatformCollision;

    [Header("通关")]
    [SerializeField] private string nextSceneName = "Chapter2VideoEnd";

    [Header("音效（可选，水镜台内）")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip pickupWoodClip;
    [SerializeField] [Range(0f, 3f)] private float jumpVolume = 1f;
    [SerializeField] [Range(0f, 3f)] private float pickupWoodVolume = 1f;
    [Tooltip("出口处按 S 进入下一场景时播放（可选）")]
    [SerializeField] private AudioClip exitConfirmKeyClip;
    [SerializeField] [Range(0f, 3f)] private float exitConfirmKeyVolume = 1f;
    [Header("出口门（可选）")]
    [Tooltip("收集齐后第一次进入出口区域时播放")]
    [SerializeField] private AudioClip doorEnterClip;
    [Tooltip("按 S 离开关卡、开始渐黑前播放")]
    [SerializeField] private AudioClip doorExitClip;
    [SerializeField] [Range(0f, 3f)] private float doorSfxVolume = 1f;

    // 出生点 = 人物初始位置，由场景中摆放决定
    private Vector2 _spawnPosition;
    private Vector2 _velocity;
    private int _collected;
    private bool _gameOver;
    private bool _cleared;
    private bool _exitDoorEnterPlayed;
    private Transform _parent;
    private Image _playerImage;
    private float _frameTimer;
    private int _frameIndex;
    private readonly Vector3[] _corners = new Vector3[4];
    private readonly HashSet<int> _ceilingRectWarned = new HashSet<int>();
    private AudioSource _sfx;

    void Start()
    {
        _parent = playerRect != null ? playerRect.parent : null;
        _spawnPosition = playerRect != null ? playerRect.anchoredPosition : Vector2.zero;
        _velocity = Vector2.zero;
        _lastGroundedTime = Time.time;
        _gameStartTime = Time.time;
        _playerImage = playerRect != null ? FootShadow.GetCharacterImage(playerRect) : null;
        if (idleSprite != null && _playerImage != null) _playerImage.sprite = idleSprite;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (pressSPrompt != null) pressSPrompt.SetActive(false);

        if ((platforms == null || platforms.Length == 0) && playerRect != null && _parent != null)
        {
            var list = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform t in _parent)
            {
                if (t == playerRect) continue;
                var rt = t.GetComponent<RectTransform>();
                if (rt != null && (t.name.Contains("Platform") || t.name.Contains("Floor") || t.name.Contains("平台") || t.name.Contains("地面")))
                    list.Add(rt);
            }
            if (list.Count > 0) platforms = list.ToArray();
        }
        if (_parent != null && platforms != null)
        {
            foreach (Transform t in _parent)
            {
                var hm = t.GetComponent<PlatformHeightMap>();
                if (hm != null && hm.HasData)
                {
                    var rt = t.GetComponent<RectTransform>();
                    if (rt != null && (platforms == null || System.Array.IndexOf(platforms, rt) < 0))
                        Debug.LogWarning($"平台「{t.name}」有烘焙高度图但未在 Platforms 数组中，请拖入 Canvas → Chapter2PlatformerController → Platforms");
                }
            }
        }

        EnsureSfxSource();
        if (collectFlyCurve == null || collectFlyCurve.length == 0)
            collectFlyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    void EnsureSfxSource()
    {
        if (jumpClip == null && pickupWoodClip == null && exitConfirmKeyClip == null && doorEnterClip == null && doorExitClip == null) return;
        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = 0f;
    }

    void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
    }

    // AI辅助：Google Gemini（2026-04，水镜台行走/跳跃缓冲/贴地逻辑讨论）— 以下为人工接入 PlatformHeightMap 与场景调参后的实现。
    void Update()
    {
        if (_gameOver || _cleared || playerRect == null || _parent == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        bool jumpKey = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
        if (jumpKey) _jumpBufferTime = 0.15f;
        _jumpBufferTime -= Time.deltaTime;
        bool canJump = _jumpBufferTime > 0;

        bool grounded = IsGrounded();
        if (grounded && _velocity.y <= 0) _lastGroundedTime = Time.time;

        // 只有真正站在平台上（静止或下落）才贴地；正在起跳时不算 grounded，否则会抵消跳跃
        bool standing = grounded && _velocity.y <= 0;
        var jumpedThisFrame = false;
        if (standing)
        {
            _velocity.y = 0;
            if (canJump)
            {
                _velocity.y = jumpForce;
                _jumpBufferTime = 0;
                jumpedThisFrame = true;
            }
        }
        else
        {
            _velocity.y -= gravity * Time.deltaTime;
            if (_velocity.y < -maxFallSpeed) _velocity.y = -maxFallSpeed;
            if (canJump && (WasGroundedRecently() || InStartJumpBuffer()))
            {
                _velocity.y = jumpForce;
                _jumpBufferTime = 0;
                jumpedThisFrame = true;
            }
        }

        if (jumpedThisFrame)
            PlaySfx(jumpClip, jumpVolume);

        _velocity.x = h * moveSpeed;

        if (h != 0)
        {
            var s = playerRect.localScale;
            s.x = h > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            playerRect.localScale = s;
            UpdateWalkFrame();
        }
        else if (idleSprite != null && _playerImage != null)
            _playerImage.sprite = idleSprite;

        var pos = playerRect.anchoredPosition;
        pos += _velocity * Time.deltaTime;
        playerRect.anchoredPosition = pos;

        ResolvePlatformCollisions();

        CheckCollectibles();
        CheckSpikes();
        CheckExit();
    }

    private float _lastGroundedTime;
    private float _gameStartTime;
    private float _jumpBufferTime;
    private float _lastDebugLogTime;
    private bool WasGroundedRecently() => Time.time - _lastGroundedTime < 0.2f;
    private bool InStartJumpBuffer() => Time.time - _gameStartTime < 1f;

    void UpdateWalkFrame()
    {
        if (_playerImage == null || walkFrames == null || walkFrames.Length == 0) return;
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= frameInterval)
        {
            _frameTimer = 0;
            _frameIndex = (_frameIndex + 1) % walkFrames.Length;
            if (walkFrames[_frameIndex] != null) _playerImage.sprite = walkFrames[_frameIndex];
        }
    }

    void GetLocalBounds(RectTransform rt, out float left, out float right, out float bottom, out float top)
    {
        left = right = bottom = top = 0;
        if (rt == null || _parent == null) return;
        rt.GetWorldCorners(_corners);
        var p0 = _parent.InverseTransformPoint(_corners[0]);
        var p2 = _parent.InverseTransformPoint(_corners[2]);
        left = p0.x; bottom = p0.y; right = p2.x; top = p2.y;
    }

    /// <summary>获取平台在指定 X 处的顶面 Y。优先用烘焙高度图，其次 Sprite 透明度，否则用矩形边。</summary>
    bool TryGetPlatformSurfaceY(RectTransform p, float charX, out float surfaceY)
    {
        var hm = p.GetComponent<PlatformHeightMap>();
        if (hm != null && hm.HasData && _parent != null)
        {
            if (hm.TryGetSurfaceY(p, _parent, charX, out surfaceY)) return true;
            // 边缘采样失败时用 rect 顶边 fallback，便于踏上
            GetLocalBounds(p, out float _, out float _, out float _, out float rectTop);
            surfaceY = rectTop;
            return true;
        }
        if (TryGetSpriteSurfaceY(p, charX, out surfaceY)) return true;
        p.GetWorldCorners(_corners);
        var c = new Vector2[4];
        for (int i = 0; i < 4; i++) c[i] = _parent.InverseTransformPoint(_corners[i]);
        float bestY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            var a = c[i];
            var b = c[(i + 1) % 4];
            float xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x);
            if (charX < xMin - 5f || charX > xMax + 5f) continue;
            float y = Mathf.Abs(b.x - a.x) < 0.001f ? Mathf.Max(a.y, b.y) : Mathf.Lerp(a.y, b.y, Mathf.Clamp01((charX - a.x) / (b.x - a.x)));
            if (y > bestY) bestY = y;
        }
        if (bestY <= float.MinValue + 1f) { surfaceY = 0; return false; }
        surfaceY = bestY;
        return true;
    }

    static bool _spriteReadWarned;
    /// <summary>用 Sprite 透明度找顶面 Y，原图即碰撞。Image 必须拖入 Sprite，且纹理需 Read/Write。</summary>
    bool TryGetSpriteSurfaceY(RectTransform p, float charX, out float surfaceY)
    {
        surfaceY = 0;
        var img = p.GetComponent<Image>();
        if (img == null) img = p.GetComponentInChildren<Image>();
        if (img == null || img.sprite == null) return false;
        var sprite = img.sprite;
        var tex = sprite.texture as Texture2D;
        if (tex == null || !tex.isReadable)
        {
            if (!_spriteReadWarned) { Debug.LogWarning("平台原图碰撞需勾选 Read/Write：选中平台图→Inspector→Read/Write Enabled→Apply"); _spriteReadWarned = true; }
            return false;
        }
        GetLocalBounds(p, out float left, out float right, out float bottom, out float top);
        if (right - left < 0.1f) return false;
        if (charX < left - 2f || charX > right + 2f) return false;
        float u = Mathf.Clamp01((charX - left) / (right - left));
        int texX = Mathf.Clamp((int)(sprite.rect.x + u * sprite.rect.width), (int)sprite.rect.x, (int)sprite.rect.xMax - 1);
        int texYMin = (int)sprite.rect.y;
        int texYMax = (int)sprite.rect.yMax;
        for (int y = texYMax - 1; y >= texYMin; y--)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = Mathf.Clamp(texX + dx, (int)sprite.rect.x, (int)sprite.rect.xMax - 1);
                if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                if (tex.GetPixel(x, y).a > 0.15f)
                {
                    float v = (y - texYMin) / Mathf.Max(0.001f, sprite.rect.height);
                    surfaceY = bottom + v * (top - bottom);
                    return true;
                }
            }
        }
        return false;
    }

    bool IsGrounded()
    {
        if (playerRect == null || _parent == null) return false;
        GetLocalBounds(playerRect, out float pl, out float pr, out float pb, out float pt);
        float charCenterX = (pl + pr) * 0.5f;
        if (platforms == null) return false;
        foreach (var p in platforms)
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            GetLocalBounds(p, out float tl, out float tr, out float tb, out float tt);
            if (pr <= tl || pl >= tr) continue;
            if (!TryGetPlatformSurfaceY(p, charCenterX, out float surfaceY)) continue;
            bool onTop = pb >= surfaceY - groundedTolerance && pb <= surfaceY + groundedTolerance;
            if (onTop) return true;
        }
        return false;
    }

    // AI辅助：Google Gemini（2026-04，顶头阻挡/平台底面与 AABB 几何讨论）— 以下为与 PlatformHeightMap.TryGetBottomSurfaceY 等结合的工程实现。
    void ResolvePlatformCollisions()
    {
        if (playerRect == null || _parent == null) return;
        GetLocalBounds(playerRect, out float pl, out float pr, out float pb, out float pt);
        float halfH = (pt - pb) * 0.5f;

        float? ceilingY = null;
        float? pushLeft = null;
        float? pushRight = null;
        float charCenterX = (pl + pr) * 0.5f;
        float charLeft = pl + (pr - pl) * 0.15f, charRight = pr - (pr - pl) * 0.15f;

        float? snapToSurfaceY = null; // 站立时贴合平台轮廓
        foreach (var p in platforms ?? new RectTransform[0])
        {
            if (p == null) continue;
            if (!p.gameObject.activeSelf) continue;
            GetLocalBounds(p, out float tl, out float tr, out float tb, out float tt);
            bool hasSurface = TryGetPlatformSurfaceY(p, charCenterX, out float surfaceY);
            if (hasSurface && TryGetPlatformSurfaceY(p, charLeft, out float syL) && TryGetPlatformSurfaceY(p, charRight, out float syR))
                surfaceY = Mathf.Max(surfaceY, syL, syR);
            if (!hasSurface) surfaceY = tt;

            bool overlapX = pr > tl && pl < tr;
            bool overlapY = pb < tt && pt > tb;
            bool standingOnTop = hasSurface && pb >= surfaceY - groundedTolerance && pb <= surfaceY + groundedTolerance && overlapX;
            bool penetrated = hasSurface && overlapX && _velocity.y <= 0 && pb < surfaceY - 2f && pt > surfaceY - 20f;

            if ((standingOnTop || penetrated) && _velocity.y <= 0)
            {
                if (!snapToSurfaceY.HasValue || surfaceY > snapToSurfaceY.Value)
                    snapToSurfaceY = surfaceY;
            }

            if (_velocity.y > 0 && overlapX && pb < surfaceY - 5f)
            {
                var hm = p.GetComponent<PlatformHeightMap>();
                bool useBottomMap = hm != null && hm.bottomHeightMap != null && hm.bottomHeightMap.Length > 0 && _parent != null;
                float ceilingBottomY = tb;
                bool hasCeiling = !useBottomMap;
                if (!useBottomMap && (p.GetComponent<Image>()?.sprite != null || p.GetComponentInChildren<Image>()?.sprite != null) && !_ceilingRectWarned.Contains(p.GetInstanceID()))
                {
                    _ceilingRectWarned.Add(p.GetInstanceID());
                    Debug.LogWarning($"平台「{p.name}」有 Sprite 但无烘焙底面，头顶碰撞为矩形。请添加 Platform Height Map 并点击烘焙。");
                }
                if (useBottomMap)
                {
                    float byMin = float.MaxValue;
                    if (hm.TryGetBottomSurfaceY(p, _parent, charCenterX, out float by)) { byMin = Mathf.Min(byMin, by); hasCeiling = true; }
                    if (hm.TryGetBottomSurfaceY(p, _parent, charLeft, out float byL)) { byMin = Mathf.Min(byMin, byL); hasCeiling = true; }
                    if (hm.TryGetBottomSurfaceY(p, _parent, charRight, out float byR)) { byMin = Mathf.Min(byMin, byR); hasCeiling = true; }
                    if (hm.TryGetBottomSurfaceY(p, _parent, pl, out float byPl)) { byMin = Mathf.Min(byMin, byPl); hasCeiling = true; }
                    if (hm.TryGetBottomSurfaceY(p, _parent, pr, out float byPr)) { byMin = Mathf.Min(byMin, byPr); hasCeiling = true; }
                    if (hasCeiling) ceilingBottomY = byMin;
                }
                if (hasCeiling && pt >= ceilingBottomY - 5f && pt <= ceilingBottomY + 40f)
                {
                    float cy = ceilingBottomY - halfH - 2f;
                    if (!ceilingY.HasValue || cy < ceilingY.Value) ceilingY = cy;
                }
            }

            if (overlapX && overlapY && !standingOnTop)
            {
                float platformLeft = tl, platformRight = tr;
                var hm = p.GetComponent<PlatformHeightMap>();
                if (hm != null && hm.HasEdgeData && _parent != null)
                {
                    float charCenterY = (pb + pt) * 0.5f;
                    bool hasLeft = hm.TryGetLeftEdgeX(p, _parent, charCenterY, out float leftEdge);
                    bool hasRight = hm.TryGetRightEdgeX(p, _parent, charCenterY, out float rightEdge);
                    if (!hasLeft || !hasRight || leftEdge >= rightEdge - 2f) continue;
                    platformLeft = leftEdge;
                    platformRight = rightEdge;
                }
                float edgeX = 0;
                if (_velocity.x > 0 && pr > platformLeft && pl < platformLeft)
                {
                    edgeX = Mathf.Clamp(platformLeft + 10f, platformLeft, platformRight - 5f);
                    if (stepUpHeight > 0 && TryGetPlatformSurfaceY(p, edgeX, out float edgeSy) && edgeSy - pb > 0 && edgeSy - pb <= stepUpHeight)
                        snapToSurfaceY = (!snapToSurfaceY.HasValue || edgeSy > snapToSurfaceY.Value) ? edgeSy : snapToSurfaceY;
                    else
                        pushLeft = pushLeft.HasValue ? Mathf.Min(pushLeft.Value, platformLeft) : platformLeft;
                }
                if (_velocity.x < 0 && pl < platformRight && pr > platformRight)
                {
                    edgeX = Mathf.Clamp(platformRight - 10f, platformLeft + 5f, platformRight);
                    if (stepUpHeight > 0 && TryGetPlatformSurfaceY(p, edgeX, out float edgeSy2) && edgeSy2 - pb > 0 && edgeSy2 - pb <= stepUpHeight)
                        snapToSurfaceY = (!snapToSurfaceY.HasValue || edgeSy2 > snapToSurfaceY.Value) ? edgeSy2 : snapToSurfaceY;
                    else
                        pushRight = pushRight.HasValue ? Mathf.Max(pushRight.Value, platformRight) : platformRight;
                }
            }
        }

        var pos = playerRect.anchoredPosition;
        if (snapToSurfaceY.HasValue)
        {
            GetLocalBounds(playerRect, out float _, out float _, out float curPb, out float _);
            pos.y += snapToSurfaceY.Value - curPb;
            _velocity.y = 0;
            if (debugPlatformCollision && Time.time - _lastDebugLogTime > 1f)
            {
                _lastDebugLogTime = Time.time;
                Debug.Log($"平台轮廓贴合: surfaceY={snapToSurfaceY.Value:F0} curPb={curPb:F0}");
            }
        }
        if (ceilingY.HasValue)
        {
            float curY = _parent.InverseTransformPoint(playerRect.TransformPoint(Vector3.zero)).y;
            pos.y += ceilingY.Value - curY;
            _velocity.y = 0;
        }
        if (pushLeft.HasValue)
        {
            float halfW = (pr - pl) * 0.5f;
            float curX = _parent.InverseTransformPoint(playerRect.TransformPoint(Vector3.zero)).x;
            pos.x += (pushLeft.Value - halfW - 2f) - curX;
            _velocity.x = 0;
        }
        else if (pushRight.HasValue)
        {
            float halfW = (pr - pl) * 0.5f;
            float curX = _parent.InverseTransformPoint(playerRect.TransformPoint(Vector3.zero)).x;
            pos.x += (pushRight.Value + halfW + 2f) - curX;
            _velocity.x = 0;
        }

        playerRect.anchoredPosition = pos;
    }

    bool Overlaps(RectTransform a, RectTransform b)
    {
        if (a == null || b == null) return false;
        GetLocalBounds(a, out float al, out float ar, out float ab, out float at);
        GetLocalBounds(b, out float bl, out float br, out float bb, out float bt);
        return !(ar < bl || al > br || at < bb || ab > bt);
    }

    void CheckCollectibles()
    {
        if (playerRect == null || _parent == null) return;
        foreach (var w in woodItems ?? new RectTransform[0])
        {
            if (w == null) continue;
            if (!w.gameObject.activeSelf) continue;
            if (Overlaps(playerRect, w))
            {
                if (_collected >= totalToCollect) continue;
                int slotIndex = _collected;
                _collected++;
                PlaySfx(pickupWoodClip, pickupWoodVolume);
                if (collectionSlotTargets != null && collectionSlotTargets.Length > 0 &&
                    slotIndex >= 0 && slotIndex < collectionSlotTargets.Length &&
                    collectionSlotTargets[slotIndex] != null)
                    StartCoroutine(FlyWoodItemToCollectionSlot(w, slotIndex));
                else
                    w.gameObject.SetActive(false);
            }
        }
    }

    static Sprite GetWoodItemSprite(RectTransform wood)
    {
        if (wood == null) return null;
        var img = wood.GetComponent<Image>();
        if (img == null) img = wood.GetComponentInChildren<Image>();
        return img != null ? img.sprite : null;
    }

    /// <summary>槽位根上用于显示「飞入木构件」的一层 Image（盖住底框，底框用槽位自己的 Image）。</summary>
    static Image GetOrCreateCollectionOverlayImage(RectTransform slot)
    {
        if (slot == null) return null;
        var tr = slot.Find(CollectionOverlayChildName);
        if (tr != null)
            return tr.GetComponent<Image>();
        var go = new GameObject(CollectionOverlayChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(slot, false);
        rt.SetAsLastSibling();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(48f, 48f);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }

    bool TryWorldCenterInCanvasLocal(RectTransform uiRect, RectTransform canvasRt, Canvas canvas, out Vector2 localCenter)
    {
        localCenter = default;
        if (uiRect == null || canvasRt == null || canvas == null) return false;
        uiRect.GetWorldCorners(_corners);
        var worldCenter = (_corners[0] + _corners[2]) * 0.5f;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        var screen = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, cam, out localCenter);
    }

    IEnumerator FlyWoodItemToCollectionSlot(RectTransform wood, int slotIndex)
    {
        var sprite = GetWoodItemSprite(wood);
        var slot = collectionSlotTargets[slotIndex];
        if (slot == null || sprite == null)
        {
            if (wood != null) wood.gameObject.SetActive(false);
            yield break;
        }

        var rootCanvas = wood.GetComponentInParent<Canvas>()?.rootCanvas ?? slot.GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas == null)
        {
            wood.gameObject.SetActive(false);
            yield break;
        }
        var canvasRt = rootCanvas.transform as RectTransform;
        if (!TryWorldCenterInCanvasLocal(wood, canvasRt, rootCanvas, out var startLocal) ||
            !TryWorldCenterInCanvasLocal(slot, canvasRt, rootCanvas, out var endLocal))
        {
            wood.gameObject.SetActive(false);
            yield break;
        }

        wood.gameObject.SetActive(false);

        var flyGo = new GameObject("FlyingWoodCollect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var flyRt = flyGo.GetComponent<RectTransform>();
        flyRt.SetParent(canvasRt, false);
        flyRt.SetAsLastSibling();
        flyRt.anchorMin = flyRt.anchorMax = new Vector2(0.5f, 0.5f);
        flyRt.pivot = new Vector2(0.5f, 0.5f);
        var flyImg = flyGo.GetComponent<Image>();
        flyImg.sprite = sprite;
        flyImg.raycastTarget = false;
        float w0 = wood.sizeDelta.x > 1f ? wood.sizeDelta.x : 80f;
        float h0 = wood.sizeDelta.y > 1f ? wood.sizeDelta.y : 80f;
        float s = flyingIconMaxSize / Mathf.Max(w0, h0);
        flyRt.sizeDelta = new Vector2(w0 * s, h0 * s);
        flyRt.anchoredPosition = startLocal;

        float dur = Mathf.Max(0.05f, collectFlyDuration);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float e = collectFlyCurve != null && collectFlyCurve.length > 0 ? collectFlyCurve.Evaluate(u) : u;
            var pos = Vector2.Lerp(startLocal, endLocal, e);
            pos.y += Mathf.Sin(Mathf.PI * u) * collectFlyArcHeight;
            flyRt.anchoredPosition = pos;
            yield return null;
        }

        var overlay = GetOrCreateCollectionOverlayImage(slot);
        if (overlay != null)
        {
            overlay.sprite = sprite;
            overlay.enabled = true;
            overlay.color = Color.white;
            overlay.SetNativeSize();
            float sw = Mathf.Max(1f, overlay.rectTransform.sizeDelta.x);
            float sh = Mathf.Max(1f, overlay.rectTransform.sizeDelta.y);
            float slotCap = Mathf.Min(slot.rect.width, slot.rect.height);
            float maxS = Mathf.Min(flyingIconMaxSize, slotCap > 4f ? slotCap - 4f : flyingIconMaxSize);
            float sc = maxS / Mathf.Max(sw, sh);
            overlay.rectTransform.sizeDelta = new Vector2(sw * sc, sh * sc);
        }

        Destroy(flyGo);
    }

    void CheckSpikes()
    {
        if (playerRect == null || _parent == null) return;
        foreach (var s in spikeZones ?? new RectTransform[0])
        {
            if (s == null) continue;
            if (!s.gameObject.activeSelf) continue;
            if (Overlaps(playerRect, s)) { OnHitSpike(); return; }
        }
    }

    void CheckExit()
    {
        if (playerRect == null || exitZone == null || _collected < totalToCollect) return;
        if (Overlaps(playerRect, exitZone))
        {
            if (pressSPrompt != null) pressSPrompt.SetActive(true);
            if (!_exitDoorEnterPlayed && doorEnterClip != null)
            {
                PlaySfx(doorEnterClip, doorSfxVolume);
                _exitDoorEnterPlayed = true;
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                PlaySfx(exitConfirmKeyClip, exitConfirmKeyVolume);
                _cleared = true;
                StartCoroutine(LoadNextScene());
            }
        }
        else
        {
            _exitDoorEnterPlayed = false;
            if (pressSPrompt != null) pressSPrompt.SetActive(false);
        }
    }

    void OnHitSpike()
    {
        if (_gameOver) return;
        _gameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    void OnRestart()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOver = false;
        _velocity = Vector2.zero;
        _lastGroundedTime = Time.time;
        _gameStartTime = Time.time;
        if (playerRect != null) playerRect.anchoredPosition = _spawnPosition;
        foreach (var w in woodItems ?? new RectTransform[0])
        {
            if (w == null) continue;
            w.gameObject.SetActive(true);
        }
        _collected = 0;
        ClearCollectionBarSlots();
    }

    void ClearCollectionBarSlots()
    {
        if (collectionSlotTargets == null) return;
        foreach (var slot in collectionSlotTargets)
        {
            if (slot == null) continue;
            var tr = slot.Find(CollectionOverlayChildName);
            if (tr == null) continue;
            var o = tr.GetComponent<Image>();
            if (o == null) continue;
            o.sprite = null;
            o.enabled = false;
        }
    }

    IEnumerator LoadNextScene()
    {
        PlaySfx(doorExitClip, doorSfxVolume);
        if (pressSPrompt != null) pressSPrompt.SetActive(false);
        var fade = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (fade != null)
        {
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                var c = fade.color;
                c.a = Mathf.Lerp(0, 1, t / 0.8f);
                fade.color = c;
                yield return null;
            }
        }
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
