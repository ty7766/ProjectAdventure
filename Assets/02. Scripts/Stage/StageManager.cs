using GameManager.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum  StageObjectType
{
    NoDamageClear,
    NoFallClear,
    TimeLimitClear,
    RemainHealthClear,
    CollectGemsClear
}

[Serializable]
public class StageObject
{
    public StageObjectType stageObjectType;
    public int value;
    public bool isCleared = false;
}

public class StageManager : MonoBehaviour
{
    //--- Components & Settings ---//
    [Header("Components")]
    [SerializeField]
    private PlayerController _playerController;
    private MapManager _mapManager;

    [Header("Stage Settings")]
    [SerializeField] 
    private int _requiredGemsToClear = 1;
    [SerializeField] 
    private Transform _playerStartPosition;
    [SerializeField]
    private float _playerStartPositionOffset;

    [Header("Stage Objects")]
    [SerializeField]
    private List<StageObject> _stageObjects;

    //--- Fields ---//
    private int _collectedGems = 0;
    private Coroutine _timeScaleCoroutine;
    private float _initialFixedDeltaTime;
    private float _stageTimer = 0f;
    private bool _isTimerRunning = true;
    private bool _isInitialized = false;

    private HashSet<string> _collectedGemIDs = new HashSet<string>();

    //--- Stage Objeect related States ---//
    private bool _isPlayerDamageTaken = false;
    private bool _isPlayerFallenDown = false;

    //--- Events ---//
    public event Action<int, int> OnGemCountChanged;
    public event Action OnStageCleared;

    //--- Properties ---// 
    public float StageTimer => _stageTimer;
    public bool IsTimerRunning => _isTimerRunning;
    public List<StageObject> StageObjects => _stageObjects;
    public PlayerController PlayerController => _playerController;

    //--- Unity Methods ---//
    private void Awake()
    {
        SubscribeEvents();
        LoadStageObjectStatusFromSave();
    }

    private void OnDestroy() 
    {
        if (_playerController != null)
        {
            _playerController.OnPlayerDamageTaken -= HandlePlayerDamageTakenEvent;
            _playerController.OnPlayerFallenDown -= HandlePlayerFallenDownEvent;
        }
        ResumeGameImmediately(); //스테이지 매니저 파괴 시 타임 스케일 복구
    }

    private void Start()
    {
        // StageLoader.InitializeStage()가 먼저 호출된 경우 중복 실행 방지
        if (!_isInitialized)
        {
            InitializeStage();
        }
    }

    private void Update()
    {
        TimerTick();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 스테이지 부가 목표인 보석을 수집합니다.
    /// </summary>
    public void CollectGem(string gemID)
    {
        if (string.IsNullOrEmpty(gemID))
        {
            return;
        }
        if (_collectedGemIDs.Add(gemID))
        {
            _collectedGems++;
            OnGemCountChanged?.Invoke(_collectedGems, _requiredGemsToClear);
        }
    }

    public void StageClear()
    {
        ClearStage();
    }

    public void StartStage()
    {
        _isTimerRunning = true;
        _stageTimer = 0f;
        ResumeGameSmoothly();
        EnablePlayerControl();
        CheckStageObject();
    }

    /// <summary>
    /// 게임을 부드럽게 일시정지합니다.
    /// </summary>
    /// <param name="duration"></param>
    public void PauseGameSmoothly(float duration = 0.5f) => SmoothTimeScale(0f, duration);

    /// <summary>
    /// 게임을 부드럽게 재개합니다.
    /// </summary>
    /// <param name="duration"></param>
    public void ResumeGameSmoothly(float duration = 0.5f) => SmoothTimeScale(1f, duration);

    public void ResumeGameImmediately()
    {
        Time.timeScale = 1f;
        if(_initialFixedDeltaTime > 0)
        {
            Time.fixedDeltaTime = _initialFixedDeltaTime;
        }
    }

    /// <summary>
    /// 타임스케일을 부드럽게 변경합니다.
    /// </summary>
    /// <param name="targetScale">목표 타임스케일 (0f = 정지, 1f = 정상)</param>
    /// <param name="duration">변화에 걸리는 시간(초)</param>
    public void SmoothTimeScale(float targetScale, float duration)
    {
        if (_timeScaleCoroutine != null)
            StopCoroutine(_timeScaleCoroutine);

        _timeScaleCoroutine = StartCoroutine(ChangeTimeScale(targetScale, duration));
    }

    /// <summary>
    /// 해당 gemID를 가지고 있는 Gem이 수집 되었는지 확인하는 메소드
    /// </summary>
    /// <param name="gemID"></param>
    /// <returns></returns>
    public bool IsGemCollected(string gemID)
    {
        if (string.IsNullOrEmpty(gemID))
        {
            return false;
        }
        return _collectedGemIDs.Contains(gemID);
    }

    /// <summary>
    /// PlayerController 레퍼런스를 외부에서 주입합니다. 기존 이벤트 구독을 해제하고 새 컨트롤러로 재구독합니다.
    /// </summary>
    /// <param name="controller">연결할 PlayerController</param>
    public void SetPlayerController(PlayerController controller)
    {
        if(_playerController != null)
        {
            _playerController.OnPlayerDamageTaken -= HandlePlayerDamageTakenEvent;
            _playerController.OnPlayerFallenDown -= HandlePlayerFallenDownEvent;
        }
        _playerController = controller;
        SubscribeEvents();
    }

    /// <summary>
    /// MapManager 레퍼런스를 외부에서 주입합니다.
    /// </summary>
    /// <param name="mapManager">연결할 MapManager</param>
    public void SetMapManager(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// 스테이지 상태를 초기화하고 게임을 일시정지 후 플레이어 조작을 비활성화합니다.
    /// StageLoader가 스테이지를 활성화할 때 호출됩니다.
    /// </summary>
    public void InitializeStage()
    {
        _isInitialized = true;

        _collectedGems = 0;
        _collectedGemIDs.Clear();
        _isPlayerDamageTaken = false;
        _isPlayerFallenDown = false;
        _stageTimer = 0f;
        _isTimerRunning = false;
        _initialFixedDeltaTime = Time.fixedDeltaTime;

        if (_playerController != null && _playerStartPosition != null)
        {
            Vector3 startPos = _playerStartPosition.position + Vector3.up * _playerStartPositionOffset;
            _playerController.PlaceAtStartPosition(startPos);
        }

        OnGemCountChanged?.Invoke(_collectedGems, _requiredGemsToClear);
        PauseGameSmoothly();
        DisablePlayerControl();
        GraphicManager.Instance?.SetDoFMode("Stage");
    }

    //--- Private Helpers ---//
    private void SubscribeEvents()
    {
        if (_playerController != null)
        {
            _playerController.OnPlayerDamageTaken += HandlePlayerDamageTakenEvent;
            _playerController.OnPlayerFallenDown += HandlePlayerFallenDownEvent;
        }
    }

    private IEnumerator ChangeTimeScale(float targetScale, float duration)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float currentScale = Mathf.Lerp(startScale, targetScale, t);

            Time.timeScale = currentScale;
            Time.fixedDeltaTime = _initialFixedDeltaTime * currentScale;

            yield return null;
        }

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = _initialFixedDeltaTime * targetScale;

        if (Time.timeScale <= 0)
        {
            Time.fixedDeltaTime = _initialFixedDeltaTime;
        }
    }

