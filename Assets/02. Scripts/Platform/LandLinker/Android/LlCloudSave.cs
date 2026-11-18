#if UNITY_ANDROID
using System.Collections.Generic;
using GameManager.Singleton;
using Kory.Platform;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 진행도 클라우드 세이브 (서버 계약: korygames-platform/server/games/ll/docs/api.md).
/// 접속 직후 서버 진행도를 내려받아 로컬과 합치고, 로컬 세이브가 쓰일 때마다 올린다.
/// 서버와 로컬 모두 "줄지 않는 병합"(클리어 OR, 별 최댓값)이라 어느 쪽이 먼저든 진행도가 사라지지 않는다.
/// </summary>
public static class LlCloudSave
{
    private static string SavePath => KoryRoutes.Game("/save");

    private static bool s_pushing;
    private static bool s_pushAgain;
    private static string s_lastPushedBody;
    private static JObject s_pendingServerSave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_pushing = false;
        s_pushAgain = false;
        s_lastPushedBody = null;
        s_pendingServerSave = null;
    }

    public static void Attach()
    {
        GameSaveManager.Saved -= OnLocalSaved;
        GameSaveManager.Saved += OnLocalSaved;
    }

    /// <summary>KorySession.AfterLogin: 로그인 직후 서버 진행도를 받아 합치고, 로컬이 앞서 있으면 올린다.</summary>
    public static async Awaitable<KoryAuthOutcome> PullAfterLogin()
    {
        var result = await KoryApi.Get<JObject>(KoryRoutes.Profile);
        if (!result.ok)
        {
            return result.Transient ? KoryAuthOutcome.Offline : KoryAuthOutcome.Failed;
        }

        ApplyServerSave(result.data?["profile"]?["save"] as JObject);
        s_lastPushedBody = null;
        Push();
        return KoryAuthOutcome.SignedIn;
    }

    private static void OnLocalSaved()
    {
        Push();
    }

    private static async void Push()
    {
        // Online은 AfterLogin(PullAfterLogin)이 끝난 뒤에 켜지므로, 로그인 토큰 유무로 판단한다.
        if (string.IsNullOrEmpty(KorySession.Token) || !GameSaveManager.HasInstance)
        {
            return;
        }
        if (s_pushing)
        {
            s_pushAgain = true;
            return;
        }

        ApplyPendingServerSave();
        JObject body = BuildBody(GameSaveManager.Instance.CreateSnapshot());
        string json = body.ToString(Newtonsoft.Json.Formatting.None);
        if (json == s_lastPushedBody)
        {
            return;
        }

        s_pushing = true;
        try
        {
            var result = await KoryApi.Post<JObject>(SavePath, body);
            if (result.ok)
            {
                s_lastPushedBody = json;
                ApplyServerSave(result.data?["profile"]?["save"] as JObject);
            }
            else if (!result.Transient)
            {
                Debug.LogWarning($"[LandLinker] 클라우드 세이브 거부: {result.errorCode} {result.raw}");
                s_lastPushedBody = json; // 같은 내용을 반복해서 보내지 않는다
            }
        }
        finally
        {
            s_pushing = false;
        }

        if (s_pushAgain)
        {
            s_pushAgain = false;
            Push();
        }
    }

    private static JObject BuildBody(GameSaveData snapshot)
    {
        var stages = new JArray();
        if (snapshot.StageRecords != null)
        {
            foreach (var record in snapshot.StageRecords)
            {
                // 서버는 클리어한 스테이지만 보관한다 (별은 클리어한 스테이지에만 있을 수 있다)
                if (record == null || !record.IsCleared) continue;
                stages.Add(new JObject
                {
                    ["n"] = record.StageNumber,
                    ["cleared"] = true,
                    ["stars"] = Mathf.Clamp(record.AcquiredStars, 0, 3),
                });
            }
        }
        return new JObject
        {
            ["tutorialDone"] = snapshot.HasCompletedTutorial,
            ["stages"] = stages,
        };
    }

    private static void ApplyServerSave(JObject save)
    {
        if (save == null) return;
        if (!GameSaveManager.HasInstance)
        {
            s_pendingServerSave = save; // 세이브 매니저가 뜨면 합친다
            return;
        }

        var records = new List<StageSaveRecord>();
        if (save["stages"] is JArray stages)
        {
            foreach (var stage in stages)
            {
                int n = stage.Value<int?>("n") ?? 0;
                if (n <= 0) continue;
                records.Add(new StageSaveRecord(n, stage.Value<bool?>("cleared") ?? false, stage.Value<int?>("stars") ?? 0));
            }
        }
        bool tutorialDone = save.Value<bool?>("tutorialDone") ?? false;
        if (GameSaveManager.Instance.MergeProgress(tutorialDone, records))
        {
            Debug.Log("[LandLinker] 클라우드 진행도를 로컬 세이브에 합쳤습니다.");
        }
    }

    private static void ApplyPendingServerSave()
    {
        if (s_pendingServerSave == null || !GameSaveManager.HasInstance) return;
        var pending = s_pendingServerSave;
        s_pendingServerSave = null;
        ApplyServerSave(pending);
    }
}
#endif
