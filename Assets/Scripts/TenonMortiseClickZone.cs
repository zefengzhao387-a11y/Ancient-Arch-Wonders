using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在榫卯光晕或榫卯图上，零度时点击即可完成嵌入
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TenonMortiseClickZone : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MeasurementBarController measurementBar;

    private void Awake()
    {
        if (measurementBar == null)
            measurementBar = FindObjectOfType<MeasurementBarController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (measurementBar != null)
            measurementBar.TryCompleteByClick();
    }
}
