using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Utils.IO
{
    public static class SaveSystem
    {
        /// <summary>
        /// 게임 데이터를 디스크에 저장합니다
        /// </summary>
        public static void SaveGameData(string filePath, List<StageSaveRecord> saveData)
        {
            try
            {
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(filePath, json);
                CustomDebug.Log($"저장 완료: {filePath}");
            }
            catch (System.Exception e)
            {
                CustomDebug.LogError($"저장 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 데이터를 디스크에서 읽어옵니다
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<StageSaveRecord> LoadGameData(string filePath)
        {
            List<StageSaveRecord> result;
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    result = JsonConvert.DeserializeObject<List<StageSaveRecord>>(json);
                    return result;
                }
                catch (System.Exception e)
                {
                    CustomDebug.LogError($"[Utils.IO] 디스크에서 세이브 파일을 읽는 중 오류가 발생했습니다. : {e.Message}");
                }
            }
            CustomDebug.Log($"[Utils.IO] 디스크에 세이브 파일이 존재하지 않습니다.");
            return null;
        }
    }
}

