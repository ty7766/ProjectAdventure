using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
public class TitleView : MonoBehaviour, IUIScreenTransition
{
    //--- Settings ---//
    [Header("Title Buttons")]
    [SerializeField] private UnityEngine.UI.Button _playButton;
    [SerializeField] private UnityEngine.UI.Button _optionsButton;
    [SerializeField] private UnityEngine.UI.Button _exitButton;

    [Header("Slide Sequence")]
    [SerializeField, Tooltip("등장 순서대로 배치 (로고 -> Play -> Options -> Exit). 퇴장은 역순")]
    private List<RectTransform> _slideTargets = new List<RectTransform>();
    [SerializeField, Range(0.1f, 2f)] private float _slideInDuration = 0.5f;
    [SerializeField, Range(0.1f, 2f)] private float _slideOutDuration = 0.3f;
    [SerializeField, Range(0f, 0.5f)] private float _slideInInterval = 0.1f;
    [SerializeField, Range(0f, 0.5f)] private float _slideOutInterval = 0.06f;
    [SerializeField, Tooltip("화면 왼쪽 끝에서 추가로 더 밀어낼 거리 (캔버스 단위)")]
    private float _offscreenMargin = 50f;

    //--- Dependency Injection ---//
    [SerializeField] private TitleFlowController _flowController;

    //--- Fields ---//
    private TitlePresenter _titlePresenter;
    private UnityAction _onPlayButtonClicked;
    private UnityAction _onOptionsButtonClicked;
    private UnityAction _onExitButtonClicked;

    private enum SlideState { Hidden, Animating, Shown }
    private SlideState _slideState = SlideState.Hidden;
    private Vector2[] _homePositions;
    private RectTransform _canvasRect;
    private Sequence _slideSequence;
    private readonly Vector3[] _cornerBuffer = new Vector3[4];


    //--- Unity Methods ---//
    private void Awake()
    {
        if (IsCheckFailed())
        {
            this.enabled = false;
            return;
        }

        InitializeView();
    }

    private void Start()
    {
        SoundManager.Instance.PlayBGM(SoundType.BGM_TitleSceneMusic);
    }

    private void OnDestroy()
    {
        _slideSequence?.Kill();
        DisposeButtonHandlers();
    }

