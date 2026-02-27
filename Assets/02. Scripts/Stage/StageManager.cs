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
    [SerializeField] private PlayerController _playerController;

    [Header("Stage Settings")]
    [SerializeField] private int _requiredGemsToClear = 1;

    [Header("Stage Objects")]
    [SerializeField] private List<StageObject> _stageObjects;

    //--- Fields ---//
    private int _collectedGems = 0;
    private Coroutine _timeScaleCoroutine;
    private float _initialFixedDeltaTime;
    private float _stageTimer = 0f;
    private bool _isTimerRunning = true;

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
        SoundManager.Instance.PlaySFX(SoundType.SFX_ClearUI);

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
                    if (_playerController != null && _playerController.Health >= obj.value)
                    {
                        obj.isCleared = true;
                    }
                    else
                    {
                        obj.isCleared = false;
                    }
                    break;

                case StageObjectType.CollectGemsClear:
                    if(_collectedGems >= obj.value)
                    {
                        obj.isCleared = true;
                    }
                    else
                    {
                        obj.isCleared = false;
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
        GameSaveManager.Instance.RecordStageClear(acquiredStars);
    }
}
