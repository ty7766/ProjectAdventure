using GameManager.Singleton;
using System.Collections.Generic;
using UnityEngine;

public class StageLoader : Singleton<StageLoader>
{
    [Header("References")]
    [SerializeField]
    private HUDFlowPresenter _hudFlowPresenter;
    [SerializeField]
    private HUDSystemPresenter _hudSystemPresenter;
    [SerializeField]
    private HUDStatusPresenter _hudStatusPresenter;
    [SerializeField]
    private CameraFollow _cameraFollow;
    [SerializeField]
    private PlayerController _playerController;
    [SerializeField]
    private PlayerProperties _playerProperties;
    [SerializeField]
    private MapChangeEffect _mapChangeEffect;

    // stageNumber(1-based) -> pre-instantiated stage instance
    private Dictionary<int, GameObject> _stageInstances = new Dictionary<int, GameObject>();
    private int _currentStageNumber = -1;

    protected override bool PersistAcrossScenes => false;
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        PreloadAllStages();

        int stageNumber = GameSaveManager.Instance.CurrentStageNumber;
        if (stageNumber > 0)
        {
            LoadStage(stageNumber);
        }
    }

    /// <summary>
    /// Activates the specified stage. Deactivates the previous stage and reconnects references.
    /// </summary>
    /// <param name="stageNumber">Stage number to activate (1-based)</param>
    public void LoadStage(int stageNumber)
    {
        if (!_stageInstances.TryGetValue(stageNumber, out GameObject stageInstance))
        {
            CustomDebug.LogError($"[StageLoader] Stage {stageNumber} instance not found.");
            return;
        }

        if (_currentStageNumber > 0 && _stageInstances.TryGetValue(_currentStageNumber, out GameObject current))
        {
            current.SetActive(false);
        }

        stageInstance.SetActive(true);
        _currentStageNumber = stageNumber;

        ConnectReferences(stageInstance);
    }

    /// <summary>
    /// Restarts the current stage from the beginning. Resets state and reconnects references.
    /// </summary>
    public void ReloadCurrentStage()
    {
        if (_currentStageNumber <= 0)
        {
            return;
        }
        if (!_stageInstances.TryGetValue(_currentStageNumber, out GameObject stageInstance))
        {
            return;
        }

        stageInstance.SetActive(false);
        stageInstance.SetActive(true);

        ConnectReferences(stageInstance);

        // CollectGem()으로 개별 비활성화된 GemObject를 재활성화
        // ConnectReferences() 이후 호출하여 _collectedGemIDs 클리어 뒤에 OnEnable이 실행되도록 순서 보장
        foreach (var gem in stageInstance.GetComponentsInChildren<GemObject>(true))
        {
            if (!gem.gameObject.activeSelf)
            {
                gem.gameObject.SetActive(true);
            }
        }
    }

    private void PreloadAllStages()
    {
        int totalStages = GameSaveManager.Instance.GetTotalStageNumber();
        for (int i = 1; i <= totalStages; i++)
        {
            var stageData = GameSaveManager.Instance.GetStageData(i);
            if (stageData == null || stageData.StagePrefab == null)
            {
                CustomDebug.LogError($"[StageLoader] Stage {i} prefab is missing.");
                continue;
            }

            GameObject instance = Instantiate(stageData.StagePrefab);
            instance.SetActive(false);
            _stageInstances[i] = instance;
        }
    }

    private void ConnectReferences(GameObject stageRoot)
    {
        var stageManager = stageRoot.GetComponentInChildren<StageManager>(true);
        if (stageManager == null)
        {
            CustomDebug.LogError("[StageLoader] StageManager not found.");
            return;
        }

        stageManager.SetPlayerController(_playerController);

        var mapManager = stageRoot.GetComponentInChildren<MapManager>(true);
        if (mapManager != null)
        {
            stageManager.SetMapManager(mapManager);
            mapManager.SetMapChangeEffect(_mapChangeEffect);
        }

        // HUD 먼저 구독 등록 (InitializeStage의 이벤트를 놓치지 않기 위해)
        if (_hudStatusPresenter != null) _hudStatusPresenter.Setup(stageManager);
        else CustomDebug.LogError("[StageLoader] HUDStatusPresenter is not assigned.");

        stageManager.InitializeStage();

        _cameraFollow?.SnapToTarget();

        if (_hudFlowPresenter != null)
        {
            _hudFlowPresenter.Setup(stageManager, _playerProperties);
        }
        else
        {
            CustomDebug.LogError("[StageLoader] HUDFlowPresenter is not assigned.");
        }

        if (_hudSystemPresenter != null)
        {
            _hudSystemPresenter.Setup(stageManager);
        }
        else
        {
            CustomDebug.LogError("[StageLoader] HUDSystemPresenter is not assigned.");
        }
    }
}
