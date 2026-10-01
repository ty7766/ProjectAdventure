using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// UI 반응 연출에서 공통으로 쓰는 헬퍼.
/// </summary>
public static class UIFeedbackUtility
{
    /// <summary>
    /// 터치 입력으로 발생한 포인터 이벤트인지 판별한다. (터치에는 hover 개념이 없다)
    /// </summary>
    public static bool IsTouch(BaseEventData eventData)
    {
        if (eventData is ExtendedPointerEventData extended)
        {
            return extended.pointerType == UIPointerType.Touch;
        }

        // 구 입력 모듈: 마우스는 음수 id(-1~-3), 터치는 0 이상의 fingerId
        return eventData is PointerEventData pointer && pointer.pointerId >= 0;
    }

    /// <summary>
    /// 등록된 사운드만 재생한다. (클립이 아직 등록되지 않은 신규 UI SFX가 경고 로그를 뿌리지 않도록)
    /// </summary>
    public static void PlaySound(SoundType soundType)
    {
        if (soundType == SoundType.None || !SoundManager.HasInstance)
        {
            return;
        }

        if (SoundManager.Instance.HasSound(soundType))
        {
            SoundManager.Instance.PlaySFX(soundType);
        }
    }
}
