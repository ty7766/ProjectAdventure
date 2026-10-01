using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// 동적 위치 조이스틱.
/// 이 컴포넌트가 붙은 터치 영역(MovementTouchArea)에서 드래그가 시작되면 그 위치가 조이스틱의 중앙이 되고 즉시 입력이 시작된다.
/// 입력이 끝난 뒤에는 _holdDuration 동안 그 자리에 유지되며(이 동안 재터치는 위치를 재설정하지 않고 고정 조이스틱처럼 동작),
/// 이후 페이드 아웃되어 다시 숨김 상태가 된다.
/// </summary>
[AddComponentMenu("Input/Dynamic Joystick")]
public class DynamicJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private enum State
    {
        Hidden,
        Active,   // 포인터를 누르고 있는 중
        Holding,  // 입력 종료 후 유예 시간 (위치 고정)
        Fading,
    }

    //--- Settings ---//
    [Header("References")]
    [SerializeField, Tooltip("조이스틱 전체 루트 (JoyStickOutline). 드래그 시작 위치로 이동한다")]
    private RectTransform _joystickRoot;
    [SerializeField, Tooltip("조이스틱 노브 (JoyStickOutline/JoyStick). 루트 중앙 기준으로 움직인다")]
    private RectTransform _knob;
    [SerializeField, Tooltip("루트의 페이드 처리용 CanvasGroup. blocksRaycasts는 꺼서 터치 영역이 입력을 받도록 한다")]
    private CanvasGroup _canvasGroup;

    [Header("Behaviour")]
    [SerializeField, Min(1f), Tooltip("노브가 중앙에서 움직일 수 있는 최대 거리 (캔버스 단위)")]
    private float _movementRange = 175f;
    [SerializeField, Min(0f), Tooltip("입력 종료 후 조이스틱이 제자리에 유지되는 시간(초)")]
    private float _holdDuration = 0.25f;
    [SerializeField, Min(0.01f), Tooltip("유예 시간이 끝난 뒤 페이드 아웃에 걸리는 시간(초)")]
    private float _fadeDuration = 0.15f;

    [InputControl(layout = "Vector2")]
    [SerializeField]
    private string _controlPath = "<Gamepad>/leftStick";

    //--- State ---//
    private State _state = State.Hidden;
    private int _pointerId;
    private float _stateTimer;

    protected override string controlPathInternal
    {
        get => _controlPath;
        set => _controlPath = value;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetToHidden();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ResetToHidden();
    }

    private void Update()
    {
        // 일시정지(timeScale 0) 중에도 유예/페이드가 진행되도록 unscaled 시간 사용
        switch (_state)
        {
            case State.Holding:
                _stateTimer += Time.unscaledDeltaTime;
                if (_stateTimer >= _holdDuration)
                {
                    _state = State.Fading;
                    _stateTimer = 0f;
                }
                break;

            case State.Fading:
                _stateTimer += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(_stateTimer / _fadeDuration);
                if (_stateTimer >= _fadeDuration)
                    ResetToHidden();
                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 이미 다른 손가락이 조이스틱을 쓰는 중이면 무시
        if (_state == State.Active)
            return;

        // 유예 시간 중에는 현재 자리를 유지한 채 입력을 받고, 그 외(숨김/페이드 중)에는 터치 위치로 중앙을 재설정
        if (_state != State.Holding)
            PlaceRootAt(eventData.position, eventData.pressEventCamera);

        _state = State.Active;
        _pointerId = eventData.pointerId;
        _canvasGroup.alpha = 1f;

        MoveKnob(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_state != State.Active || eventData.pointerId != _pointerId)
            return;

        MoveKnob(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_state != State.Active || eventData.pointerId != _pointerId)
            return;

        _knob.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);

        _state = State.Holding;
        _stateTimer = 0f;
    }

    //--- Internal ---//
    private void PlaceRootAt(Vector2 screenPosition, Camera uiCamera)
    {
        var parent = _joystickRoot.parent as RectTransform;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPosition, uiCamera, out Vector3 world))
            return;

        // 피벗 설정과 무관하게 루트의 '중앙'이 터치 위치에 오도록 보정
        Vector3 pivotToCenter = _joystickRoot.TransformPoint(_joystickRoot.rect.center) - _joystickRoot.position;
        _joystickRoot.position = world - pivotToCenter;
    }

    private void MoveKnob(Vector2 screenPosition, Camera uiCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_joystickRoot, screenPosition, uiCamera, out Vector2 local))
            return;

        Vector2 delta = Vector2.ClampMagnitude(local - _joystickRoot.rect.center, _movementRange);
        _knob.anchoredPosition = delta;
        SendValueToControl(delta / _movementRange);
    }

    private void ResetToHidden()
    {
        _state = State.Hidden;
        _stateTimer = 0f;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
        if (_knob != null)
            _knob.anchoredPosition = Vector2.zero;

        SendValueToControl(Vector2.zero); // 컨트롤이 없으면(비활성화 후) 내부에서 무시됨
    }
}
