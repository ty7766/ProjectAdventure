using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

namespace GameManager.Singleton // 철자 수정
{

    public class GameSaveManager : MonoBehaviour
    {
        public static GameSaveManager Instance { get; private set; }

        [SerializeField] private List<StageData> _stageDataBase;
        [SerializeField] private List<StageSaveRecord> _saveData;

        [Header("현재 스테이지 정보")]
        [SerializeField] private int _currentStage;

        private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");
        
        public int CurrentStageNumber => _currentStage;

        //--- Unity Lifecycle Methods ---//
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeSaveData();
            CustomDebug.Log($"세이브 데이터 초기화 완료, 총 수집 클별 : {GetTotalAcquiredStars()}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGameData();
        }

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        //--- Public Methods ---//
        /// <summary>
        /// 게임 데이터를 디스크에 저장합니다
        /// </summary>
        public void SaveGameData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_saveData, Formatting.Indented);
                File.WriteAllText(SavePath, json);
                CustomDebug.Log($"저장 완료: {SavePath}");
            }
            catch (System.Exception e)
            {
                CustomDebug.LogError($"저장 실패: {e.Message}");
            }
        }

        public void RecordStageClear(int acquiredStars)
        {
            int saveDataIndex = _currentStage - 1;

            if(saveDataIndex < 0 || saveDataIndex >= _saveData.Count)
            {
                CustomDebug.LogError($"[Out Of Index] 스테이지 세이브를 기록하지 못함, 세이브 데이터 인덱스 : {saveDataIndex}");
                return;
            }

            _saveData[saveDataIndex].IsCleared = true;
            _saveData[saveDataIndex].AcquiredStars = acquiredStars;

            SaveGameData();
        }

        /// <summary>
        /// 스테이지 번호로 스테이지 씬을 로드합니다.
        /// </summary>
        /// <param name="stageNumber"></param>
        public void LoadStage(int stageNumber)
        {
            string sceneName = null;
            foreach(StageData stageData in _stageDataBase)
            {
                if(stageData.StageNumber == stageNumber)
                {
                    sceneName = stageData.SceneName;
                    break;
                }
            }

            if(sceneName != null && CheckStageUnlockRequirement(stageNumber))
            {
                _currentStage = stageNumber;
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                CustomDebug.LogError($"찾을 수 없거나 해금되지 않은 스테이지 : {stageNumber}");
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
                    if (GetSaveRecord(stageNumber - 1)?.IsCleared == true)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case RequiredStageCondition.MustHaveTotalClearStars:
                    if (GetTotalAcquiredStars() >= stageData.RequiredStarsValue)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
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

        //--- Private Methods ---//
        private void InitializeSaveData()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                _saveData = JsonConvert.DeserializeObject<List<StageSaveRecord>>(json);
            }
            else
            {
                _saveData = new List<StageSaveRecord>();
                foreach (var stage in _stageDataBase)
                {
                    if(stage == null)
                    {
                        continue;
                    }
                    _saveData.Add(new StageSaveRecord(stage.StageNumber, false, 0));
                }
                SaveGameData();
            }
        }
    }
}