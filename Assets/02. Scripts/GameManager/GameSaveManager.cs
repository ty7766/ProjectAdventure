using System.Collections.Generic;
using System.Collections;
using System.IO;
using Utils.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL || UNITY_EDITOR
using ArcadeBackend;
#endif

namespace GameManager.Singleton
{
    public class GameSaveManager : Singleton<GameSaveManager>
    {
        [SerializeField] 
        private List<StageData> _stageDataBase;
        [SerializeField] 
        private List<StageSaveRecord> _saveData;

        private bool _hasCompletedTutorial;

        [Header("현재 스테이지 정보")]
        [SerializeField] private int _currentStage;

        private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");
        private bool _saveDataInitialized;

#if UNITY_WEBGL || UNITY_EDITOR
        private bool _saveQueuedUntilCloudLoad;
#endif
        
        public int CurrentStageNumber => _currentStage;

        //--- Unity Lifecycle Methods ---//
        protected override void Awake()
        {
            base.Awake();
            if(Instance != this)
            {
                return;
            }
#if UNITY_WEBGL || UNITY_EDITOR
            InitializeDefaultSaveData();
#else
            InitializeSaveData();
#endif
            CustomDebug.Log($"세이브 데이터 초기화 완료, 총 수집 클별 : {GetTotalAcquiredStars()}");
        }

#if UNITY_WEBGL || UNITY_EDITOR
        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            StartCoroutine(LoadCloudSaveDataRoutine());
        }

