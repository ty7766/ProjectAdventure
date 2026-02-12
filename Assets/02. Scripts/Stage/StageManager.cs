using GameManager.Singleton;
using System;
using System.Collections;
using UnityEngine;

public enum  StageObjectType
{
    NoDamageClear,
    NoFallClear,
    TimeLimitClear,
    RemainHealthClear
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
    [SerializeField] private PlayerController _playerController;

    [Header("Stage Settings")]
    [SerializeField] private int _requiredGemsToClear = 1;

    [Header("Stage Objects")]
    [SerializeField] private StageObject[] _stageObjects;

    //--- Fields ---//
    private int _collectedGems = 0;
    private Coroutine _timeScaleCoroutine;
    private float _initialFixedDeltaTime;
    private float _stageTimer = 0f;
    private bool _isTimerRunning = true;

    //--- Stage Objeect related States ---//
    private bool _isPlayerDamageTaken = false;
    private bool _isPlayerFallenDown = false;

    //--- Events ---//
    public event Action<int, int> OnGemCountChanged;
    public event Action OnStageCleared;

    //--- Properties ---// 
    public float StageTimer => _stageTimer;
    public bool IsTimerRunning => _isTimerRunning;
    public StageObject[] StageObjects => _stageObjects;

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
    }

    private void Start()
    {
        OnGemCountChanged?.Invoke(_collectedGems, _requiredGemsToClear);
        _initialFixedDeltaTime = Time.fixedDeltaTime;
        PauseGameSmoothly();
        DisablePlayerControl(); //스테이지 시작 전에는 플레이어 움직임 비활성화
    }

    private void Update()
    {
        TimerTick();
    }

    //--- Public Methods ---//
    /// <summary>
    /// 스테이지 클리어 조건인 보석 수집을 처리하고 클리어 목표를 달성한 경우 스테이지를 클리어합니다.
    /// </summary>
    public void CollectGem()
    {
        _collectedGems++;
        OnGemCountChanged?.Invoke(_collectedGems, _requiredGemsToClear);
        if (_collectedGems >= _requiredGemsToClear)
        {
            ClearStage();
        }
    }

    public void StartStage()
    {
        _isTimerRunning = true;
        _stageTimer = 0f;
        ResumeGameSmoothly();
        EnablePlayerControl();
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

        SaveStageClearData();
    }

    private void CheckStageObject()
    {
        foreach (var obj in _stageObjects)
        {
            if (obj.isCleared)
            {
                continue; //이미 클리어된 도전과제는 건너뜀
            }
            switch (obj.stageObjectType)
            {
                case StageObjectType.NoDamageClear:
                    if (!_isPlayerDamageTaken)
                    {
                        obj.isCleared = true;
                    }
                    break;

                case StageObjectType.NoFallClear:
                    if (!_isPlayerFallenDown)
                    {
                        obj.isCleared = true;
                    }
                    break;

                case StageObjectType.TimeLimitClear:
                    if (_stageTimer <= obj.value)
                    {
                        obj.isCleared = true;
                    }
                    break;

                case StageObjectType.RemainHealthClear:
                    if (_playerController != null && _playerController.Health >= obj.value)
                    {
                        obj.isCleared = true;
                    }
                    break;
            }
        }
    }

    private void DisablePlayerControl()
    {
        if (_playerController != null)
        {
            _playerController.DisablePlayerControl();
        }
        else
        {
            CustomDebug.LogWarning("PlayerController reference is missing in StageManager.");
        }
    }

    private void EnablePlayerControl()
    {
        if (_playerController != null)
        {
            _playerController.EnablePlayerControl();
        }
        else
        {
            CustomDebug.LogWarning("PlayerController reference is missing in StageManager.");
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
        if(GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.RecordStageClear(acquiredStars);
        }
        else
        {
            CustomDebug.LogWarning($"게임 세이브 매니저가 초기화 되어 있지 않아 세이브 할 수 없습니다.");
        }
    }
}
