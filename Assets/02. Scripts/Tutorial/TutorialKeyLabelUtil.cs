using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputAction 바인딩 경로를 사람이 읽는 키 라벨로 변환하는 유틸.
/// 리바인딩(Image 2 참고 - KeyBind 탭)이 적용된 실제 경로를 기준으로 표시한다.
/// </summary>
public static class TutorialKeyLabelUtil
{
    /// <summary>
    /// 터치 UI 플랫폼용 키 가이드 라벨. 키보드 키 대신 화면 터치 컨트롤(조이스틱 / 맵 좌우·교체 버튼)을 가리킨다.
    /// 맵 버튼은 보통 실제 버튼 아이콘이 칩에 그려지므로(TutorialView.ResolveTouchIcon), 여기 라벨은 아이콘이 없을 때의 대체 글자다.
    /// </summary>
    public static string[] GetTouchLabels(TutorialObjectiveConfig config)
    {
        switch (config.CompletionType)
        {
            case TutorialCompletionType.MoveInDirection:
                // 가상 조이스틱은 방향키가 아니므로 칩에는 조작 대상 이름을 표시한다
                return new[] { "조이스틱" };

            case TutorialCompletionType.SelectMap:
                return new[] { config.SelectDirection < 0 ? "◀" : "▶" };

            case TutorialCompletionType.ChangeMap:
                // 터치에는 교체 버튼 하나만 있다 (항상 다음 맵으로 교체)
                return new[] { "교체" };

            default:
                // FindGem / CollectGem / ReachGoal - 키 가이드 없음
                return System.Array.Empty<string>();
        }
    }

    /// <summary>
    /// 터치 UI 플랫폼용 목표 설명. 키보드 기준으로 작성된 설명("를 눌러 ...") 대신
    /// 실제 터치 컨트롤(조이스틱을 밂 / 버튼 모양)에 맞는 문구를 돌려준다.
    /// 칩 우측에 이어 붙어 "[칩] + 설명"이 한 문장이 되므로, 칩에 이미 표시된 내용(버튼 아이콘 등)은 반복하지 않는다.
    /// 해당하지 않는 목표는 원래 설명을 그대로 반환한다.
    /// </summary>
    public static string GetTouchDescription(TutorialObjectiveConfig config)
    {
        switch (config.CompletionType)
        {
            case TutorialCompletionType.MoveInDirection:
                // 터치 스틱은 손가락으로 끌어 미는 조작이므로 "기울여" 대신 "밀어"를 쓴다
                return config.MoveDirection switch
                {
                    TutorialMoveDirection.Up => "을 위로 밀어 이동",
                    TutorialMoveDirection.Down => "을 아래로 밀어 이동",
                    TutorialMoveDirection.Left => "을 왼쪽으로 밀어 이동",
                    _ => "을 오른쪽으로 밀어 이동",
                };

            case TutorialCompletionType.SelectMap:
                return config.SelectDirection < 0
                    ? "버튼을 눌러 이전 맵 선택"
                    : "버튼을 눌러 다음 맵 선택";

            case TutorialCompletionType.ChangeMap:
                return "버튼을 눌러 선택된 구역의 지형 교체";

            default:
                return config.Description;
        }
    }

    /// <summary>
    /// "&lt;Keyboard&gt;/w" 같은 바인딩 경로를 키캡 라벨로 변환합니다. (예: "W", "←", "Q")
    /// </summary>
    public static string GetLabel(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath))
        {
            return "?";
        }

        // 실제 디바이스에서 이름을 읽어올 수 있으면 그것을 우선 사용 (리바인딩 반영)
        InputControl control = InputSystem.FindControl(bindingPath);
        if (control != null)
        {
            return Prettify(control.displayName);
        }

        // 폴백: 경로 파싱
        int slash = bindingPath.LastIndexOf('/');
        string name = slash >= 0 ? bindingPath.Substring(slash + 1) : bindingPath;
        return Prettify(name);
    }

    private static string Prettify(string controlName)
    {
        if (string.IsNullOrEmpty(controlName))
        {
            return "?";
        }

        switch (controlName.ToLowerInvariant())
        {
            // "Left"/"Right" 단어로 오는 displayName도 특수문자로 통일 표기
            case "left":
            case "left arrow":
            case "leftarrow":
                return "←";
            case "right":
            case "right arrow":
            case "rightarrow":
                return "→";
            case "up":
            case "up arrow":
            case "uparrow":
                return "↑";
            case "down":
            case "down arrow":
            case "downarrow":
                return "↓";
            case "left shift":
                return "Shift";
            case "left ctrl":
            case "leftctrl":
                return "Ctrl";
            case "space":
                return "Space";
            case "escape":
                return "Esc";
            default:
                // 알파벳 한 글자면 대문자로
                if (controlName.Length == 1 && char.IsLetter(controlName[0]))
                {
                    return controlName.ToUpperInvariant();
                }
                return controlName;
        }
    }
}
