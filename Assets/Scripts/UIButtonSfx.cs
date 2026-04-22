using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 挂在带 Button 的物体上：指针按下时走全局 <see cref="GameUISfxHub"/>（共用一条默认点击音）。
/// 用 PointerDown 早于 Button.onClick，避免 onClick 里先关 interactable 导致同一次点击无声。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("留空则用 GameUISfxHub 的默认点击音；仅个别按钮需要不同音时再拖")]
    [FormerlySerializedAs("clickClip")]
    [SerializeField] AudioClip clickOverride;

    Button _button;

    void Awake() => _button = GetComponent<Button>();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_button != null && !_button.interactable) return;
        GameUISfxHub.PlayShared(clickOverride);
    }
}