    //--- IUIScreenTransition ---//
    /// <summary>
    /// 로고 -> Play -> Options -> Exit 순서로 화면 왼쪽 밖에서 슬라이드 인 (Quadratic Ease Out)
    /// </summary>
    public void PlayEnter(Action onComplete)
    {
        // 초기화 실패(인스펙터 누락) 시 연출 없이 통과
        if (_homePositions == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_slideState == SlideState.Shown)
        {
            onComplete?.Invoke();
            return;
        }

        if (_slideState == SlideState.Hidden)
        {
            SnapToHidden();
        }

        _slideSequence?.Kill();
        _slideState = SlideState.Animating;
        _slideSequence = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < _slideTargets.Count; i++)
        {
            _slideSequence.Insert(i * _slideInInterval,
                _slideTargets[i].DOAnchorPos(_homePositions[i], _slideInDuration).SetEase(Ease.OutQuad));
        }
        _slideSequence.OnComplete(() =>
        {
            _slideState = SlideState.Shown;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 등장의 역순(Exit -> Options -> Play -> 로고)으로 화면 왼쪽 밖으로 슬라이드 아웃 (Quadratic Ease In)
    /// </summary>
    public void PlayExit(Action onComplete)
    {
        // 초기화 실패(인스펙터 누락) 시 연출 없이 통과
        if (_homePositions == null)
        {
            onComplete?.Invoke();
            return;
        }

        _slideSequence?.Kill();
        _slideState = SlideState.Animating;
        _slideSequence = DOTween.Sequence().SetUpdate(true);
        for (int i = _slideTargets.Count - 1, order = 0; i >= 0; i--, order++)
        {
            _slideSequence.Insert(order * _slideOutInterval,
                _slideTargets[i].DOAnchorPos(GetHiddenPosition(i), _slideOutDuration).SetEase(Ease.InQuad));
        }
        _slideSequence.OnComplete(() =>
        {
            _slideState = SlideState.Hidden;
            onComplete?.Invoke();
        });
    }

    //--- Private Methods ---// 
    private bool IsCheckFailed()
    {
        if (_playButton == null)
        {
            CustomDebug.LogError("Play Button is not assigned in the inspector.");
            return true;
        }
        if (_optionsButton == null)
        {
            CustomDebug.LogError("Options Button is not assigned in the inspector.");
            return true;
        }
        if (_exitButton == null)
        {
            CustomDebug.LogError("Exit Button is not assigned in the inspector.");
            return true;
        }
        if (_flowController == null)
        {
            CustomDebug.LogError("TitleFlowController is not assigned in the inspector.");
            return true;
        }
        return false;
    }

    private void InitializeView()
    {
        InitializeSlideTargets();
        _titlePresenter = new TitlePresenter(this, _flowController);
        _exitButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnExitButtonClicked(); });
        _playButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnPlayButtonClicked(); });
        _optionsButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnOptionsButtonClicked(); });
        UIButtonFeedback.Ensure(_playButton);
        UIButtonFeedback.Ensure(_optionsButton);
        UIButtonFeedback.Ensure(_exitButton);
        UIDefaultSelection.Ensure(gameObject, () => _playButton);
        GraphicManager.Instance.SetDoFMode("Title");
    }

    private void InitializeSlideTargets()
    {
        _canvasRect = GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;

        // 인스펙터 미지정 시 기본 순서: Play -> Options -> Exit (각 버튼의 부모 컨테이너)
        if (_slideTargets.Count == 0)
        {
            _slideTargets.Add(_playButton.transform.parent as RectTransform);
            _slideTargets.Add(_optionsButton.transform.parent as RectTransform);
            _slideTargets.Add(_exitButton.transform.parent as RectTransform);
        }
        _slideTargets.RemoveAll(target => target == null);

        _homePositions = new Vector2[_slideTargets.Count];
        for (int i = 0; i < _slideTargets.Count; i++)
        {
            _homePositions[i] = _slideTargets[i].anchoredPosition;
        }
    }

    private void SnapToHidden()
    {
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < _slideTargets.Count; i++)
        {
            _slideTargets[i].anchoredPosition = GetHiddenPosition(i);
        }
    }

    /// <summary>
    /// 홈 위치에 있을 때의 오른쪽 끝이 캔버스 왼쪽 끝(+여백) 밖으로 나가는 위치를 계산한다.
    /// </summary>
    private Vector2 GetHiddenPosition(int index)
    {
        RectTransform target = _slideTargets[index];
        RectTransform parent = (RectTransform)target.parent;
        Vector2 home = _homePositions[index];

        target.GetWorldCorners(_cornerBuffer);
        float rightNow = _canvasRect.InverseTransformPoint(_cornerBuffer[2]).x;

        // 현재 위치 -> 홈 위치 보정 (부모 로컬 단위를 캔버스 로컬 단위로 변환)
        Vector3 homeDeltaWorld = parent.TransformVector(new Vector3(home.x - target.anchoredPosition.x, 0f, 0f));
        float rightAtHome = rightNow + _canvasRect.InverseTransformVector(homeDeltaWorld).x;

        float distanceInCanvas = rightAtHome - _canvasRect.rect.xMin + _offscreenMargin;
        float parentUnitsPerCanvasUnit = parent.InverseTransformVector(_canvasRect.TransformVector(Vector3.right)).x;

        return new Vector2(home.x - distanceInCanvas * parentUnitsPerCanvasUnit, home.y);
    }

    private void DisposeButtonHandlers()
    {
        if (_playButton)
        {
            _playButton.onClick.RemoveAllListeners();
        }

        if (_optionsButton)
        {
            _optionsButton.onClick.RemoveAllListeners();
        }

        if (_exitButton)
        {
            _exitButton.onClick.RemoveAllListeners();
        }
    }
    private void PlayClickSound()
    {
        SoundManager.Instance.PlaySFX(SoundType.SFX_ButtonClick);
    }
}
