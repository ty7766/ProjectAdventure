using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
            GameObject oldChip = _chipContainer.GetChild(i).gameObject;
            oldChip.SetActive(false);
            Destroy(oldChip);
        }

        // 키가 둘 이상이면(QE 등) 칩 하나에 "Q 또는 E"처럼 이어서 표시한다
        if (keyPaths.Length == 1)
        {
            CreateChip(template, TutorialKeyLabelUtil.GetLabel(keyPaths[0]));
        }
        else
        {
            string[] labels = new string[keyPaths.Length];
            for (int i = 0; i < keyPaths.Length; i++)
            {
                labels[i] = TutorialKeyLabelUtil.GetLabel(keyPaths[i]);
            }
            CreateChip(template, string.Join(" 또는 ", labels));
        }

        template.SetActive(false);

        // 칩 개수/텍스트 확정 후 컨테이너(Content Size Fitter)를 즉시 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(_chipContainer);
    }

    /// <summary>칩 템플릿을 복제해 라벨을 채우고, 텍스트 기준 크기 계산을 즉시 반영합니다.</summary>
    private void CreateChip(GameObject template, string label)
    {
        GameObject chip = Instantiate(template, _chipContainer);
        chip.SetActive(true);
        TMP_Text text = chip.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
        // 텍스트 교체 후 ContentSizeFitter 계산을 즉시 반영
        if (chip.transform is RectTransform chipRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipRect);
        }
    }
}
