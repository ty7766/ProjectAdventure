using UnityEngine;

/// <summary>
/// 튜토리얼 전용 골인 지점. StageManager.StageClear()를 호출하지 않고
/// 이벤트 버스로만 통보한다. (튜토리얼 완료 콜백과 클리어 패널 분리 목적)
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialGoalObject : MonoBehaviour
{
    //--- Fields ---//
    private TutorialEventBus _eventBus;
    private bool _reached;

    /// <summary>이미 골인 지점에 도달했는지 여부</summary>
    public bool WasReached => _reached;

    //--- Unity Methods ---//
    private void OnTriggerEnter(Collider other)
    {
        if (_reached || !other.CompareTag("Player"))
        {
            return;
        }
        _reached = true;

        if (_eventBus != null)
        {
            _eventBus.RaiseGoalReached();
        }
    }

    //--- Public Methods ---//
    /// <summary>도달 상태를 초기화합니다. (튜토리얼 재시작 시 다시 판정할 수 있도록)</summary>
    public void ResetState()
    {
        _reached = false;
    }

    /// <summary>이벤트 버스를 주입합니다.</summary>
    public void Setup(TutorialEventBus eventBus)
    {
        _eventBus = eventBus;
    }
}
