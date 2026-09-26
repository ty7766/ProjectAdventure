using UnityEngine;

/// <summary>
/// 튜토리얼 전용 보석. StageManager에 기록하지 않고 이벤트 버스로만 통보한다.
/// (HUD 미션 카운트 오염 방지 + 튜토리얼 종료 후 잔여 해제 불필요)
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialGemObject : MonoBehaviour
{
    //--- Fields ---//
    private TutorialEventBus _eventBus;
    private bool _collected;

    //--- Properties ---//
    /// <summary>이미 획득되었는지 여부 (튜토리얼 종료 시 복구 판단용)</summary>
    public bool WasCollected => _collected;

    //--- Unity Methods ---//
    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag("Player"))
        {
            return;
        }
        _collected = true;

        if (_eventBus != null)
        {
            _eventBus.RaiseGemCollected();
        }
        gameObject.SetActive(false);
    }

    //--- Public Methods ---//
    /// <summary>이벤트 버스를 주입합니다.</summary>
    public void Setup(TutorialEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>플레이어가 근접했을 때 발견 처리를 위해 Manager에서 호출합니다.</summary>
    public void NotifyFound()
    {
        if (_collected || _eventBus == null)
        {
            return;
        }
        _eventBus.RaiseGemFound();
    }
}
