using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 신규 튜토리얼 패널 UI. 디자이너가 만든 프리팹(HUD/TutorialHUD) 구조에 직접 붙어 동작한다.
/// - 섹션 시작: 타이틀 먼저 슬라이드 인 → 각 목표 아이템 순차 슬라이드 인
/// - 섹션 종료: 패널 전체를 화면 밖으로 슬라이드 아웃 후 다음 섹션 진행
/// - 목표 완료: 개별 아이템의 체크박스 체크 애니메이션
/// 모든 애니메이션은 DOTween + Ease 보간을 사용한다.
/// </summary>
public class TutorialView : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("패널 루트")]
    [SerializeField, Tooltip("튜토리얼 패널 루트 (CanvasGroup 포함, 슬라이드 대상)")]
    private CanvasGroup _panelRoot;
    [SerializeField, Tooltip("튜토리얼 패널 RectTransform (슬라이드 대상)")]
    private RectTransform _panelRect;

    [Header("타이틀")]
    [SerializeField, Tooltip("섹션 타이틀 슬라이드 대상 (Title 내부의 콘텐츠). HLG 정렬 충돌을 피하기 위해 내부 RectTransform을 쓴다")]
    private RectTransform _titleRect;
    [SerializeField, Tooltip("섹션 번호 텍스트 (Round 배지)")]
    private TMP_Text _sectionNumberText;
    [SerializeField, Tooltip("섹션 제목 텍스트")]
    private TMP_Text _sectionTitleText;

    [Header("목표 목록")]
    [SerializeField, Tooltip("목표 아이템들이 붙는 컨테이너")]
    private RectTransform _itemContainer;
    [SerializeField, Tooltip("목표 아이템 컴포넌트들 (프리팹에 미리 배치된 순서대로)")]
    private TutorialGoalItem[] _goalItems;

    [Header("애니메이션 설정")]
    [SerializeField, Tooltip("패널 슬라이드(인/아웃) 시간(초)")]
    private float _panelSlideDuration = 0.45f;
    [SerializeField, Tooltip("패널 슬라이드 아웃 거리(px)")]
    private float _panelSlideOutDistance = 600f;
    [SerializeField, Tooltip("타이틀 슬라이드 인 시간(초)")]
    private float _titleSlideDuration = 0.3f;
    [SerializeField, Tooltip("아이템 슬라이드 인 거리(px)")]
    private float _itemSlideDistance = 500f;
    [SerializeField, Tooltip("아이템 사이 순차 딜레이(초)")]
    private float _itemStagger = 0.12f;

    //--- Fields ---//
    private Tween _panelTween;
    private Tween _titleTween;
    private Coroutine _showCoroutine;
    private Vector2 _panelHomePosition;
    private bool _panelHomeCaptured;

    /// <summary>섹션 전환이 진행 중인지 (매니저가 입력 처리를 막는 데 사용)</summary>
    public bool IsTransitioning { get; private set; }

    //--- Unity Methods ---//
    private void Awake()
    {
        if (_panelRect == null)
        {
            _panelRect = (RectTransform)transform;
        }
        EnsureTitleSlideRoot();

        // 시작 시 패널은 감춰둔다 (ShowSection에서 슬라이드 인)
        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        if (_panelTween != null)
        {
            _panelTween.Kill();
        }
        if (_titleTween != null)
        {
            _titleTween.Kill();
        }
    }

    //--- Public Methods ---//
    /// <summary>섹션을 표시합니다. (타이틀 → 목표 아이템 순차 슬라이드 인)</summary>
    public void ShowSection(TutorialSectionConfig[] sections, int index)
    {
        TutorialSectionConfig section = sections[index];

        if (_sectionNumberText != null)
        {
            _sectionNumberText.text = $"{index + 1}";
        }
        if (_sectionTitleText != null)
        {
            _sectionTitleText.text = section.Title;
        }

        // 아이템 내용 채우기 (프리팹에 배치된 개수만큼만 표시)
        int itemCount = Mathf.Min(section.Objectives.Length, _goalItems.Length);
        for (int i = 0; i < _goalItems.Length; i++)
        {
            TutorialGoalItem item = _goalItems[i];
            if (item == null)
            {
                continue;
            }

            bool used = i < itemCount;
            item.gameObject.SetActive(used);
            if (used)
            {
                TutorialObjectiveConfig config = section.Objectives[i];
                item.ResetVisual();
                item.Setup(config.Description, ResolveKeyPaths(config));
            }
        }

        // 등장 시퀀스: 패널 인 → 타이틀 → 아이템 순차
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }
        _showCoroutine = StartCoroutine(ShowSectionRoutine());
    }

    /// <summary>인덱스의 목표를 완료 처리합니다.</summary>
    public void SetObjectiveCompleted(int index)
    {
        if (index < 0 || index >= _goalItems.Length)
        {
            return;
        }
        _goalItems[index].PlayComplete();
    }

    /// <summary>
    /// 현재 섹션 패널을 화면 밖으로 슬라이드 아웃하고 완료 시 onDone을 호출합니다.
    /// </summary>
    public void HidePanel(System.Action onDone = null)
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        IsTransitioning = true;

        if (_panelRect == null || !gameObject.activeInHierarchy)
        {
            CompleteHide(onDone);
            return;
        }

        Vector2 target = _panelRect.anchoredPosition + new Vector2(_panelSlideOutDistance, 0f);
        _panelTween?.Kill();
        _panelTween = _panelRect.DOAnchorPos(target, _panelSlideDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() => CompleteHide(onDone));
    }

    /// <summary>튜토리얼 전체 종료 처리.</summary>
    public void HideAll()
    {
        HidePanel(() => SetPanelVisible(false));
    }

    //--- Private Methods ---//
    /// <summary>
    /// Title(HorizontalLayoutGroup) 아래 콘텐츠를 감싸는 슬라이드 루트를 보장합니다.
    /// HLG가 Title의 위치를 통제하므로, 슬라이드 애니메이션은 내부 루트를 움직여 충돌을 피한다.
    /// </summary>
    private void EnsureTitleSlideRoot()
    {
        if (_titleRect == null || _titleRect.parent != transform.parent)
        {
            return; // 이미 내부 콘텐츠가 지정되어 있음
        }

        // _titleRect가 Title(HLG) 자신이면 내부 루트를 만들어 자식들을 옮긴다
        GameObject root = new GameObject("TitleSlideRoot", typeof(RectTransform));
        RectTransform rt = (RectTransform)root.transform;
        rt.SetParent(_titleRect, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        for (int i = _titleRect.childCount - 1; i >= 0; i--)
        {
            Transform child = _titleRect.GetChild(i);
            if (child == rt)
            {
                continue;
            }
            child.SetParent(rt, false);
        }

        _titleRect = rt;
    }

    private IEnumerator ShowSectionRoutine()
    {
        // 1) 홈 위치 캡처 (Awake에서 프리팹 값과 다를 수 있으므로 ShowSection 시작 시점 앞둔 위치 기준)
        if (!_panelHomeCaptured)
        {
            _panelHomePosition = ComputePanelHomePosition();
            _panelHomeCaptured = true;
        }

        // 2) 패널 등장 준비: 알파 0 + 화면 밖 오른쪽에서 시작
        SetPanelVisible(false);
        _panelRect.anchoredPosition = _panelHomePosition + new Vector2(_panelSlideOutDistance, 0f);
        for (int i = 0; i < _goalItems.Length; i++)
        {
            _goalItems[i]?.ResetVisual();
        }

        // 타이틀 위치를 화면 밖 오른쪽으로
        Vector2 titleTarget = _titleRect.anchoredPosition;
        _titleRect.anchoredPosition = titleTarget + new Vector2(_panelSlideOutDistance, 0f);

        yield return null; // 레이아웃 정리 한 프레임

        // 3) 패널 슬라이드 인 (화면 밖 → 원위치)
        _panelTween?.Kill();
        Sequence entrance = DOTween.Sequence().SetUpdate(true);
        entrance.Join(_panelRect.DOAnchorPos(_panelHomePosition, _panelSlideDuration).SetEase(Ease.OutCubic));
        entrance.Join(_panelRoot.DOFade(1f, _panelSlideDuration).SetEase(Ease.OutQuad));
        _panelTween = entrance;

        yield return new WaitForSecondsRealtime(_panelSlideDuration * 0.8f);

        // 4) 타이틀 먼저 슬라이드 인
        _titleTween?.Kill();
        _titleTween = _titleRect.DOAnchorPos(titleTarget, _titleSlideDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        // 타이틀 슬라이드가 거의 완료된 뒤 아이템 시작
        yield return new WaitForSecondsRealtime(_titleSlideDuration * 0.6f);

        // 5) 각 목표 아이템 순차 슬라이드 인
        float delay = 0f;
        for (int i = 0; i < _goalItems.Length; i++)
        {
            TutorialGoalItem item = _goalItems[i];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }
            item.PlaySlideIn(_itemSlideDistance, delay);
            delay += _itemStagger;
        }

        _showCoroutine = null;
        IsTransitioning = false;
    }

    private void CompleteHide(System.Action onDone)
    {
        _panelTween = null;
        SetPanelVisible(false);
        onDone?.Invoke();
        IsTransitioning = false;
    }

    /// <summary>패널 표시/숨김 기본 상태를 설정합니다.</summary>
    private void SetPanelVisible(bool visible)
    {
        if (_panelRoot == null)
        {
            return;
        }
        _panelRoot.alpha = visible ? 1f : 0f;
        _panelRoot.interactable = visible;
        _panelRoot.blocksRaycasts = visible;
    }

    /// <summary>패널의 원래(디자이너 지정) 위치를 계산합니다.</summary>
    private Vector2 ComputePanelHomePosition()
    {
        // 프리팹 원래 값 (Layout Group에 묶이지 않은 루트 오브젝트)
        // Canvas Scale과 무관한 anchoredPosition 기준값: VerticalStack과 동일한 (0, -128)
        return new Vector2(0f, -128f);
    }

    /// <summary>목표 구성에서 실제 표시할 키 바인딩 경로를 얻습니다. (리바인딩 반영)</summary>
    private string[] ResolveKeyPaths(TutorialObjectiveConfig config)
    {
        if (InputManager.Instance == null || InputManager.Instance.GameControls == null)
        {
            return System.Array.Empty<string>();
        }

        GameControls controls = InputManager.Instance.GameControls;

        switch (config.CompletionType)
        {
            case TutorialCompletionType.MoveInDirection:
                return new[] { GetMoveBindingPath(controls, config.MoveDirection) };

            case TutorialCompletionType.SelectMap:
                return config.SelectDirection < 0
                    ? new[] { GetMapBindingPath(controls, "SelectMap", "negative") }
                    : new[] { GetMapBindingPath(controls, "SelectMap", "positive") };

            case TutorialCompletionType.ChangeMap:
                // "Q 또는 E" - 두 키 모두 표시
                return new[]
                {
                    GetMapBindingPath(controls, "ChangeMap", "negative"),
                    GetMapBindingPath(controls, "ChangeMap", "positive"),
                };

            default:
                // FindGem / CollectGem / ReachGoal - 키 가이드 없음
                return System.Array.Empty<string>();
        }
    }

    private static string GetMoveBindingPath(GameControls controls, TutorialMoveDirection direction)
    {
        string bindingName = direction switch
        {
            TutorialMoveDirection.Up => "up",
            TutorialMoveDirection.Down => "down",
            TutorialMoveDirection.Left => "left",
            TutorialMoveDirection.Right => "right",
            _ => "up",
        };

        UnityEngine.InputSystem.InputAction moveAction = controls.asset.FindAction("Player/Move");
        return FindBindingPath(moveAction, bindingName, fallback: direction switch
        {
            TutorialMoveDirection.Up => "<Keyboard>/w",
            TutorialMoveDirection.Down => "<Keyboard>/s",
            TutorialMoveDirection.Left => "<Keyboard>/a",
            TutorialMoveDirection.Right => "<Keyboard>/d",
            _ => "<Keyboard>/w",
        });
    }

    private static string GetMapBindingPath(GameControls controls, string actionName, string bindingName)
    {
        string fallback = actionName == "SelectMap"
            ? (bindingName == "negative" ? "<Keyboard>/leftArrow" : "<Keyboard>/rightArrow")
            : (bindingName == "negative" ? "<Keyboard>/q" : "<Keyboard>/e");

        UnityEngine.InputSystem.InputAction action = controls.asset.FindAction($"Map/{actionName}");
        return FindBindingPath(action, bindingName, fallback);
    }

    private static string FindBindingPath(UnityEngine.InputSystem.InputAction action, string bindingName, string fallback)
    {
        if (action == null)
        {
            return fallback;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            UnityEngine.InputSystem.InputBinding binding = action.bindings[i];
            if (binding.isPartOfComposite && binding.name == bindingName)
            {
                return binding.effectivePath;
            }
        }
        return fallback;
    }
}
