using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_WEBGL || UNITY_EDITOR
using ArcadeBackend;
#endif

namespace Utils.IO
{
    public static class SaveSystem
    {
#if UNITY_WEBGL || UNITY_EDITOR
        private const string CloudSaveSlot = "main";
#endif
        /// <summary>
        /// 플랫폼 설정에 따라 게임 데이터를 로컬 또는 클라우드에 저장합니다.
        /// </summary>
        public static void SaveGameData(string filePath, GameSaveData saveData)
        {
            try
            {
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

#if UNITY_WEBGL || UNITY_EDITOR
                SaveCloudGameData(json);
#else
                File.WriteAllText(filePath, json);
                CustomDebug.Log($"저장 완료: {filePath}");
#endif
            }
            catch (Exception e)
            {
                CustomDebug.LogError($"저장 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 데이터를 비동기로 읽습니다. WebGL과 에디터에서는 ArcadeSdk를 사용하고,
        /// 그 외 플랫폼에서는 기존 로컬 파일을 읽은 뒤 콜백을 즉시 호출합니다.
        /// </summary>
        public static void LoadGameDataAsync(
            string filePath,
            Action<bool, GameSaveData> onComplete = null)
        {
#if UNITY_WEBGL || UNITY_EDITOR
            var sdk = ArcadeSdk.Instance;
            if (sdk == null)
            {
                CustomDebug.LogError("[Utils.IO] ArcadeSdk 인스턴스를 찾을 수 없어 클라우드 세이브를 읽지 못했습니다.");
                Complete(onComplete, false, null);
                return;
            }

            sdk.LoadSave(CloudSaveSlot, (ok, save) =>
            {
                if (!ok || save == null || string.IsNullOrEmpty(save.data))
                {
                    CustomDebug.LogWarning("[Utils.IO] 클라우드 세이브가 없거나 읽지 못했습니다.");
                    Complete(onComplete, false, null);
                    return;
                }

                try
                {
                    var loadedData = JsonConvert.DeserializeObject<GameSaveData>(save.data);
                    Complete(onComplete, loadedData != null, loadedData);
                }
                catch (Exception e)
                {
                    CustomDebug.LogError($"[Utils.IO] 클라우드 세이브를 읽는 중 오류가 발생했습니다: {e.Message}");
                    Complete(onComplete, false, null);
                }
            });
#else
            var loadedData = LoadGameData(filePath);
            Complete(onComplete, loadedData != null, loadedData);
#endif
        }

        /// <summary>
        /// 게임 데이터를 삭제합니다. 에디터와 WebGL에서는 클라우드 슬롯을 삭제하고,
        /// 그 외 플랫폼에서는 로컬 파일을 삭제합니다.
        /// </summary>
        public static void DeleteGameData(string filePath, Action<bool> onComplete = null)
        {
#if UNITY_WEBGL || UNITY_EDITOR
            var sdk = ArcadeSdk.Instance;
            if (sdk == null)
            {
                CustomDebug.LogError("[Utils.IO] ArcadeSdk 인스턴스를 찾을 수 없어 클라우드 세이브를 삭제하지 못했습니다.");
                Complete(onComplete, false);
                return;
            }

            sdk.DeleteSave(CloudSaveSlot, ok =>
            {
                if (ok)
                {
                    CustomDebug.Log("[Utils.IO] 클라우드 세이브 삭제 완료");
                }
                else
                {
                    CustomDebug.LogWarning("[Utils.IO] 클라우드 세이브 삭제에 실패했습니다.");
                }

                Complete(onComplete, ok);
            });
#else
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                CustomDebug.Log($"세이브 삭제 완료: {filePath}");
                Complete(onComplete, true);
            }
            catch (Exception e)
            {
                CustomDebug.LogError($"세이브 삭제 실패: {e.Message}");
                Complete(onComplete, false);
            }
#endif
        }

        /// <summary>
        /// 게임 데이터를 디스크에서 읽어옵니다
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static GameSaveData LoadGameData(string filePath)
        {
#if UNITY_WEBGL || UNITY_EDITOR
            CustomDebug.LogWarning("[Utils.IO] 클라우드 세이브는 비동기 방식입니다. LoadGameDataAsync를 사용하세요.");
            return null;
#else
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<GameSaveData>(json);
                }
                catch (Exception e)
                {
                    CustomDebug.LogError($"[Utils.IO] 디스크에서 세이브 파일을 읽는 중 오류가 발생했습니다. : {e.Message}");
                }
            }
            CustomDebug.Log($"[Utils.IO] 디스크에 세이브 파일이 존재하지 않습니다.");
            return null;
#endif
        }

#if UNITY_WEBGL || UNITY_EDITOR
        private static void SaveCloudGameData(string json)
        {
            var sdk = ArcadeSdk.Instance;
            if (sdk == null)
            {
                CustomDebug.LogError("[Utils.IO] ArcadeSdk 인스턴스를 찾을 수 없어 클라우드 세이브를 저장하지 못했습니다.");
                return;
            }

            // -1은 revision 검사를 생략하는 last-writer-wins 저장입니다.
            sdk.SaveData(CloudSaveSlot, json, -1, (ok, save) =>
            {
                if (ok)
                {
                    CustomDebug.Log($"클라우드 세이브 저장 완료: {CloudSaveSlot}");
                    ArcadeWelcomeNotification.ShowCloudSaveCompleted();
                }
                else
                {
                    CustomDebug.LogWarning($"클라우드 세이브 저장 실패: {CloudSaveSlot}");
                }
            });
        }
#endif

        private static void Complete(Action<bool, GameSaveData> callback, bool success, GameSaveData saveData)
        {
            if (callback != null)
            {
                callback(success, saveData);
            }
        }

        private static void Complete(Action<bool> callback, bool success)
        {
            if (callback != null)
            {
                callback(success);
            }
        }
    }
}

