using System;
using UnityEngine;

/// <summary>
/// 튜토리얼 목표의 완료 감지 방식
/// </summary>
public enum TutorialCompletionType
{
    MoveInDirection, // 지정 방향 키를 눌러 이동
    SelectMap,       // 좌/우 방향키로 맵 선택 커서 이동
    ChangeMap,       // Q/E로 지형 교체 성공
    FindGem,         // 숨겨진 보석 근처에 도달 (발견)
    CollectGem,      // 보석 획득
    ReachGoal,       // 골인 지점 도달
}

/// <summary>
/// MoveInDirection 목표의 이동 방향
/// </summary>
public enum TutorialMoveDirection
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// 섹션 시작 시 소환(활성화)할 튜토리얼 전용 오브젝트
/// </summary>
public enum TutorialSpawnObjectType
{
    None,
    Gem,
    Goal,
}

/// <summary>
/// 목표 패널에 표시되는 개별 목표 한 줄의 구성 데이터
/// </summary>
[Serializable]
public class TutorialObjectiveConfig
{
    [Tooltip("목표 설명 (키 가이드 칩 우측에 표시됩니다)")]
    public string Description = "키를 눌러 이동";

    [Tooltip("완료 감지 방식")]
    public TutorialCompletionType CompletionType = TutorialCompletionType.MoveInDirection;

    [Tooltip("MoveInDirection: 이동 방향")]
    public TutorialMoveDirection MoveDirection = TutorialMoveDirection.Up;

    [Tooltip("SelectMap: 맵 선택 방향 (-1 = 이전 맵, 1 = 다음 맵)")]
    public int SelectDirection = 1;
}

/// <summary>
/// 튜토리얼의 큰 섹션 한 단위 (예: 이동하기, 맵 선택하기, ...)
/// </summary>
[Serializable]
public class TutorialSectionConfig
{
    [Tooltip("섹션 제목 (예: 이동하기)")]
    public string Title = "이동하기";

    [Tooltip("섹션 시작 시 소환할 오브젝트")]
    public TutorialSpawnObjectType SpawnOnStart = TutorialSpawnObjectType.None;

    [Tooltip("섹션 시작 시 카메라를 이동시킬 TutorialAnchor ID (비워두면 플레이어 추적 유지)")]
    public string FocusAnchorID = "";

    [Tooltip("이 섹션의 목표 목록 (모두 완료해야 다음 섹션으로 진행)")]
    public TutorialObjectiveConfig[] Objectives = Array.Empty<TutorialObjectiveConfig>();

    [Tooltip("목표 목록 아래에 표시할 팁 문구 (비워두면 팁을 표시하지 않습니다)")]
    public string Tip = "";
}
