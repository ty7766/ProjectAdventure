using UnityEngine;

/// <summary>
/// 튜토리얼 진행 이벤트를 구독자들에게 전달하는 단순 버스.
/// Manager / Spawn 오브젝트 / View 간 직접 참조를 줄인다.
/// </summary>
public class TutorialEventBus
{
    public event System.Action<TutorialSpawnObjectType> SpawnRequested;
    public event System.Action GemFound;
    public event System.Action GemCollected;
    public event System.Action GoalReached;

    public void RequestSpawn(TutorialSpawnObjectType type) => SpawnRequested?.Invoke(type);
    public void RaiseGemFound() => GemFound?.Invoke();
    public void RaiseGemCollected() => GemCollected?.Invoke();
    public void RaiseGoalReached() => GoalReached?.Invoke();
}