        private IEnumerator LoadCloudSaveDataRoutine()
        {
#if UNITY_EDITOR
            float deadline = Time.realtimeSinceStartup + 10f;
            while (ArcadeSdk.Instance != null
                && !ArcadeSdk.Instance.IsReady
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
#endif

            // WebGL waits inside ArcadeSdk; the editor waits above because its
            // developer token is hydrated asynchronously during startup.
            SaveSystem.LoadGameDataAsync(SavePath, HandleCloudSaveLoaded);
            yield break;
        }
#endif

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

#if UNITY_EDITOR
        [ContextMenu("디버그 : 세이브 파일 초기화 (튜토리얼 다시보기용)")]
        private void DebugResetSave()
        {
#if UNITY_WEBGL || UNITY_EDITOR
            SaveSystem.DeleteGameData(SavePath, ok =>
            {
                if (!ok)
                {
                    CustomDebug.LogWarning("[GameSaveManager] 클라우드 세이브 삭제에 실패했습니다.");
                }

                InitializeDefaultSaveData();
                _saveDataInitialized = true;
                _saveQueuedUntilCloudLoad = false;
                Save();
                CustomDebug.Log("[GameSaveManager] 클라우드 세이브 데이터 초기화 완료");
            });
#else
            if(File.Exists(SavePath))
            {
                File.Delete(SavePath);
                CustomDebug.Log("세이브 파일 삭제");

                _hasCompletedTutorial = false;  
                InitializeSaveData();
                CustomDebug.Log("[GameSaveManager] 세이브 데이터 초기화 완료");
            }
#endif
        }
#endif
        //--- Public Methods ---//
        public void RecordStageClear(int acquiredStars)
        {
            var record = GetSaveRecord(_currentStage);

            if(record == null)
            {
                CustomDebug.LogError($"스테이지 세이브를 기록하지 못함, 스테이지 번호 : {_currentStage}");
                return;
            }    

            record.IsCleared = true;
            record.AcquiredStars = Mathf.Max(acquiredStars, record.AcquiredStars);
            Save();
        }

        /// <summary>
        /// 스테이지 번호로 스테이지를 로드합니다. 게임 씬에 있으면 StageLoader로 프리팹을 교체하고,
        /// 타이틀 등 다른 씬에 있으면 게임 씬(PlayStage)으로 이동합니다.
        /// </summary>
        /// <param name="stageNumber">로드할 스테이지 번호 (1-based)</param>
        public void LoadStage(int stageNumber)
        {
            if (!CheckStageUnlockRequirement(stageNumber))
            {
                CustomDebug.LogError($"해금되지 않은 스테이지 : {stageNumber}");
                return;
            }

            _currentStage = stageNumber;

            //이미 게임 씬이면 프리팹만 교체, 아니면 씬 이동후 StageLoader가 처리
            if (StageLoader.HasInstance)
            {
                StageLoader.Instance.LoadStage(stageNumber);
            }
            else
            {
                SceneManager.LoadScene("PlayStage");
            }
        }

        public StageSaveRecord GetSaveRecord(int stageNumber)
        {
            return _saveData.Find(record => record.StageNumber == stageNumber);
        }

        public StageData GetStageData(int stageNumber)
        {
            return _stageDataBase.Find(record => record.StageNumber == stageNumber);
        }

        /// <summary>
        /// 게임 해금 조건 체크
        /// </summary>
        /// <param name="stageNumber"></param>
        /// <returns>플레이 가능이면 true, 아니면 false</returns>
        public bool CheckStageUnlockRequirement(int stageNumber)
        {
            StageData stageData = GetStageData(stageNumber);

            if(stageData == null)
            {
                CustomDebug.LogError($"찾을 수 없는 스테이지 데이터, 스테이지 번호 : {stageNumber}");
                return false;
            }

            switch (stageData.RequiredCondition)
            {
                case RequiredStageCondition.UnlockedByDefault:
                    return true;

                case RequiredStageCondition.MustClearPreviousStage:
                    return GetSaveRecord(stageNumber - 1)?.IsCleared == true;

                case RequiredStageCondition.MustHaveTotalClearStars:
                    return GetTotalAcquiredStars() >= stageData.RequiredStarsValue;
                default:
                    CustomDebug.LogError($"구현되지 않은 스테이지 해금 조건");
                    return false;
            }
        }

        public int GetTotalStageNumber()
        {
            return _stageDataBase.Count;
        }

        public int GetTotalAcquiredStars()
        {
            int sum = 0;

            foreach (var saveRecord in _saveData)
            {
                if(saveRecord == null)
                {
                    continue;
                }
                sum += saveRecord.AcquiredStars;

            }

            return sum;
        }

        /// <summary>
        /// 튜토리얼 완료 여부를 반환합니다.
        /// </summary>
        public bool HasCompletedTutorial()
        {
            return _hasCompletedTutorial;
        }

        /// <summary>
        /// 튜토리얼 완료 상태를 저장합니다.
        /// </summary>
        public void SetTutorialCompleted()
        {
            _hasCompletedTutorial = true;
            Save();
        }

        //--- Private Methods ---//
        private void Save()
        {
#if UNITY_WEBGL || UNITY_EDITOR
            if (!_saveDataInitialized)
            {
                _saveQueuedUntilCloudLoad = true;
                return;
            }
#endif

            SaveSystem.SaveGameData(SavePath, new GameSaveData
            {
                HasCompletedTutorial = _hasCompletedTutorial,
                StageRecords = _saveData
            });
        }

        private void InitializeDefaultSaveData()
        {
            _hasCompletedTutorial = false;
            _saveData = new List<StageSaveRecord>();

            foreach (var stage in _stageDataBase)
            {
                if (stage == null) continue;
                _saveData.Add(new StageSaveRecord(stage.StageNumber, false, 0));
            }
        }

        private void MergeLoadedData(GameSaveData loadedData)
        {
            if (loadedData == null)
            {
                return;
            }

            _hasCompletedTutorial = loadedData.HasCompletedTutorial;
            if (loadedData.StageRecords == null)
            {
                return;
            }

            foreach (var record in loadedData.StageRecords)
            {
                if (record == null)
                {
                    continue;
                }

                var target = _saveData.Find(s => s.StageNumber == record.StageNumber);
                if (target != null)
                {
                    target.IsCleared = record.IsCleared;
                    target.AcquiredStars = record.AcquiredStars;
                }
            }
        }

#if UNITY_WEBGL || UNITY_EDITOR
        private void HandleCloudSaveLoaded(bool ok, GameSaveData loadedData)
        {
            if (ok && loadedData != null)
            {
                MergeLoadedData(loadedData);
                CustomDebug.Log("클라우드 세이브 데이터를 성공적으로 병합했습니다.");
            }
            else
            {
                // 네트워크/인증 실패 시 기존 클라우드 데이터를 기본값으로 덮어쓰지 않습니다.
                // 이후 실제 게임 데이터가 변경될 때 Save()가 재시도합니다.
                CustomDebug.LogWarning("클라우드 세이브를 사용할 수 없어 새 세이브 데이터로 시작합니다.");
            }

            _saveDataInitialized = true;
            if (_saveQueuedUntilCloudLoad)
            {
                _saveQueuedUntilCloudLoad = false;
                Save();
            }
        }
#endif

        private void InitializeSaveData()
        {
            InitializeDefaultSaveData();

            if (File.Exists(SavePath))
            {
                var loadedData = SaveSystem.LoadGameData(SavePath);
                if (loadedData != null)
                {
                    MergeLoadedData(loadedData);
                    CustomDebug.Log("세이브 데이터를 성공적으로 병합했습니다.");
                }
            }
            else
            {
                CustomDebug.Log("새 세이브 파일을 생성합니다.");
            }

            _saveDataInitialized = true;
            Save();
        }

    }
}
