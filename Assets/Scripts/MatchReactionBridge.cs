using UnityEngine;

/// <summary>
/// 配对完成时通知 MatchToMeasurementBridge，启动测量仪
/// </summary>
public class MatchReactionBridge : MatchReaction
{
    public MatchToMeasurementBridge bridge;

    public override void OnMatched()
    {
        if (bridge == null) return;
        var dz = GetComponent<DropZone>();
        bridge.OnPairMatched(dz);
    }
}
