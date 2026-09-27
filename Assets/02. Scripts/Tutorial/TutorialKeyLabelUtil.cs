using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputAction 바인딩 경로를 사람이 읽는 키 라벨로 변환하는 유틸.
/// 리바인딩(Image 2 참고 - KeyBind 탭)이 적용된 실제 경로를 기준으로 표시한다.
/// </summary>
public static class TutorialKeyLabelUtil
{
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
