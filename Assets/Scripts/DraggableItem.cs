using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖拽的榫卯：拖到对应 DropZone 上完成配对
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private int itemId;
    private RectTransform _rect;
    private RectTransform _parentRect;
    private CanvasGroup _canvasGroup;
    private Vector2 _offset;
    private Vector2 _homePosition;
    private bool _matched;

    public int ItemId => itemId;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _parentRect = _rect.parent as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        _homePosition = _rect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_matched) return;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.9f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var local);
        _offset = _rect.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_matched) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var pos))
            return;
        _rect.anchoredPosition = pos + _offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
        if (!_matched)
            _rect.anchoredPosition = _homePosition;
    }

    public void SetMatched(bool matched)
    {
        _matched = matched;
        if (matched && _canvasGroup != null)
        {
            _canvasGroup.alpha = 0.4f;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
