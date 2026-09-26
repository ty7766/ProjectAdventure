using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

/// <summary>
/// 목표 패널의 개별 목표 한 줄. 프리팹으로 제작하며 TutorialView가 인스턴스화한다.
/// 레이아웃은 부모의 Vertical Layout Group이 담당하므로 이 컴포넌트는
/// 알파 페이드 등 시각 연출만 수행한다. (위치 애니메이션으로 레이아웃이 깨지는 문제 방지)
/// </summary>
public class TutorialObjectiveRow : MonoBehaviour
{
    //--- Components ---//
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private RectTransform _chipContainer;
    [SerializeField] private GameObject _checkObject;
    [SerializeField] private ProceduralImage _dimPanel;

    //--- Properties ---//
    public bool IsCompleted { get; private set; }

    //--- Unity Methods ---//
    private void Awake()
    {
        ResetVisual();
    }

    /// <summary>재사용 대비 시각 상태를 초기화합니다.</summary>
    public void ResetVisual()
    {
        IsCompleted = false;
        if (_checkObject != null)
        {
            _checkObject.SetActive(false);
        }
        if (_dimPanel != null)
        {
            Color c = _dimPanel.color;
            c.a = 0f;
            _dimPanel.color = c;
        }
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f; // 등장 애니메이션에서 페이드 인
        }
    }

    //--- Public Methods ---//
    /// <summary>행 내용을 채웁니다. keyPaths에는 "&lt;Keyboard&gt;/w" 형태의 바인딩 경로가 담긴다.</summary>
    public void Setup(string description, string[] keyPaths)
    {
        _descriptionText.text = description;
        BuildKeyChips(keyPaths);
    }

    /// <summary>등장 연출: 페이드 인만 적용한다. (레이아웃과 충돌하지 않음)</summary>
    public void PlayFadeIn(float delay)
    {
        if (_canvasGroup == null)
        {
            return;
        }
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0f;
        _canvasGroup.DOFade(1f, 0.25f).SetDelay(delay).SetUpdate(true);
    }

    /// <summary>완료 마킹: 체크 팝 + 딤 패널 페이드 인.</summary>
    public void PlayComplete()
    {
        IsCompleted = true;

        if (_checkObject != null)
        {
            _checkObject.SetActive(true);
            _checkObject.transform.localScale = Vector3.one * 0.3f;
            _checkObject.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        if (_dimPanel != null)
        {
            _dimPanel.DOFade(0.5f, 0.3f).SetUpdate(true);
        }

        if (_canvasGroup != null)
        {
            // 진행 중이던 페이드인을 즉시 완료시킨다
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 1f;
        }
    }

    /// <summary>바인딩 경로 목록을 키캡 칩으로 절차 생성합니다. 프리팹의 첫 자식 칩을 템플릿으로 복제한다.</summary>
    private void BuildKeyChips(string[] keyPaths)
    {
        if (_chipContainer == null)
        {
            return;
        }

        // 키 가이드가 없는 목표면 칩 영역을 통째로 숨겨 텍스트 정렬을 유지한다
        if (keyPaths == null || keyPaths.Length == 0)
        {
            _chipContainer.gameObject.SetActive(false);
            return;
        }
        _chipContainer.gameObject.SetActive(true);

        GameObject template = _chipContainer.GetChild(0).gameObject;
        for (int i = _chipContainer.childCount - 1; i >= 1; i--)
        {
            Destroy(_chipContainer.GetChild(i).gameObject);
        }

        foreach (string path in keyPaths)
        {
            GameObject chip = Instantiate(template, _chipContainer);
            chip.SetActive(true);
            TMP_Text text = chip.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = TutorialKeyLabelUtil.GetLabel(path);
            }
        }

        template.SetActive(false);
    }
}
