using System.Collections.Generic;
using System.IO;
using Utils.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

namespace GameManager.Singleton
{

    public class GameSaveManager : Singleton<GameSaveManager>
    {
        [SerializeField] private List<StageData> _stageDataBase;
        [SerializeField] private List<StageSaveRecord> _saveData;

        private bool _hasCompletedTutorial;

        [Header("현재 스테이지 정보")]
        [SerializeField] private int _currentStage;

        private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");
        
        public int CurrentStageNumber => _currentStage;

        //--- Unity Lifecycle Methods ---//
        protected override void Awake()
        {
            base.Awake();
            if(Instance != this)
            {
                return;
            }
            InitializeSaveData();
            CustomDebug.Log($"세이브 데이터 초기화 완료, 총 수집 클별 : {GetTotalAcquiredStars()}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

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
            SaveSystem.SaveGameData(SavePath, new GameSaveData
            {
                HasCompletedTutorial = _hasCompletedTutorial,
                StageRecords = _saveData
            });
        }

        private void InitializeSaveData()
        {
            _saveData = new List<StageSaveRecord>();
            foreach (var stage in _stageDataBase)
            {
                if (stage == null) continue;
                _saveData.Add(new StageSaveRecord(stage.StageNumber, false, 0));
            }

            if (File.Exists(SavePath))
            {
                var loadedData = SaveSystem.LoadGameData(SavePath);
                if (loadedData != null)
                {
                    _hasCompletedTutorial = loadedData.HasCompletedTutorial;
                    if (loadedData.StageRecords != null)
                    {
                        foreach (var record in loadedData.StageRecords)
                        {
                            var target = _saveData.Find(s => s.StageNumber == record.StageNumber);
                            if (target != null)
                            {
                                target.IsCleared = record.IsCleared;
                                target.AcquiredStars = record.AcquiredStars;
                            }
                        }
                    }
                    CustomDebug.Log("세이브 데이터를 성공적으로 병합했습니다.");
                }
            }
            else
            {
                CustomDebug.Log("새 세이브 파일을 생성합니다.");
            }

            Save();
        }

    }
}