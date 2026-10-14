using UnityEngine;

/// <summary>
/// 튜토리얼 전용 보석. 이벤트 버스로 튜토리얼 진행을 통보하고,
/// StageManager에도 기록해 스테이지 부가 목표(보석 수집) 달성에 반영한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialGemObject : MonoBehaviour
{
    //--- Fields ---//
    private TutorialEventBus _eventBus;
    private StageManager _stageManager;
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
        // 튜토리얼 보석도 스테이지 미션에 그대로 반영한다 (튜토리얼 = 스테이지 1의 실제 플레이).
        if (_stageManager != null)
        {
            _stageManager.CollectGem($"{name}_Gem");
        }
        gameObject.SetActive(false);
    }

    //--- Public Methods ---//
    /// <summary>이벤트 버스를 주입합니다.</summary>
    public void Setup(TutorialEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>획득 상태를 초기화합니다. (튜토리얼 재시작 시 다시 획득할 수 있도록)</summary>
    public void ResetState()
    {
        _collected = false;
    }

    /// <summary>StageManager를 주입해 부가 목표(보석 수집) 판정에 반영되도록 합니다.</summary>
    public void SetStageManager(StageManager stageManager)
    {
        _stageManager = stageManager;
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
