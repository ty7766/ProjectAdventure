/// <summary>
/// 게임에서 쓰는 햅틱 프리셋. 실제 진동 패턴은 HapticPresets에서 정의한다.
/// </summary>
public enum HapticType
{
    None = 0,

    //UI
    Tap = 100,          // 터치 버튼 누름
    Selection = 101,    // 선택 이동 (맵 슬롯 좌우)
    Denied = 102,       // 거부 (비활성 버튼, 맵 교체 불가)

    //Flow
    CountdownTick = 200,
    CountdownGo = 201,

    //InGame
    GemCollect = 300,
    MapSwap = 301,
    Damage = 302,
    Death = 303,

    //Stage Clear
    StarDrop = 400,     // 별이 내리꽂히는 동안 차오르는 예비 진동
    StarImpact = 401,   // 별이 박히는 순간의 충격 + 패널 흔들림 여진
}
