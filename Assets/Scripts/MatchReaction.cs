using UnityEngine;

/// <summary>
/// 配对成功后的扩展反应，挂在 DropZone 上
/// </summary>
public class MatchReaction : MonoBehaviour
{
    public virtual void OnMatched() { }
}
