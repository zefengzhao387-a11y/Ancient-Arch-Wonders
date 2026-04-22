using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 榫卯放置区：上方三个槽，接收拖拽物品。配对成功后显示微微铆合图（可调大小位置），完全铆合后换另一张图
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private int expectedItemId;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite matchedSprite;       // 微微铆合
    [SerializeField] private Sprite fullyRivetedSprite; // 完全铆合
    [SerializeField] private Sprite introSprite;        // 完全铆合后展示的榫卯介绍图

    [Header("微微铆合图尺寸位置（可自由调节）")]
    [SerializeField] private bool useCustomRect;
    [SerializeField] private Vector2 anchorMin = new Vector2(0, 0);
    [SerializeField] private Vector2 anchorMax = new Vector2(1, 1);
    [SerializeField] private Vector2 offsetMin = Vector2.zero;
    [SerializeField] private Vector2 offsetMax = Vector2.zero;

    private bool _isFullyRiveted;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponentInChildren<Image>();
            if (targetImage == null) targetImage = GetComponent<Image>();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var go = eventData.pointerDrag;
        if (go == null) return;

        var item = go.GetComponent<DraggableItem>();
        if (item == null || item.ItemId != expectedItemId) return;

        var mrb = GetComponent<MatchReactionBridge>();
        var bridge = mrb != null && mrb.bridge != null
            ? mrb.bridge
            : FindObjectOfType<MatchToMeasurementBridge>();
        if (mrb != null && bridge != null)
            mrb.bridge = bridge;

        if (bridge != null && bridge.IsAwaitingFullRivet)
        {
            bridge.NotifyPairingBlocked();
            return;
        }

        OnMatchSuccess(item);
    }

    private void OnMatchSuccess(DraggableItem item)
    {
        if (item == null) return;

        if (targetImage != null && matchedSprite != null)
            ApplySprite(targetImage, matchedSprite);

        item.SetMatched(true);
        item.gameObject.SetActive(false);

        foreach (var r in GetComponents<MatchReaction>())
            if (r != null) r.OnMatched();
    }

    private void ApplySprite(Image img, Sprite s)
    {
        if (img == null || s == null) return;
        img.sprite = s;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        if (useCustomRect)
        {
            var rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }

    public Sprite IntroSprite => introSprite;

    public bool IsFullyRiveted => _isFullyRiveted;

    public void SetFullyRiveted()
    {
        if (_isFullyRiveted) return;
        _isFullyRiveted = true;
        if (targetImage != null && fullyRivetedSprite != null)
            ApplySprite(targetImage, fullyRivetedSprite);
    }
}
