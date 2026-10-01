using System;

/// <summary>
/// 화면(캔버스) 단위 등장/퇴장 연출. 화면 전환 컨트롤러가 알파 페이드 대신 이 연출을 사용한다.
/// </summary>
public interface IUIScreenTransition
{
    /// <summary>등장 연출을 재생하고, 끝나면 onComplete를 호출한다. (이미 등장해 있으면 즉시 호출)</summary>
    void PlayEnter(Action onComplete);

    /// <summary>퇴장 연출을 재생하고, 끝나면 onComplete를 호출한다.</summary>
    void PlayExit(Action onComplete);
}
