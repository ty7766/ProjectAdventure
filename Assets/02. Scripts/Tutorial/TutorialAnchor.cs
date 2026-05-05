using UnityEngine;

/// <summary>
/// 튜토리얼이 강조할 위치 마커
/// 스테이지 프리팹 안의 빈 GameObject에 부착하고 AnchorID를 지정하면 활성화시 자동으로 추적
/// </summary>
public class TutorialAnchor : MonoBehaviour
{
    [SerializeField, Tooltip("TutorialStepConfig의 _worldAnchorID와 일치해야합니다")]
    private string _anchorID;

    public string AnchorID => _anchorID;

    private void OnEnable()
    {
        TutorialAnchorRegistry.Register(this);
    }

    private void OnDisable()
    {
        TutorialAnchorRegistry.Unregister(this);
    }
}
