using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 활성 상태인 TutorialAnchor 들을 ID로 조회할 수 있게 보관하는 레지스트리
/// </summary>
public class TutorialAnchorRegistry
{
    private static readonly Dictionary<string, TutorialAnchor> _anchors = new Dictionary<string, TutorialAnchor>(); 

    public static void Register(TutorialAnchor anchor)
    {
        if (anchor == null || string.IsNullOrEmpty(anchor.AnchorID))
        {
            return;
        }
        _anchors[anchor.AnchorID] = anchor;
    }
    
    public static void Unregister(TutorialAnchor anchor)
    {
        if(anchor == null || string.IsNullOrEmpty(anchor.AnchorID))
        {
            return;
        }
        if(_anchors.TryGetValue(anchor.AnchorID, out TutorialAnchor existing) && existing == anchor)
        {
            _anchors.Remove(anchor.AnchorID);
        }
    }

    public static Transform GetTransform(string anchorID)
    {
        if (string.IsNullOrEmpty(anchorID))
        {
            return null;
        }
        if (_anchors.TryGetValue(anchorID, out TutorialAnchor anchor) && anchor != null)
        {
            return anchor.transform;
        }
        return null;
    }
}
