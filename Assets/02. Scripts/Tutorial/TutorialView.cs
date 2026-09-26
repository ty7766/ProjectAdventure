using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 튜토리얼 목표 패널 UI. 프리팹 참조 기반으로 우상단(알림 영역 회피, 중앙 근접)에 배치되며
/// 섹션 전환 / 목표 완료 / 등장 애니메이션을 DOTween으로 연출한다.
/// 행 배치는 Vertical Layout Group + ContentSizeFitter에 맡기고 코드는 알파 연출만 한다.
/// </summary>
public class TutorialView : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("패널 루트")]
    [SerializeField]
    private CanvasGroup _panelRoot;
    [SerializeField]
    private RectTransform _panelRect;

    [Header("섹션 헤더")]
    [SerializeField]
    private TMP_Text _sectionNumberText;
    [SerializeField]
    private TMP_Text _sectionTitleText;

    [Header("목표 목록")]
    [SerializeField, Tooltip("목표 행이 붙는 컨테이너 (Vertical Layout Group 권장)")]
    private RectTransform _objectiveContainer;
    [SerializeField, Tooltip("목표 행 프리팹 (TutorialObjectiveRow)")]
    private TutorialObjectiveRow _objectiveRowPrefab;

    [Header("애니메이션 설정")]
    [SerializeField]
    private float _panelSlideDistance = 40f;
    [SerializeField]
    private float _panelSlideDuration = 0.3f;
    [SerializeField]
    private float _rowStagger = 0.07f;

    //--- Fields ---//
    private readonly List<TutorialObjectiveRow> _rows = new List<TutorialObjectiveRow>();
    private GameControls _controls;
    private Coroutine _fadeCoroutine;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (_panelRect == null)
        {
            _panelRect = (RectTransform)transform;
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_panelRoot != null)
        {
            _panelRoot.DOKill();
        }
    }

    //--- Public Methods ---//
    /// <summary>섹션 전체를 패널에 렌더링하고 등장 애니메이션을 재생합니다.</summary>
    public void ShowSection(TutorialSectionConfig[] sections, int index)
    {
        StopFade();
        ClearRows();

        TutorialSectionConfig section = sections[index];

        _sectionNumberText.text = $"{index + 1}";
        _sectionTitleText.text = section.Title;

        for (int i = 0; i < section.Objectives.Length; i++)
        {
            TutorialObjectiveConfig config = section.Objectives[i];
            TutorialObjectiveRow row = Instantiate(_objectiveRowPrefab, _objectiveContainer);
            row.gameObject.SetActive(true); // 템플릿이 비활성이므로 인스턴스도 비활성으로 생성됨 → 반드시 활성화
            row.ResetVisual();
            row.Setup(config.Description, ResolveKeyPaths(config));
            _rows.Add(row);
        }

        PlayPanelEntrance();

        // 레이아웃 재계산 후 행 페이드인 (한 프레임 대기)
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeRowsNextFrame());
    }

    /// <summary>인덱스의 목표를 완료 처리합니다.</summary>
    public void SetObjectiveCompleted(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            return;
        }
        _rows[index].PlayComplete();
    }

    /// <summary>카메라 이동 등을 위해 현재 섹션 패널을 숨깁니다.</summary>
    public void HidePanel()
    {
        StopFade();
        if (_panelRoot != null)
        {
            _panelRoot.DOKill();
            _panelRoot.alpha = 0f;
            _panelRoot.interactable = false;
            _panelRoot.blocksRaycasts = false;
        }
    }

    /// <summary>튜토리얼 전체 종료 처리.</summary>
    public void HideAll()
    {
        HidePanel();
    }

    //--- Private Methods ---//
    /// <summary>InputManager로부터 GameControls를 지연 로드합니다.</summary>
    private GameControls Controls
    {
        get { return _controls ??= InputManager.Instance.GameControls; }
    }

    /// <summary>목표 구성에서 실제 표시할 키 바인딩 경로를 얻습니다. (리바인딩 반영)</summary>
    private string[] ResolveKeyPaths(TutorialObjectiveConfig config)
    {
        if (Controls == null)
        {
            return System.Array.Empty<string>();
        }

        switch (config.CompletionType)
        {
            case TutorialCompletionType.MoveInDirection:
                return new[] { GetMoveBindingPath(config.MoveDirection) };

            case TutorialCompletionType.SelectMap:
                return config.SelectDirection < 0
                    ? new[] { GetMapBindingPath("SelectMap", "negative") }
                    : new[] { GetMapBindingPath("SelectMap", "positive") };

            case TutorialCompletionType.ChangeMap:
                // "Q 또는 E" - 두 키 모두 표시
                return new[]
                {
                    GetMapBindingPath("ChangeMap", "negative"),
                    GetMapBindingPath("ChangeMap", "positive"),
                };

            default:
                // FindGem / CollectGem / ReachGoal - 키 가이드 없음
                return System.Array.Empty<string>();
        }
    }

    private string GetMoveBindingPath(TutorialMoveDirection direction)
    {
        string bindingName = direction switch
        {
            TutorialMoveDirection.Up => "up",
            TutorialMoveDirection.Down => "down",
            TutorialMoveDirection.Left => "left",
            TutorialMoveDirection.Right => "right",
            _ => "up",
        };

        InputAction moveAction = Controls.asset.FindAction("Player/Move");
        return FindBindingPath(moveAction, bindingName, fallback: direction switch
        {
            TutorialMoveDirection.Up => "<Keyboard>/w",
            TutorialMoveDirection.Down => "<Keyboard>/s",
            TutorialMoveDirection.Left => "<Keyboard>/a",
            TutorialMoveDirection.Right => "<Keyboard>/d",
            _ => "<Keyboard>/w",
        });
    }

    private string GetMapBindingPath(string actionName, string bindingName)
    {
        string fallback = actionName == "SelectMap"
            ? (bindingName == "negative" ? "<Keyboard>/leftArrow" : "<Keyboard>/rightArrow")
            : (bindingName == "negative" ? "<Keyboard>/q" : "<Keyboard>/e");

        InputAction action = Controls.asset.FindAction($"Map/{actionName}");
        return FindBindingPath(action, bindingName, fallback);
    }

    private static string FindBindingPath(InputAction action, string bindingName, string fallback)
    {
        if (action == null)
        {
            return fallback;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isPartOfComposite && binding.name == bindingName)
            {
                return binding.effectivePath;
            }
        }
        return fallback;
    }

    private IEnumerator FadeRowsNextFrame()
    {
        // Vertical Layout Group이 새 행들을 배치할 때까지 대기
        yield return null;
        yield return null;

        _fadeCoroutine = null;
        float delay = 0.1f;
        foreach (TutorialObjectiveRow row in _rows)
        {
            if (row != null)
            {
                row.PlayFadeIn(delay);
                delay += _rowStagger;
            }
        }
    }

    private void PlayPanelEntrance()
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.DOKill();

        Vector2 target = _panelRect.anchoredPosition;
        _panelRect.anchoredPosition = target + new Vector2(_panelSlideDistance, 0f);
        _panelRoot.alpha = 0f;
        _panelRoot.interactable = true;
        _panelRoot.blocksRaycasts = true;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(_panelRoot.DOFade(1f, _panelSlideDuration));
        seq.Join(_panelRect.DOAnchorPos(target, _panelSlideDuration).SetEase(Ease.OutCubic));
    }

    private void StopFade()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    private void ClearRows()
    {
        foreach (TutorialObjectiveRow row in _rows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }
        _rows.Clear();
    }
}
