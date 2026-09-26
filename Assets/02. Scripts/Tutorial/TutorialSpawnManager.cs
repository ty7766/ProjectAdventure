using UnityEngine;

/// <summary>
/// 튜토리얼 전용 스폰 오브젝트 관리.
/// 보석/골인 오브젝트는 스테이지 프리팹 안에 미리 배치(비활성 상태)해 두고,
/// 해당 섹션에 도달했을 때 SetActive(true)로 '소환'한다. 도달 전에는 씬에 존재하지 않는 것과 같다.
/// </summary>
public class TutorialSpawnManager : MonoBehaviour
{
    //--- Fields ---//
    private TutorialGemObject _gem;
    private TutorialGoalObject _goal;
    private TutorialEventBus _eventBus;

    //--- Properties ---//
    /// <summary>튜토리얼 동안 보석이 획득됐는지 여부</summary>
    public bool WasGemCollected => _gem != null && _gem.WasCollected;

    //--- Public Methods ---//
    /// <summary>이벤트 버스를 주입합니다.</summary>
    public void Setup(TutorialEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// 스테이지 루트 하위에서 튜토리얼용 보석/골인 오브젝트를 찾아 상태를 준비합니다.
    /// 튜토리얼이 시작되는 동안에는 숨겨두고, 각 섹션 진입 시 소환한다.
    /// TutorialManager가 튜토리얼 시작 직후 호출합니다.
    /// </summary>
    public void BindStageObjects(Transform stageRoot)
    {
        _gem = stageRoot.GetComponentInChildren<TutorialGemObject>(true);
        _goal = stageRoot.GetComponentInChildren<TutorialGoalObject>(true);

        if (_gem == null)
        {
            CustomDebug.LogWarning("[TutorialSpawnManager] 스테이지에서 TutorialGemObject를 찾지 못했습니다.");
        }
        else
        {
            _gem.gameObject.SetActive(false); // 섹션 4 진입 시 활성화
        }

        if (_goal == null)
        {
            CustomDebug.LogWarning("[TutorialSpawnManager] 스테이지에서 TutorialGoalObject를 찾지 못했습니다.");
        }
        else
        {
            _goal.gameObject.SetActive(false); // 섹션 5 진입 시 활성화
        }
    }

    /// <summary>해당 타입 오브젝트를 소환(활성화)합니다. 이미 소환된 경우 무시합니다.</summary>
    public void Spawn(TutorialSpawnObjectType type)
    {
        switch (type)
        {
            case TutorialSpawnObjectType.Gem:
                ActivateGem();
                break;
            case TutorialSpawnObjectType.Goal:
                ActivateGoal();
                break;
        }
    }

    /// <summary>튜토리얼 종료 후 상태를 정리합니다. 골대는 다시 활성화해 일반 클리어 판정이 가능하게 한다.</summary>
    public void DespawnAll()
    {
        if (_gem != null)
        {
            // 보석은 획득 여부와 무관하게 다시 등장하지 않도록 비활성으로 둔다.
            // (튜토리얼에서 파괴/획득 처리된 상태 유지)
            _gem.gameObject.SetActive(false);
        }
        if (_goal != null)
        {
            // 골대는 스테이지 1의 실제 클리어 판정 오브젝트이므로 활성화해 복구한다.
            // 단, 즉시 켜면 플레이어가 아직 트리거 안에 있어 OnTriggerEnter가 재발할 수 있으므로
            // 다음 프레임으로 미룬다. (StageManager의 중복 클리어 가드가 있는 이중 안전망)
            var goal = _goal;
            _goal = null;
            RequestRestoreGoalNextFrame(goal);
        }
    }

    private void RequestRestoreGoalNextFrame(MonoBehaviour goal)
    {
        if (this == null || goal == null)
        {
            return;
        }
        StartCoroutine(RestoreGoalNextFrameRoutine(goal));
    }

    private System.Collections.IEnumerator RestoreGoalNextFrameRoutine(MonoBehaviour goal)
    {
        yield return null;
        if (goal != null)
        {
            goal.gameObject.SetActive(true);
        }
    }

    /// <summary>소환된 보석의 위치를 조회합니다. (소환 전이면 false)</summary>
    public bool TryGetGemPosition(out Vector3 position)
    {
        if (_gem != null && _gem.gameObject.activeInHierarchy)
        {
            position = _gem.transform.position;
            return true;
        }
        position = default;
        return false;
    }

    //--- Private Methods ---//
    private void ActivateGem()
    {
        if (_gem == null)
        {
            return;
        }
        if (_gem.gameObject.activeSelf)
        {
            return;
        }
        _gem.Setup(_eventBus);
        _gem.gameObject.SetActive(true);
    }

    private void ActivateGoal()
    {
        if (_goal == null)
        {
            return;
        }
        if (_goal.gameObject.activeSelf)
        {
            return;
        }
        _goal.Setup(_eventBus);
        _goal.gameObject.SetActive(true);
    }
}
