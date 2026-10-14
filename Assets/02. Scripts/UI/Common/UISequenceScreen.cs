using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 요소들이 순서대로 등장하는 화면 연출의 공용 베이스.
/// - 모든 트윈은 SetUpdate(true)로 언스케일드 시간에서 돈다. (클리어/사망/일시정지로 Time.timeScale = 0이어도 재생)
/// - 등장 중 입력을 막을지(_interactableDuringEnter), 입력으로 연출을 건너뛸 수 있는지(_skippable)를 화면별로 정한다.
/// - 파생 클래스는 OnInitialize에서 요소를 Register 하고, BuildEnter에서 시퀀스를 구성한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UISequenceScreen : MonoBehaviour, IUIScreenTransition
{
    //--- Settings ---//
    [Header("Sequence Common")]
    [SerializeField, Tooltip("등장 연출 도중에도 버튼 입력을 받을지 (일시정지처럼 바로 조작해야 하는 화면)")]
    private bool _interactableDuringEnter;
    [SerializeField, Tooltip("등장 연출 중 클릭/터치/키 입력 시 연출을 끝 상태로 건너뜀")]
    private bool _skippable;
    [SerializeField, Range(0.05f, 1f)] protected float _exitDuration = 0.2f;

    //--- Fields ---//
    protected CanvasGroup _rootGroup;
    private Sequence _sequence;
    // 에디터 플레이 중 리컴파일(핫 리로드) 시 Element 참조는 복원되지 않으므로 초기화 플래그도 함께 초기화되게 한다
    [NonSerialized] private bool _isInitialized;
    private bool _isEntering;
    private bool _wasSkipped;
    private Action _pendingEnterComplete;
    private Coroutine _finishCoroutine;

    //--- Properties ---//
    public bool IsEntering => _isEntering;

    //--- Unity Methods ---//
    protected virtual void Update()
    {
        if (_isEntering && _skippable && !_wasSkipped && WasSkipPressedThisFrame())
        {
            _wasSkipped = true;
            // 삽입 콜백(SFX 등)은 몰아서 터지지 않도록 생략하고 끝 상태로 점프
            _sequence?.Complete(false);
        }
    }

    protected virtual void OnDestroy()
    {
        _sequence?.Kill();
    }

    //--- IUIScreenTransition ---//
    public void PlayEnter(Action onComplete)
    {
        EnsureInitialized();
        StopRunning();

        gameObject.SetActive(true);
        _isEntering = true;
        _wasSkipped = false;
        _pendingEnterComplete = onComplete;
        SetInputEnabled(_interactableDuringEnter);
        if (!_interactableDuringEnter)
        {
            // 포인터는 blocksRaycasts로 막히지만 키보드/패드 Submit은 선택된 버튼에 그대로 들어가므로 선택을 해제한다
            ReleaseSelection();
        }

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        BuildEnter(_sequence);
        _sequence.OnComplete(HandleEnterComplete);
    }

    public void PlayExit(Action onComplete)
    {
        EnsureInitialized();
        StopRunning();
        SetInputEnabled(false);
        ReleaseSelection();

        if (!gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        BuildExit(_sequence);
        _sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    //--- Protected Methods ---//
    /// <summary>요소 등록(홈 상태 캡처). 최초 연출 직전에 한 번 호출된다.</summary>
    protected abstract void OnInitialize();

    /// <summary>등장 시퀀스 구성. 각 요소의 시작 상태도 여기서 세팅한다.</summary>
    protected abstract void BuildEnter(Sequence sequence);

    /// <summary>퇴장 시퀀스 구성. 기본은 짧은 페이드 아웃 (퇴장은 절제)</summary>
    protected virtual void BuildExit(Sequence sequence)
    {
        sequence.Append(_rootGroup.DOFade(0f, _exitDuration).SetEase(Ease.InQuad));
    }

    /// <summary>등장 연출이 끝나고 입력이 열린 직후 호출된다.</summary>
    protected virtual void OnEnterCompleted() { }

    protected void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        // 터치 배율을 먼저 적용해야 홈 상태가 최종 값으로 캡처된다
        PlatformUIScale.ApplyInChildren(this);
        _rootGroup = GetComponent<CanvasGroup>();
        OnInitialize();
    }

    protected static Element Register(Component target)
    {
        return target == null ? null : new Element((RectTransform)target.transform);
    }

    /// <summary>offset 만큼 떨어진 곳에서 홈 위치로 이동하며 나타난다.</summary>
    protected static void InsertSlideIn(Sequence sequence, Element element, float at, Vector2 offset, float duration, Ease ease)
    {
        if (element == null)
        {
            return;
        }
        element.Rect.anchoredPosition = element.HomePosition + offset;
        element.Group.alpha = 0f;
        sequence.Insert(at, element.Rect.DOAnchorPos(element.HomePosition, duration).SetEase(ease));
        sequence.Insert(at, element.Group.DOFade(1f, duration * 0.6f).SetEase(Ease.OutQuad));
    }

    /// <summary>fromScale 배율에서 홈 크기로 변하며 나타난다.</summary>
    protected static void InsertPopIn(Sequence sequence, Element element, float at, float fromScale, float duration, Ease ease)
    {
        if (element == null)
        {
            return;
        }
        element.Rect.anchoredPosition = element.HomePosition;
        element.Rect.localScale = element.HomeScale * fromScale;
        element.Group.alpha = 0f;
        sequence.Insert(at, element.Rect.DOScale(element.HomeScale, duration).SetEase(ease));
        sequence.Insert(at, element.Group.DOFade(1f, duration * 0.5f).SetEase(Ease.OutQuad));
    }

    protected static void InsertFadeIn(Sequence sequence, Element element, float at, float duration)
    {
        if (element == null)
        {
            return;
        }
        element.Group.alpha = 0f;
        sequence.Insert(at, element.Group.DOFade(1f, duration).SetEase(Ease.OutQuad));
    }

    protected static void InsertSound(Sequence sequence, float at, SoundType soundType)
    {
        sequence.InsertCallback(at, () => UIFeedbackUtility.PlaySound(soundType));
    }

    /// <summary>건너뛰기 시 InsertSound와 마찬가지로 생략된다.</summary>
    protected static void InsertHaptic(Sequence sequence, float at, HapticType hapticType, float intensity = 1f)
    {
        sequence.InsertCallback(Mathf.Max(0f, at), () => Haptics.Play(hapticType, intensity));
    }

    //--- Private Methods ---//
    private void HandleEnterComplete()
    {
        if (_wasSkipped)
        {
            // 건너뛰기 입력과 같은 프레임에 버튼이 눌리지 않도록 한 프레임 뒤에 입력을 연다
            _finishCoroutine = StartCoroutine(FinishEnterNextFrame());
            return;
        }
        FinishEnter();
    }

    private IEnumerator FinishEnterNextFrame()
    {
        yield return null;
        _finishCoroutine = null;
        FinishEnter();
    }

    private void FinishEnter()
    {
        _isEntering = false;
        SetInputEnabled(true);
        // 입력이 열린 뒤 네비게이션 모드라면 기본 버튼에 포커스를 준다
        if (!_interactableDuringEnter && TryGetComponent(out UIDefaultSelection defaultSelection))
        {
            defaultSelection.OnScreenShown();
        }
        OnEnterCompleted();

        Action callback = _pendingEnterComplete;
        _pendingEnterComplete = null;
        callback?.Invoke();
    }

    private void StopRunning()
    {
        _sequence?.Kill();
        _sequence = null;
        _isEntering = false;
        _pendingEnterComplete = null;
        if (_finishCoroutine != null)
        {
            StopCoroutine(_finishCoroutine);
            _finishCoroutine = null;
        }
    }

    /// <summary>
    /// 포인터 입력만 차단한다. interactable을 끄면 버튼이 Disabled 색으로 바뀌었다가
    /// 연출이 끝날 때 원래 색으로 튀므로 등장/퇴장 연출 중에도 버튼 색은 유지한다.
    /// 키보드/패드 입력은 ReleaseSelection으로 선택을 비워 차단한다. (UIDefaultSelection은 blocksRaycasts가 꺼진 화면에 포커스를 주지 않음)
    /// </summary>
    private void SetInputEnabled(bool enabled)
    {
        _rootGroup.interactable = true;
        _rootGroup.blocksRaycasts = enabled;
    }

    /// <summary>퇴장하는 화면의 버튼이 키보드/패드 포커스를 쥔 채 남지 않도록 선택 해제</summary>
    private void ReleaseSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null
            && eventSystem.currentSelectedGameObject.transform.IsChildOf(transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private static bool WasSkipPressedThisFrame()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            return true;
        }
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }
        return Gamepad.current != null
            && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame);
    }

    //--- Nested Types ---//
    /// <summary>
    /// 연출 대상 요소. 등록 시점의 위치/크기를 홈으로 기억하고, 알파 제어용 CanvasGroup을 보장한다.
    /// </summary>
    protected sealed class Element
    {
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        public readonly Vector2 HomePosition;
        public readonly Vector3 HomeScale;

        public Element(RectTransform rect)
        {
            Rect = rect;
            Group = rect.GetComponent<CanvasGroup>();
            if (Group == null)
            {
                Group = rect.gameObject.AddComponent<CanvasGroup>();
            }
            HomePosition = rect.anchoredPosition;
            HomeScale = rect.localScale;
        }
    }
}
