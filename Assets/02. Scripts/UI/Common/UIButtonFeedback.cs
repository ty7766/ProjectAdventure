using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 공용 반응 연출 (hover / select / press / release / 비활성 클릭 거부).
/// - 마우스 hover 시 해당 버튼을 Select 상태로 만들어 hover와 게임패드/키보드 select를 하나의 상태로 통합한다.
/// - 터치 입력은 hover를 건너뛰고 press/release만 반응한다.
/// - ButtonClickEffectView(스프라이트 스왑)와 함께 붙여서 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler
{
    //--- Settings ---//
    [Header("연출 대상 (비워두면 자기 자신)")]
    [SerializeField] private RectTransform _target;

    [Header("Hover / Select")]
    [SerializeField] private float _hoverScale = 1.06f;
    [SerializeField, Tooltip("hover 시 z축 기울기(도). 0이면 기울이지 않음")]
    private float _hoverTilt = 0f;
    [SerializeField] private float _hoverDuration = 0.15f;

    [Header("Press / Release")]
    [SerializeField] private float _pressScale = 0.93f;
    [SerializeField] private float _pressDuration = 0.06f;
    [SerializeField] private float _releaseDuration = 0.22f;

    [Header("Denied (비활성 상태 클릭)")]
    [SerializeField] private float _deniedShakeStrength = 12f;
    [SerializeField] private float _deniedDuration = 0.35f;

    [Header("Sound")]
    [SerializeField] private SoundType _hoverSound = SoundType.SFX_ButtonHover;
    [SerializeField] private SoundType _deniedSound = SoundType.SFX_ButtonDenied;

    //--- Fields ---//
    private Selectable _selectable;
    private Vector3 _baseScale = Vector3.one;
    private Quaternion _baseRotation = Quaternion.identity;
    private Vector2 _baseAnchoredPosition;
    private bool _isHovered;
    private bool _isPressed;
    private Tween _scaleTween;
    private Tween _tiltTween;
    private Tween _shakeTween;

    public RectTransform Target => _target;
    public bool IsHovered => _isHovered;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (_target == null)
        {
            _target = (RectTransform)transform;
        }
        _selectable = GetComponent<Selectable>();
        _baseScale = _target.localScale;
        _baseRotation = _target.localRotation;
    }

    private void OnDisable()
    {
        KillTweens();
        _isHovered = false;
        _isPressed = false;
        _target.localScale = _baseScale;
        _target.localRotation = _baseRotation;
    }

    //--- Public Methods ---//
    public void SetHoverScale(float hoverScale)
    {
        _hoverScale = hoverScale;
        if (_isHovered && !_isPressed)
        {
            ApplyState(_hoverDuration, Ease.OutBack);
        }
    }

    /// <summary>
    /// 거부 연출: 좌우로 흔들고 거부 사운드를 재생한다.
    /// </summary>
    public void PlayDenied()
    {
        if (_shakeTween != null && _shakeTween.IsActive())
        {
            _shakeTween.Complete();
        }

        _baseAnchoredPosition = _target.anchoredPosition;
        _shakeTween = _target
            .DOShakeAnchorPos(_deniedDuration, new Vector2(_deniedShakeStrength, 0f), 20, 0f, false, true)
            .SetUpdate(true)
            .OnKill(() => _target.anchoredPosition = _baseAnchoredPosition);
        UIFeedbackUtility.PlaySound(_deniedSound);
    }

    //--- Event Handlers ---//
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIFeedbackUtility.IsTouch(eventData) || !IsInteractable())
        {
            return;
        }

        // hover == select: 게임패드/키보드 포커스와 마우스 hover가 같은 상태를 공유한다
        if (_selectable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }
        else
        {
            SetHovered(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIFeedbackUtility.IsTouch(eventData))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null, eventData);
        }
        else
        {
            SetHovered(false);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 터치로 눌러서 선택된 경우에는 hover 연출을 하지 않는다
        if (UIFeedbackUtility.IsTouch(eventData))
        {
            return;
        }
        SetHovered(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHovered(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable())
        {
            return;
        }
        _isPressed = true;
        ApplyState(_pressDuration, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed)
        {
            return;
        }
        _isPressed = false;

        // 터치는 hover가 없으므로 손을 떼면 기본 크기로 튕겨 돌아간다
        if (UIFeedbackUtility.IsTouch(eventData))
        {
            _isHovered = false;
        }
        ApplyState(_releaseDuration, Ease.OutBack);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !IsInteractable())
        {
            PlayDenied();
        }
    }

    //--- Private Methods ---//
    private bool IsInteractable()
    {
        return _selectable == null || _selectable.IsInteractable();
    }

    private void SetHovered(bool hovered)
    {
        if (_isHovered == hovered)
        {
            return;
        }
        _isHovered = hovered;

        if (hovered)
        {
            UIFeedbackUtility.PlaySound(_hoverSound);
        }
        ApplyState(_hoverDuration, hovered ? Ease.OutBack : Ease.OutQuad);
    }

    private void ApplyState(float duration, Ease ease)
    {
        float scale = _isPressed ? _pressScale : (_isHovered ? _hoverScale : 1f);

        _scaleTween?.Kill();
        _scaleTween = _target.DOScale(_baseScale * scale, duration).SetEase(ease).SetUpdate(true);

        if (!Mathf.Approximately(_hoverTilt, 0f))
        {
            float tilt = _isHovered && !_isPressed ? _hoverTilt : 0f;
            _tiltTween?.Kill();
            _tiltTween = _target.DOLocalRotateQuaternion(_baseRotation * Quaternion.Euler(0f, 0f, tilt), duration).SetEase(ease).SetUpdate(true);
        }
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();
        _tiltTween?.Kill();
        _shakeTween?.Kill();
    }
}
