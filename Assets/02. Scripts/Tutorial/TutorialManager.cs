using System;
using System.Collections;
using GameManager.Singleton;
using UnityEngine;

/// <summary>
/// Apex Training Mission 스타일 튜토리얼 매니저.
/// 섹션(큰 단위) → 목표 체크리스트로 진행하며, 같은 섹션 안의 목표는
/// 순서에 상관 없이 어떤 것이든 먼저 수행하면 체크된다.
/// 모든 목표가 체크되면 다음 섹션으로 넘어간다.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("튜토리얼 설정")]
    [SerializeField]
    private TutorialSectionConfig[] _sections;
    [SerializeField]
    private TutorialView _tutorialView;
    [SerializeField]
    private TutorialCameraController _cameraController;
    [SerializeField]
    private TutorialSpawnManager _spawnManager;
    [SerializeField, Tooltip("보석 '발견' 판정 반경 (보석 스폰 지점 기준)")]
    private float _gemFindRadius = 4f;
    [SerializeField, Tooltip("섹션 완료 후 다음 섹션 전환 대기 시간(초)")]
    private float _sectionTransitionDelay = 0.9f;
    [SerializeField, Tooltip("패널 슬라이드 아웃 후 다음 섹션 표시까지 추가 대기 시간(초)")]
    private float _transitionGap = 0.1f;

    //--- Fields ---//
    private int _sectionIndex;
    private bool[] _objectiveDone;
    private TutorialInputNode _inputNode;
    private TutorialEventBus _eventBus;
    private Action _onCompleteCallback;
    private StageManager _stageManager;
    private MapManager _mapManager;
    private Transform _playerTransform;
    private Coroutine _transitionCoroutine;
    private bool _isSectionTransitioning;
    private bool _hasFoundGem;
    private bool _hasCollectedGem;

    /// <summary>튜토리얼 중 골인 지점 도달 여부</summary>
    public bool HasReachedGoal { get; private set; }

    /// <summary>튜토리얼 진행 중 여부 (HUD 등에서 입력 차단 확인용)</summary>
    public static bool IsActive { get; private set; }

    //--- Unity Methods ---//
    private void Awake()
    {
        _eventBus = new TutorialEventBus();
        _eventBus.GemFound += HandleGemEvent;
        _eventBus.GemCollected += HandleGemCollected;
        _eventBus.GoalReached += HandleGoalReached;
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.GemFound -= HandleGemEvent;
            _eventBus.GemCollected -= HandleGemCollected;
            _eventBus.GoalReached -= HandleGoalReached;
        }
        UnsubscribeMapManager();
        _inputNode?.Dispose();
        _inputNode = null;
    }

    private void Update()
    {
        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        _inputNode?.Tick();
        PollGemFind();
        SyncGemObjectives();
        SyncGoalObjectives();
        CheckSectionCompletion();
    }

    //--- Public Methods ---//
    /// <summary>튜토리얼 섹션 구성을 교체합니다. (에디터 인스톨러용)</summary>
    public void SetSections(TutorialSectionConfig[] sections)
    {
        _sections = sections;
    }

    /// <summary>
    /// 현재 씬에서 튜토리얼을 표시해야 하는지 확인합니다.
    /// </summary>
    public bool ShouldShowTutorial()
    {
        return GameSaveManager.Instance.CurrentStageNumber == 1
            && !GameSaveManager.Instance.HasCompletedTutorial();
    }

    /// <summary>
    /// 튜토리얼을 시작합니다. 완료 시 onComplete 콜백이 호출됩니다.
    /// </summary>
    public void StartTutorial(Action onComplete)
    {
        if (_sections == null || _sections.Length == 0)
        {
            CustomDebug.LogWarning("TutorialManager: 등록된 섹션이 없습니다.");
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _sectionIndex = 0;
        _isSectionTransitioning = false;
        _hasFoundGem = false;
        _hasCollectedGem = false;
        HasReachedGoal = false;
        IsActive = true;

        PreparePlayEnvironment();
        PrepareInputNode();
        SubscribeMapManager();

        _tutorialView.gameObject.SetActive(true);
        ShowSection(0);
    }

    //--- Private Methods ---//
    /// <summary>
    /// 튜토리얼 동안 플레이어가 실제로 조작할 수 있게 게임 상태를 푼다.
    /// (InitializeStage에서 Pause + 조작 비활성 상태로 들어오기 때문)
    /// </summary>
    private void PreparePlayEnvironment()
    {
        _stageManager = FindAnyObjectByType<StageManager>();
        _mapManager = FindAnyObjectByType<MapManager>();

        if (_stageManager != null)
        {
            // 튜토리얼은 스테이지 1의 실제 플레이 구간이다.
            // 시간이 흐르도록 타이머를 가동해 시간 제한 등 부가 목표 판정이 유효해진다.
            _stageManager.StartStageTimer();
            _stageManager.ResumeGameSmoothly();
            _stageManager.PlayerController?.EnablePlayerControl();
            _playerTransform = _stageManager.PlayerController != null
                ? _stageManager.PlayerController.transform
                : null;
        }
        else
        {
            CustomDebug.LogWarning("TutorialManager: StageManager를 찾지 못했습니다. 조작 활성화가 누락될 수 있습니다.");
        }

        // 스테이지 내 튜토리얼용 보석/골인 오브젝트를 탐색해 비활성 대기시킨다.
        // 스테이지 루트 기준으로 검색해야 한다 (StageManager/MapManager 하위가 아님).
        if (_spawnManager != null && _stageManager != null)
        {
            _spawnManager.Setup(_eventBus);
            _spawnManager.SetStageManager(_stageManager);
            Transform stageRoot = _stageManager.transform.root;
            _spawnManager.BindStageObjects(stageRoot);
        }
    }

    private void PrepareInputNode()
    {
        if (_inputNode == null)
        {
            _inputNode = new TutorialInputNode(InputManager.Instance.GameControls);
            _inputNode.OnMoveInDirection += HandleMoveDirection;
        }
    }

    private void SubscribeMapManager()
    {
        if (_mapManager == null)
        {
            return;
        }
        _mapManager.OnSelectionChanged += HandleMapSelected;
        _mapManager.OnMapSwapped += HandleMapSwapped;
    }

    private void UnsubscribeMapManager()
    {
        if (_mapManager == null)
        {
            return;
        }
        _mapManager.OnSelectionChanged -= HandleMapSelected;
        _mapManager.OnMapSwapped -= HandleMapSwapped;
    }

    private void ShowSection(int index)
    {
        _sectionIndex = index;
        TutorialSectionConfig section = _sections[index];
        _objectiveDone = new bool[section.Objectives.Length];
        _isSectionTransitioning = false;

        // 섹션 시작 스폰 처리 (보석/골인은 이 시점에 '소환'된다)
        if (section.SpawnOnStart != TutorialSpawnObjectType.None && _spawnManager != null)
        {
            _spawnManager.Spawn(section.SpawnOnStart);
        }

        // 맵 조작이 필요한 섹션이면 MapManager 입력을 연다
        if (_mapManager != null)
        {
            _mapManager.IsControlEnabled = RequiresMapControl(section);
        }

        // 이동 입력 감지는 이동 섹션에서만 켠다
        _inputNode.Enabled = RequiresMoveInput(section);

        // 카메라 포커싱 (앵커 지정 시)
        if (!string.IsNullOrEmpty(section.FocusAnchorID))
        {
            Transform anchor = TutorialAnchorRegistry.GetTransform(section.FocusAnchorID);
            if (anchor != null)
            {
                _tutorialView.HidePanel();
                _cameraController.MoveToTarget(anchor,
                    () =>
                    {
                        _tutorialView.ShowSection(_sections, _sectionIndex);
                        SyncGemObjectives();
                        SyncGoalObjectives();
                    });
                return;
            }
            CustomDebug.LogWarning($"TutorialManager: '{section.FocusAnchorID}'를 찾지 못하여 카메라 이동 없이 진행합니다");
        }

        _tutorialView.ShowSection(_sections, _sectionIndex);
        SyncGemObjectives();
        SyncGoalObjectives();
    }

    /// <summary>이 섹션에서 MapManager 조작(선택/교체)이 필요한지 판별합니다.</summary>
    private static bool RequiresMapControl(TutorialSectionConfig section)
    {
        foreach (TutorialObjectiveConfig objective in section.Objectives)
        {
            switch (objective.CompletionType)
            {
                case TutorialCompletionType.SelectMap:
                case TutorialCompletionType.ChangeMap:
                case TutorialCompletionType.FindGem:
                case TutorialCompletionType.ReachGoal:
                    return true;
            }
        }
        return false;
    }

    /// <summary>이 섹션에서 이동 입력 감지가 필요한지 판별합니다.</summary>
    private static bool RequiresMoveInput(TutorialSectionConfig section)
    {
        foreach (TutorialObjectiveConfig objective in section.Objectives)
        {
            if (objective.CompletionType == TutorialCompletionType.MoveInDirection)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>섹션의 모든 목표가 완료됐는지 확인하고, 완료 시 다음 섹션으로 전환합니다.</summary>
    private void CheckSectionCompletion()
    {
        foreach (bool done in _objectiveDone)
        {
            if (!done)
            {
                return;
            }
        }

        if (_transitionCoroutine != null)
        {
            return;
        }
        _isSectionTransitioning = true;
        _transitionCoroutine = StartCoroutine(SectionTransitionRoutine());
    }

    private IEnumerator SectionTransitionRoutine()
    {
        yield return new WaitForSecondsRealtime(_sectionTransitionDelay);

        _transitionCoroutine = null;

        if (_sectionIndex + 1 < _sections.Length)
        {
            // 먼저 패널 전체를 화면 밖으로 슬라이드 아웃 → 완료 후 다음 섹션 표시
            bool slideOutDone = false;
            _tutorialView.HidePanel(() => slideOutDone = true);
            while (!slideOutDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(_transitionGap);

            ShowSection(_sectionIndex + 1);
        }
        else
        {
            CompleteTutorial();
        }
    }

    /// <summary>현재 섹션에서 아직 완료되지 않은 목표 중 completionType과 일치하는 첫 번째 목표 인덱스를 찾습니다.</summary>
    private int FindFirstUndone(TutorialCompletionType type)
    {
        TutorialSectionConfig section = _sections[_sectionIndex];
        for (int i = 0; i < section.Objectives.Length; i++)
        {
            if (!_objectiveDone[i] && section.Objectives[i].CompletionType == type)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>목표를 완료 처리합니다. (이미 완료된 경우 무시)</summary>
    private void CompleteObjectiveAt(int index)
    {
        if (index < 0 || _objectiveDone[index])
        {
            return;
        }

        _objectiveDone[index] = true;
        _tutorialView.SetObjectiveCompleted(index);
        SoundManager.Instance?.PlaySFX(SoundType.SFX_ButtonClick);
    }

    //--- Event Handlers ---//
    private void HandleMoveDirection(TutorialMoveDirection direction)
    {
        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        TutorialSectionConfig section = _sections[_sectionIndex];
        for (int i = 0; i < section.Objectives.Length; i++)
        {
            if (_objectiveDone[i])
            {
                continue;
            }

            TutorialObjectiveConfig obj = section.Objectives[i];
            if (obj.CompletionType == TutorialCompletionType.MoveInDirection && obj.MoveDirection == direction)
            {
                CompleteObjectiveAt(i);
                return;
            }
        }
    }

    private void HandleMapSelected(int direction)
    {
        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        TutorialSectionConfig section = _sections[_sectionIndex];
        for (int i = 0; i < section.Objectives.Length; i++)
        {
            if (_objectiveDone[i])
            {
                continue;
            }

            TutorialObjectiveConfig obj = section.Objectives[i];
            if (obj.CompletionType != TutorialCompletionType.SelectMap)
            {
                continue;
            }
            // SelectDirection 0 = 아무 방향 허용
            if (obj.SelectDirection == 0 || obj.SelectDirection == direction)
            {
                CompleteObjectiveAt(i);
                return;
            }
        }
    }

    private void HandleMapSwapped(int direction)
    {
        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        int index = FindFirstUndone(TutorialCompletionType.ChangeMap);
        if (index >= 0)
        {
            CompleteObjectiveAt(index);
        }
    }

    private void HandleGemCollected()
    {
        _hasCollectedGem = true;
        HandleGemEvent();
    }

    private void HandleGemEvent()
    {
        _hasFoundGem = true;
        if (_spawnManager != null && _spawnManager.WasGemCollected)
        {
            _hasCollectedGem = true;
        }

        SyncGemObjectives();
    }

    private void SyncGemObjectives()
    {
        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        int findIndex = FindFirstUndone(TutorialCompletionType.FindGem);
        int collectIndex = FindFirstUndone(TutorialCompletionType.CollectGem);

        if (_hasCollectedGem || (_spawnManager != null && _spawnManager.WasGemCollected))
        {
            // 보석 획득 시 대기 중인 발견 및 수집 목표 모두 완료 처리
            if (findIndex >= 0)
            {
                CompleteObjectiveAt(findIndex);
            }
            if (collectIndex >= 0)
            {
                CompleteObjectiveAt(collectIndex);
            }
        }
        else if (_hasFoundGem && findIndex >= 0)
        {
            CompleteObjectiveAt(findIndex);
        }
    }

    private void HandleGoalReached()
    {
        HasReachedGoal = true;
        SyncGoalObjectives();
    }

    private void SyncGoalObjectives()
    {
        if (_spawnManager != null && _spawnManager.WasGoalReached)
        {
            HasReachedGoal = true;
        }

        if (!IsActive || _isSectionTransitioning)
        {
            return;
        }

        if (HasReachedGoal)
        {
            int index = FindFirstUndone(TutorialCompletionType.ReachGoal);
            if (index >= 0)
            {
                CompleteObjectiveAt(index);
            }
        }
    }

    /// <summary>
    /// 튜토리얼 골인 직후 호출되어, 해당 접촉을 곧 스테이지 완주로 이어준다.
    /// (골대 = EndFlag이므로 튜토리얼 골인 = 스테이지 1 클리어)
    /// </summary>
    private void TriggerStageClearAfterTutorial()
    {
        if (_stageManager != null)
        {
            _stageManager.StageClear();
        }
    }

    /// <summary>보석 '발견' 판정: 플레이어가 보석 근처에 도달했는지 확인합니다.</summary>
    private void PollGemFind()
    {
        int findIndex = FindFirstUndone(TutorialCompletionType.FindGem);
        if (findIndex < 0)
        {
            return; // 이미 발견 완료
        }
        if (_playerTransform == null || _spawnManager == null || !_spawnManager.TryGetGemPosition(out Vector3 gemPos))
        {
            return;
        }

        float sqrDistance = (_playerTransform.position - gemPos).sqrMagnitude;
        if (sqrDistance <= _gemFindRadius * _gemFindRadius)
        {
            HandleGemEvent();
        }
    }

    private void CompleteTutorial()
    {
        IsActive = false;
        _cameraController.RestoreFollow();
        _tutorialView.HideAll();
        _tutorialView.gameObject.SetActive(false);

        // 튜토리얼용 보석/골인을 원래 상태로 복구한다.
        // (스테이지 1은 튜토리얼과 동일 스테이지이므로 골인 지점이 곧 스테이지 완주 판정이다)
        if (_spawnManager != null)
        {
            // 보석은 획득 시 StageManager.CollectGem()으로 미션에 반영되므로 비활성 유지.
            _spawnManager.DespawnAll();
        }

        GameSaveManager.Instance.SetTutorialCompleted();

        // 튜토리얼 골인 목표가 완료된 상태로 튜토리얼이 끝났다면 곧 스테이지 완주 판정을 내린다
        if (_objectiveDone != null && _sections != null
            && _sectionIndex == _sections.Length - 1
            && LastSectionContainsGoal()
            && _objectiveDone[_objectiveDone.Length - 1])
        {
            TriggerStageClearAfterTutorial();
        }

        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }

    /// <summary>마지막 섹션에 골인 도달 목표가 포함되어 있는지 확인합니다.</summary>
    private bool LastSectionContainsGoal()
    {
        foreach (TutorialObjectiveConfig objective in _sections[^1].Objectives)
        {
            if (objective.CompletionType == TutorialCompletionType.ReachGoal)
            {
                return true;
            }
        }
        return false;
    }
}
