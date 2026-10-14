using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 게임패드/키보드 포커스 관리.
/// - 네비게이션 입력(방향키, WASD, Tab, 패드 방향)이 들어왔는데 선택된 UI가 없으면 기본 Selectable을 선택한다.
/// - 마우스를 움직이면 네비게이션 모드를 끄고 선택 하이라이트를 한 번 해제한다.
/// - 화면이 표시될 때(OnScreenShown) 네비게이션 모드라면 기본 Selectable을 바로 선택한다.
/// 같은 GameObject(또는 부모)의 CanvasGroup이 활성 상태일 때만 동작한다.
/// </summary>
[DisallowMultipleComponent]
public class UIDefaultSelection : MonoBehaviour
{
    private const float STICK_THRESHOLD = 0.5f;
    private const float MOUSE_MOVE_THRESHOLD_SQR = 4f;

    //--- Settings ---//
    [SerializeField] private Selectable _defaultSelectable;

    //--- Fields ---//
    private static bool s_isNavigationMode;
    private static int s_lastModeCheckFrame = -1;

    private CanvasGroup _canvasGroup;
    private Func<Selectable> _defaultProvider;

    public static bool IsNavigationMode => s_isNavigationMode;

    //--- Unity Methods ---//
    private void Awake()
    {
        _canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    private void Update()
    {
        UpdateInputMode();

        if (!s_isNavigationMode || !IsScreenActive() || EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
        {
            if (WasNavigationPressedThisFrame())
            {
                SelectDefault();
            }
        }
    }

    //--- Public Methods ---//
    /// <summary>
    /// 런타임에 컴포넌트를 보장하고 기본 선택 대상을 지정한다.
    /// </summary>
    public static UIDefaultSelection Ensure(GameObject root, Func<Selectable> defaultProvider)
    {
        var selection = root.GetComponent<UIDefaultSelection>();
        if (selection == null)
        {
            selection = root.AddComponent<UIDefaultSelection>();
        }
        selection._defaultProvider = defaultProvider;
        return selection;
    }

    /// <summary>
    /// 화면이 막 표시되었을 때 호출. 이전 화면의 선택을 정리하고, 네비게이션 모드면 기본 대상을 선택한다.
    /// </summary>
    public void OnScreenShown()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (s_isNavigationMode)
        {
            SelectDefault();
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void SelectDefault()
    {
        Selectable target = _defaultProvider?.Invoke() ?? _defaultSelectable;
        if (target != null && target.isActiveAndEnabled && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    //--- Private Methods ---//
    private bool IsScreenActive()
    {
        if (_canvasGroup != null && (!_canvasGroup.interactable || !_canvasGroup.blocksRaycasts))
        {
            return false;
        }

        // 공용 팝업이 떠 있으면 뒤 화면은 포커스를 가져가지 않는다
        return !(GlobalUICanvasView.HasInstance && GlobalUICanvasView.Instance.IsPopupShown());
    }

    private static void UpdateInputMode()
    {
        // 여러 화면이 동시에 존재해도 프레임당 한 번만 판정
        if (s_lastModeCheckFrame == Time.frameCount)
        {
            return;
        }
        s_lastModeCheckFrame = Time.frameCount;

        if (WasNavigationPressedThisFrame())
        {
            s_isNavigationMode = true;
            return;
        }

        Mouse mouse = Mouse.current;
        if (s_isNavigationMode && mouse != null && mouse.delta.ReadValue().sqrMagnitude > MOUSE_MOVE_THRESHOLD_SQR)
        {
            s_isNavigationMode = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private static bool WasNavigationPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
             keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame ||
             keyboard.wKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
             keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
             keyboard.tabKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame)
            {
                return true;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            Vector2 prevStick = gamepad.leftStick.ReadValueFromPreviousFrame();
            if (stick.sqrMagnitude > STICK_THRESHOLD * STICK_THRESHOLD &&
                prevStick.sqrMagnitude <= STICK_THRESHOLD * STICK_THRESHOLD)
            {
                return true;
            }
        }

        return false;
    }
}