    private void ClearStage()
    {
        DisablePlayerControl();

        CheckStageObject();
        OnStageCleared?.Invoke();
        SoundManager.Instance?.PlaySFX(SoundType.SFX_ClearUI);

        SaveStageClearData();
    }

    private void CheckStageObject()
    {
        foreach (var obj in _stageObjects)
        {
            switch (obj.stageObjectType)
            {
                case StageObjectType.NoDamageClear:
                    obj.isCleared = !_isPlayerDamageTaken;
                    break;

                case StageObjectType.NoFallClear:
                    obj.isCleared = !_isPlayerFallenDown;
                    break;

                case StageObjectType.TimeLimitClear:
                    obj.isCleared = (_stageTimer <= obj.value);
                    break;

                case StageObjectType.RemainHealthClear:
                    obj.isCleared = _playerController != null && _playerController.Health >= obj.value;
                    break;

                case StageObjectType.CollectGemsClear:
                    obj.isCleared = _collectedGems >= obj.value;
                    break;
            }
        }
    }

    private void DisablePlayerControl()
    {
        if(_playerController == null)
        {
            CustomDebug.LogWarning("PlayerController reference is missing in StageManager. Cannot disable player control.");
            return;
        }

        _playerController.DisablePlayerControl();

        if (_mapManager == null)
        {
            CustomDebug.LogWarning("MapManager reference is missing in StageManager. Map control may not be disabled.");
        }
        else
        {
            _mapManager.IsControlEnabled = false;
        }
    }

    private void EnablePlayerControl()
    {
        if (_playerController == null)
        {
            CustomDebug.LogWarning("PlayerController reference is missing in StageManager. Cannot enable player control.");
            return;
        }

        _playerController.EnablePlayerControl();

        if (_mapManager == null)
        {
            CustomDebug.LogWarning("MapManager reference is missing in StageManager. Map control may not be enabled.");
        }
        else
        {
            _mapManager.IsControlEnabled = true;
        }
    }

    private void TimerTick()
    {
        if (_isTimerRunning)
        {
            _stageTimer += Time.deltaTime;
        }
    }

    private void HandlePlayerDamageTakenEvent()
    {
        _isPlayerDamageTaken = true;
    }

    private void HandlePlayerFallenDownEvent()
    {
        _isPlayerFallenDown = true;
    }

    private void LoadStageObjectStatusFromSave()
    {
        foreach(var obj in _stageObjects)
        {
            //TODO : 저장된 클리어 상태 불러오기

        }
    }

    private void SaveStageClearData()
    {
        int acquiredStars = 0;
        foreach(var obj in _stageObjects)
        {
            if (obj.isCleared)
            {
                acquiredStars++;
            }
        }
        GameSaveManager.Instance?.RecordStageClear(acquiredStars);
    }
}
