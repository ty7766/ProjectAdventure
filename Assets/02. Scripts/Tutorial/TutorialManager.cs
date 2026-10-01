using System;
using System.Collections;
using GameManager.Singleton;
using UnityEngine;

/// <summary>
/// Apex Training Mission 스타일 튜토리얼 매니저.
/// 섹션(큰 단위) → 목표 체크리스트로 진행하며, 같은 섹션 안의 목표는
/// 순서에 상관 없이 어떤 것이든 먼저 수행하면 체크된다.
/// 모든 목표가 체크되면 다음 섹션으로 넘어간다.
/// 스테이지 상태 조작(타이머/조작/스폰/클리어)은 <see cref="TutorialStageBridge"/>에 위임한다.
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
    private TutorialStageBridge _stage;
    private MapManager _subscribedMapManager;
    private Action _onCompleteCallback;
    private Coroutine _transitionCoroutine;
    private bool _isSectionTransitioning;
    private bool _hasFoundGem;
    private bool _hasCollectedGem;

    /// <summary>튜토리얼 중 골인 지점 도달 여부</summary>
    public bool HasReachedGoal { get; private set; }

    /// <summary>튜토리얼 진행 중 여부 (HUD 등에서 입력 차단 확인용)</summary>
    public static bool IsActive { get; private set; }

    //--- Unity Methods ---//
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsActive = false;
    }

    private void Awake()
    {
        _eventBus = new TutorialEventBus();
        _eventBus.GemFound += HandleGemEvent;
        _eventBus.GemCollected += HandleGemCollected;
        _eventBus.GoalReached += HandleGoalReached;
    }

    private void OnDisable()
    {
        // 씬 언로드(타이틀 복귀 등)로 튜토리얼 도중 파괴되어도 static 상태가 남지 않도록 정리
        if (IsActive)
        {
            EndSession();
        }
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
    /// 진행 중이던 튜토리얼이 있으면(스테이지 재시작 등) 먼저 중단하고 처음부터 다시 시작한다.
    /// </summary>
    /// <param name="stageManager">현재 활성화된 스테이지의 StageManager</param>
    public void StartTutorial(StageManager stageManager, Action onComplete)
    {
        AbortTutorial();

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

        _stage = new TutorialStageBridge(stageManager, _spawnManager, _eventBus);
        _stage.PreparePlayEnvironment();
        PrepareInputNode();
        SubscribeMapManager(_stage.MapManager);

        _tutorialView.gameObject.SetActive(true);
        ShowSection(0);
    }

    /// <summary>
    /// 진행 중인 튜토리얼을 완료 처리 없이 즉시 중단합니다. (스테이지 재시작/이탈 시)
    /// 완료 콜백은 호출하지 않으며, 진행 중이 아니면 아무것도 하지 않는다.
    /// </summary>
    public void AbortTutorial()
    {
        if (!IsActive && _transitionCoroutine == null)
        {
            return;
        }

        EndSession();
        _onCompleteCallback = null;

        _cameraController.RestoreFollow();
        _tutorialView.HideImmediate();
        _tutorialView.gameObject.SetActive(false);

        // 중단된 스테이지에서도 골대가 비활성으로 남지 않도록 복구 (재시작 시 다시 숨김)
        _stage?.RestoreStageObjects();
    }

    //--- Private Methods ---//
    /// <summary>진행 상태(정적 플래그/코루틴/입력/구독)를 정리합니다. 뷰/카메라는 건드리지 않는다.</summary>
    private void EndSession()
    {
        IsActive = false;
        _isSectionTransitioning = false;
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
        if (_inputNode != null)
        {
            _inputNode.Enabled = false;
        }
        UnsubscribeMapManager();
    }

    private void PrepareInputNode()
    {
        if (_inputNode == null)
        {
            _inputNode = new TutorialInputNode(InputManager.Instance.GameControls);
            _inputNode.OnMoveInDirection += HandleMoveDirection;
        }
    }

    private void SubscribeMapManager(MapManager mapManager)
    {
        // 중복 구독 방지: 이전 구독을 먼저 해제한다 (스테이지 인스턴스가 재사용되므로 같은 MapManager일 수 있음)
        UnsubscribeMapManager();
        if (mapManager == null)
        {
            return;
        }
        _subscribedMapManager = mapManager;
        _subscribedMapManager.OnSelectionChanged += HandleMapSelected;
        _subscribedMapManager.OnMapSwapped += HandleMapSwapped;
    }

    private void UnsubscribeMapManager()
    {
        if (_subscribedMapManager == null)
        {
            return;
        }
        _subscribedMapManager.OnSelectionChanged -= HandleMapSelected;
        _subscribedMapManager.OnMapSwapped -= HandleMapSwapped;
        _subscribedMapManager = null;
    }

    private void ShowSection(int index)
    {
        _sectionIndex = index;
        TutorialSectionConfig section = _sections[index];
        _objectiveDone = new bool[section.Objectives.Length];
        _isSectionTransitioning = false;

        // 섹션 시작 스폰 처리 (보석/골인은 이 시점에 '소환'된다)
        _stage.Spawn(section.SpawnOnStart);

        // 맵 조작이 필요한 섹션이면 MapManager 입력을 연다
        _stage.SetMapControlEnabled(RequiresMapControl(section));

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
                        if (!IsActive)
                        {
                            return; // 카메라 이동 중 중단된 경우
                        }
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

            _transitionCoroutine = null;
            ShowSection(_sectionIndex + 1);
        }
        else
        {
            _transitionCoroutine = null;
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
        if (_stage != null && _stage.WasGemCollected)
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

        if (_hasCollectedGem || (_stage != null && _stage.WasGemCollected))
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
        if (_stage != null && _stage.WasGoalReached)
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

    /// <summary>보석 '발견' 판정: 플레이어가 보석 근처에 도달했는지 확인합니다.</summary>
    private void PollGemFind()
    {
        int findIndex = FindFirstUndone(TutorialCompletionType.FindGem);
        if (findIndex < 0)
        {
            return; // 이미 발견 완료
        }
        Transform player = _stage?.PlayerTransform;
        if (player == null || !_stage.TryGetGemPosition(out Vector3 gemPos))
        {
            return;
        }

        float sqrDistance = (player.position - gemPos).sqrMagnitude;
        if (sqrDistance <= _gemFindRadius * _gemFindRadius)
        {
            HandleGemEvent();
        }
    }

    private void CompleteTutorial()
    {
        EndSession();
        _cameraController.RestoreFollow();
        _tutorialView.HideAll();
        _tutorialView.gameObject.SetActive(false);

        // 튜토리얼용 보석/골인을 원래 상태로 복구한다.
        _stage.RestoreStageObjects();

        GameSaveManager.Instance.SetTutorialCompleted();

        // 골인 목표가 있는 튜토리얼에서 실제로 골인했다면 곧 스테이지 완주 판정을 내린다
        // (골대 = 스테이지 1의 클리어 판정 오브젝트. 목표 배치 순서와 무관하게 도달 여부로 판단)
        if (HasReachedGoal && ContainsGoalObjective())
        {
            _stage.TriggerStageClear();
        }

        Action callback = _onCompleteCallback;
        _onCompleteCallback = null;
        callback?.Invoke();
    }

    /// <summary>튜토리얼 섹션 중 골인 도달 목표가 포함되어 있는지 확인합니다.</summary>
    private bool ContainsGoalObjective()
    {
        foreach (TutorialSectionConfig section in _sections)
        {
            foreach (TutorialObjectiveConfig objective in section.Objectives)
            {
                if (objective.CompletionType == TutorialCompletionType.ReachGoal)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
