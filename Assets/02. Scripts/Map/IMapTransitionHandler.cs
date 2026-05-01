/// <summary>
/// 맵 전환 애니메이션 시점에 알림을 받아야 하는 컴포넌트가 구현하는 인터페이스.
/// MapChangeEffect가 AnimateExit/AnimateEnter 시작·종료 시 호출합니다.
/// </summary>
public interface IMapTransitionHandler
{
    /// <summary>Exit 애니메이션 시작 직전 호출 (풀 오브젝트 즉시 반납 등)</summary>
    void OnMapExitStart();

    /// <summary>Enter 애니메이션 시작 직전 호출 (독립 이동 컴포넌트 일시 정지 등)</summary>
    void OnMapEnterStart();

    /// <summary>Enter 애니메이션 완료 직후 호출 (이동 컴포넌트 재개 등)</summary>
    void OnMapEnterEnd();
}
