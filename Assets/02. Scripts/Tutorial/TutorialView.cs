using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

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
    [SerializeField, Tooltip("섹션 타이틀 슬라이드 대상. 부모에 Layout Group이 없는 RectTransform이어야 한다 (레이아웃이 위치를 덮어쓰지 않도록)")]
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
    [SerializeField, Tooltip("목표 목록 맨 아래의 팁 행 (체크박스를 숨긴 목표 아이템). 섹션에 팁이 있을 때만 표시된다")]
    private TutorialGoalItem _tipItem;
    [SerializeField, Tooltip("팁 행 칩에 표시할 라벨")]
    private string _tipChipLabel = "Tip";

    [Header("터치 칩 아이콘")]
    [SerializeField, Tooltip("이전 맵 선택 터치 버튼의 아이콘 Image (TouchControls/MapControls/Left/Image). 칩이 이 아이콘을 그대로 따라 그린다")]
    private UnityEngine.UI.Image _touchSelectLeftIcon;
    [SerializeField, Tooltip("다음 맵 선택 터치 버튼의 아이콘 Image (TouchControls/MapControls/Right/Image)")]
    private UnityEngine.UI.Image _touchSelectRightIcon;
    [SerializeField, Tooltip("지형 교체 터치 버튼의 아이콘 Image (TouchControls/MapControls/Change/Image)")]
    private UnityEngine.UI.Image _touchChangeMapIcon;

    [Header("패널 위치")]
    [SerializeField, Tooltip("패널이 슬라이드 인 해서 머무는 anchoredPosition (디자이너 지정 홈 위치)")]
    private Vector2 _panelHomePosition = new Vector2(0f, -128f);

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
    private Vector2 _titleHomePosition;
    private bool _titleHomeCaptured;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (_panelRect == null)
        {
            _panelRect = (RectTransform)transform;
        }
        WarnIfTitleUnderLayoutGroup();

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
        if (section.Objectives.Length > _goalItems.Length)
        {
            CustomDebug.LogWarning($"TutorialView: 섹션 '{section.Title}'의 목표({section.Objectives.Length}개)가 " +
                $"목표 아이템 슬롯({_goalItems.Length}개)보다 많아 일부가 표시되지 않습니다. TutorialHUD 프리팹에 슬롯을 추가하세요.");
        }
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
                string description = PlatformCapability.UseTouchUI
                    ? TutorialKeyLabelUtil.GetTouchDescription(config)
                    : config.Description;
                item.Setup(description, ResolveKeyLabels(config), ResolveTouchIcon(config));
            }
        }

        // 팁 행: 섹션에 팁이 있을 때만 목표 목록 아래에 표시
        if (_tipItem != null)
        {
            bool hasTip = !string.IsNullOrEmpty(section.Tip);
            _tipItem.gameObject.SetActive(hasTip);
            if (hasTip)
            {
                _tipItem.ResetVisual();
                _tipItem.Setup(section.Tip, new[] { _tipChipLabel });
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

    /// <summary>연출 없이 즉시 패널을 숨기고 진행 중인 연출을 모두 정리합니다. (튜토리얼 중단 시)</summary>
    public void HideImmediate()
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }
        _panelTween?.Kill();
        _panelTween = null;
        _titleTween?.Kill();
        _titleTween = null;

        if (_titleHomeCaptured && _titleRect != null)
        {
            _titleRect.anchoredPosition = _titleHomePosition;
        }
        if (_panelRect != null)
        {
            _panelRect.anchoredPosition = _panelHomePosition;
        }
        SetPanelVisible(false);
    }

    //--- Private Methods ---//
    /// <summary>
    /// 타이틀 슬라이드 대상의 부모에 Layout Group이 있으면 레이아웃 갱신 때 슬라이드 위치가 덮어써지므로 경고한다.
    /// (런타임에 계층을 재구성하지 않고 프리팹에서 바로잡도록 안내)
    /// </summary>
    private void WarnIfTitleUnderLayoutGroup()
    {
        if (_titleRect != null && _titleRect.parent != null
            && _titleRect.parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            CustomDebug.LogWarning($"TutorialView: 타이틀 슬라이드 대상 '{_titleRect.name}'의 부모에 Layout Group이 있어 " +
                "슬라이드 연출이 레이아웃과 충돌할 수 있습니다. Layout Group 밖의 RectTransform을 지정하세요.", this);
        }
    }

    private IEnumerator ShowSectionRoutine()
    {
        // 1) 타이틀 홈 위치는 최초 1회만 캡처 (이전 연출이 중간에 끊겨 어긋난 위치를 홈으로 삼지 않도록)
        if (!_titleHomeCaptured)
        {
            _titleHomePosition = _titleRect.anchoredPosition;
            _titleHomeCaptured = true;
        }

        // 2) 패널 등장 준비: 알파 0 + 화면 밖 오른쪽에서 시작
        SetPanelVisible(false);
        _panelRect.anchoredPosition = _panelHomePosition + new Vector2(_panelSlideOutDistance, 0f);
        for (int i = 0; i < _goalItems.Length; i++)
        {
            _goalItems[i]?.ResetVisual();
        }
        _tipItem?.ResetVisual();

        // 타이틀 위치를 화면 밖 오른쪽으로
        _titleTween?.Kill();
        Vector2 titleTarget = _titleHomePosition;
        _titleRect.anchoredPosition = titleTarget + new Vector2(_panelSlideOutDistance, 0f);

        yield return null; // 레이아웃 정리 한 프레임

        // 아이템 폭이 확정된 뒤 칩/문구 길이에 맞춰 각 행을 맞추고, 바뀐 아이템 높이로 목록을 다시 쌓는다
        // (패널이 아직 투명한 상태라 크기 변화가 보이지 않는다)
        FitItemLayouts();

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

        // 팁은 목표들 뒤에 이어서 등장
        if (_tipItem != null && _tipItem.gameObject.activeSelf)
        {
            _tipItem.PlaySlideIn(_itemSlideDistance, delay);
        }

        _showCoroutine = null;
    }

    /// <summary>표시 중인 목표 아이템(과 팁 행)의 행 레이아웃을 내용 길이에 맞춥니다.</summary>
    private void FitItemLayouts()
    {
        // 방금 활성화된 행(팁 등)은 호출 시점에 따라 아직 폭이 계산되지 않았을 수 있으므로 먼저 폭을 확정한다
        if (_itemContainer != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_itemContainer);
        }

        for (int i = 0; i < _goalItems.Length; i++)
        {
            TutorialGoalItem item = _goalItems[i];
            if (item != null && item.gameObject.activeSelf)
            {
                item.FitLayout();
            }
        }
        if (_tipItem != null && _tipItem.gameObject.activeSelf)
        {
            _tipItem.FitLayout();
        }
        if (_itemContainer != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_itemContainer);
        }
    }

    private void CompleteHide(System.Action onDone)
    {
        _panelTween = null;
        SetPanelVisible(false);
        onDone?.Invoke();
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

    /// <summary>
    /// 목표 구성에서 실제 표시할 키 가이드 라벨을 얻습니다.
    /// 터치 UI 플랫폼에서는 화면 터치 컨트롤(조이스틱/맵 버튼) 기준 라벨을, 그 외에는 리바인딩이 반영된 키 라벨을 쓴다.
    /// </summary>
    private string[] ResolveKeyLabels(TutorialObjectiveConfig config)
    {
        if (PlatformCapability.UseTouchUI)
        {
            return TutorialKeyLabelUtil.GetTouchLabels(config);
        }

        string[] paths = ResolveKeyPaths(config);
        string[] labels = new string[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            labels[i] = TutorialKeyLabelUtil.GetLabel(paths[i]);
        }
        return labels;
    }

    /// <summary>
    /// 터치 UI 플랫폼에서 칩에 그릴 터치 버튼 아이콘을 얻습니다. 아이콘이 없는 조작(조이스틱 등)이나
    /// 키보드/패드 플랫폼에서는 null을 돌려주며, 이때 칩은 라벨 글자로 표시된다.
    /// </summary>
    private UnityEngine.UI.Image ResolveTouchIcon(TutorialObjectiveConfig config)
    {
        if (!PlatformCapability.UseTouchUI)
        {
            return null;
        }

        return config.CompletionType switch
        {
            TutorialCompletionType.SelectMap => config.SelectDirection < 0 ? _touchSelectLeftIcon : _touchSelectRightIcon,
            TutorialCompletionType.ChangeMap => _touchChangeMapIcon,
            _ => null,
        };
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
