using System.Collections.Generic;
using System.IO;
using Utils.IO;
using UnityEngine;
using Newtonsoft.Json;

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
        
        public int CurrentStageNumber => _currentStage;

        /// <summary>로컬 세이브 파일을 쓴 직후 호출됩니다. (클라우드 세이브 업로드 트리거)</summary>
        public static event System.Action Saved;

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

#if UNITY_EDITOR
        [ContextMenu("디버그 : 세이브 파일 초기화 (튜토리얼 다시보기용)")]
        private void DebugResetSave()
        {
            if(File.Exists(SavePath))
            {
                File.Delete(SavePath);
                CustomDebug.Log("세이브 파일 삭제");

                _hasCompletedTutorial = false;  
                InitializeSaveData();
                CustomDebug.Log("[GameSaveManager] 세이브 데이터 초기화 완료");
            }
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
        /// 타이틀 등 다른 씬에 있으면 게임 씬(PlayStage)으로 이동합니다. 두 경우 모두 전환 연출로 화면을 덮은 뒤 진행합니다.
        /// </summary>
        /// <param name="stageNumber">로드할 스테이지 번호 (1-based)</param>
        public void LoadStage(int stageNumber)
        {
            if (!CheckStageUnlockRequirement(stageNumber))
            {
                CustomDebug.LogError($"해금되지 않은 스테이지 : {stageNumber}");
                return;
            }

            // 전환 연출 중 중복 요청(버튼 연타 등)은 무시
            if (SceneTransitionManager.IsBusy)
            {
                return;
            }

            _currentStage = stageNumber;
            SceneTransitionOptions transition = SceneTransitionOptions.StageChange(stageNumber);

            // 화면이 iris로 닫히는 동안 현재 BGM(타이틀 등)도 함께 페이드 아웃
            SoundManager.Instance?.PlayBGM(SoundType.None, 0.8f);

            //이미 게임 씬이면 프리팹만 교체, 아니면 씬 이동후 StageLoader가 처리
            if (StageLoader.HasInstance)
            {
                SceneTransitionManager.TryRun(transition, () =>
                {
                    Time.timeScale = 1f;
                    StageLoader.Instance.LoadStage(stageNumber);
                });
            }
            else
            {
                SceneTransitionManager.TryLoadScene("PlayStage", transition);
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

        /// <summary>
        /// 현재 진행도의 복사본을 반환합니다. (클라우드 업로드용)
        /// </summary>
        public GameSaveData CreateSnapshot()
        {
            var records = new List<StageSaveRecord>();
            foreach (var record in _saveData)
            {
                if (record != null)
                {
                    records.Add(new StageSaveRecord(record.StageNumber, record.IsCleared, record.AcquiredStars));
                }
            }
            return new GameSaveData { HasCompletedTutorial = _hasCompletedTutorial, StageRecords = records };
        }

        /// <summary>
        /// 다른 기기/클라우드의 진행도를 합칩니다. 진행도는 줄어들지 않습니다(클리어는 OR, 별은 최댓값).
        /// 바뀐 것이 있으면 로컬에 저장하고 true를 반환합니다.
        /// </summary>
        public bool MergeProgress(bool tutorialDone, IEnumerable<StageSaveRecord> records)
        {
            bool changed = false;
            if (tutorialDone && !_hasCompletedTutorial)
            {
                _hasCompletedTutorial = true;
                changed = true;
            }

            if (records != null)
            {
                foreach (var incoming in records)
                {
                    if (incoming == null) continue;
                    var target = GetSaveRecord(incoming.StageNumber);
                    if (target == null) continue;
                    if (incoming.IsCleared && !target.IsCleared)
                    {
                        target.IsCleared = true;
                        changed = true;
                    }
                    int stars = Mathf.Clamp(incoming.AcquiredStars, 0, 3);
                    if (stars > target.AcquiredStars)
                    {
                        target.AcquiredStars = stars;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Save();
            }
            return changed;
        }

        //--- Private Methods ---//
        private void Save()
        {
            SaveSystem.SaveGameData(SavePath, new GameSaveData
            {
                HasCompletedTutorial = _hasCompletedTutorial,
                StageRecords = _saveData
            });
            Saved?.Invoke();
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