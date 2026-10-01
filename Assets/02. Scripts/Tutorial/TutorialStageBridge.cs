using UnityEngine;

/// <summary>
/// 튜토리얼과 스테이지 게임플레이 사이의 연결부.
/// 튜토리얼 진행(섹션/목표)은 TutorialManager가, 스테이지 상태 조작(타이머/조작/스폰/클리어)은 이 클래스가 담당한다.
/// 의존성은 모두 생성자로 주입받는다. (씬 탐색 없음)
/// </summary>
public class TutorialStageBridge
{
    private readonly StageManager _stageManager;
    private readonly TutorialSpawnManager _spawnManager;
    private readonly TutorialEventBus _eventBus;

    public MapManager MapManager => _stageManager != null ? _stageManager.MapManager : null;

    public Transform PlayerTransform =>
        _stageManager != null && _stageManager.PlayerController != null
            ? _stageManager.PlayerController.transform
            : null;

    public TutorialStageBridge(StageManager stageManager, TutorialSpawnManager spawnManager, TutorialEventBus eventBus)
    {
        _stageManager = stageManager;
        _spawnManager = spawnManager;
        _eventBus = eventBus;
    }

    /// <summary>
    /// 튜토리얼 동안 플레이어가 실제로 조작할 수 있게 게임 상태를 푼다.
    /// (InitializeStage에서 Pause + 조작 비활성 상태로 들어오기 때문)
    /// </summary>
    public void PreparePlayEnvironment()
    {
        if (_stageManager == null)
        {
            CustomDebug.LogWarning("TutorialStageBridge: StageManager가 주입되지 않았습니다. 조작 활성화가 누락될 수 있습니다.");
            return;
        }

        // 튜토리얼은 스테이지 1의 실제 플레이 구간이다.
        // 시간이 흐르도록 타이머를 가동해 시간 제한 등 부가 목표 판정이 유효해진다.
        _stageManager.StartStageTimer();
        _stageManager.ResumeGameSmoothly();
        _stageManager.PlayerController?.EnablePlayerControl();

        // 스테이지 내 튜토리얼용 보석/골인 오브젝트를 탐색해 비활성 대기시킨다.
        // 스테이지 루트 기준으로 검색해야 한다 (StageManager/MapManager 하위가 아님).
        if (_spawnManager != null)
        {
            _spawnManager.Setup(_eventBus);
            _spawnManager.SetStageManager(_stageManager);
            _spawnManager.BindStageObjects(_stageManager.transform.root);
        }
    }

    /// <summary>맵 조작(선택/교체) 입력을 열거나 닫습니다.</summary>
    public void SetMapControlEnabled(bool enabled)
    {
        MapManager mapManager = MapManager;
        if (mapManager != null)
        {
            mapManager.IsControlEnabled = enabled;
        }
    }

    /// <summary>섹션 시작 시 튜토리얼 전용 오브젝트를 소환합니다.</summary>
    public void Spawn(TutorialSpawnObjectType type)
    {
        if (type != TutorialSpawnObjectType.None && _spawnManager != null)
        {
            _spawnManager.Spawn(type);
        }
    }

    /// <summary>튜토리얼용 보석/골인을 원래 상태로 복구합니다. (골대 = 스테이지 1의 실제 클리어 판정)</summary>
    public void RestoreStageObjects()
    {
        // 보석은 획득 시 StageManager.CollectGem()으로 미션에 반영되므로 비활성 유지.
        _spawnManager?.DespawnAll();
    }

    /// <summary>튜토리얼 골인 = 스테이지 1 클리어이므로 스테이지 완주 판정을 내립니다.</summary>
    public void TriggerStageClear()
    {
        _stageManager?.StageClear();
    }

    public bool WasGemCollected => _spawnManager != null && _spawnManager.WasGemCollected;

    public bool WasGoalReached => _spawnManager != null && _spawnManager.WasGoalReached;

    public bool TryGetGemPosition(out Vector3 position)
    {
        if (_spawnManager != null)
        {
            return _spawnManager.TryGetGemPosition(out position);
        }
        position = default;
        return false;
    }
}
