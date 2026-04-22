using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 卷轴丝带拖拽：拖拽超过阈值后触发打开
/// </summary>
public class ScrollRibbonDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private float dragThreshold = 80f;
    public UnityEvent onOpened;

    private Vector2 _startPos;
    private bool _opened;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_opened) return;
        _startPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_opened) return;
        float dist = Vector2.Distance(eventData.position, _startPos);
        if (dist >= dragThreshold)
        {
            _opened = true;
            onOpened?.Invoke();
        }
    }
}
