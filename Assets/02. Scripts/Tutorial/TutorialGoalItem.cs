using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

/// <summary>
/// 튜토리얼 목표 아이템 한 줄 (신규 디자인). 디자이너가 만든 프리팹 구조에 직접 붙어 동작한다.
/// - 등장: 화면(우측) 밖에서 안으로 슬라이드 인 (DOTween + Ease)
/// - 완료: 체크박스 체크 팝 애니메이션 + 아이템 딤다운
/// 부모의 Vertical Layout Group이 배치를 담당하므로 이 컴포넌트는 BG(RectTransform)만 움직인다.
/// </summary>
public class TutorialGoalItem : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("참조")]
    [SerializeField, Tooltip("슬라이드/페이드 대상 컨테이너 (BG)")]
    private RectTransform _contentRect;
    [SerializeField, Tooltip("BG에 부착된 CanvasGroup (없으면 인스펙터에서 연결되어야 함)")]
    private CanvasGroup _contentCanvasGroup;
    [SerializeField, Tooltip("목표 문구 (Prompt)")]
    private TMP_Text _promptText;
    [SerializeField, Tooltip("키캡 칩 부모 (KeyGuide). 첫 번째 자식이 칩 템플릿으로 사용된다")]
    private RectTransform _keyGuideContainer;
    [SerializeField, Tooltip("체크박스 프레임 (Checkbox)")]
    private RectTransform _checkboxRect;
    [SerializeField, Tooltip("체크 아이콘 (Checkbox/Checked)")]
    private Image _checkedImage;

    [Header("애니메이션 설정")]
    [SerializeField, Tooltip("슬라이드 인 시간(초)")]
    private float _slideInDuration = 0.35f;
    [SerializeField, Tooltip("체크 팝 시간(초)")]
    private float _checkPopDuration = 0.25f;
    [SerializeField, Tooltip("완료 시 아이템 딤 알파")]
    private float _completedDimAlpha = 0.45f;

    //--- Fields ---//
    private ProceduralImage _backgroundImage;
    private Vector2 _contentAnchoredPosition;
    private Color _backgroundColor;
    private Tween _slideTween;
    private Tween _checkTween;

    /// <summary>완료 여부</summary>
    public bool IsCompleted { get; private set; }

    //--- Unity Methods ---//
    private void Awake()
    {
        CaptureDefaults();
    }

    //--- Public Methods ---//
    /// <summary>재사용 대비 시각 상태를 초기화합니다. (컨텐츠 배치는 부모 뷰가 담당)</summary>
    public void ResetVisual()
    {
        if (_contentRect == null)
        {
            return;
        }

        IsCompleted = false;

        if (_contentCanvasGroup != null)
        {
            _contentCanvasGroup.DOKill();
        }
        if (_checkedImage != null)
        {
            _checkedImage.gameObject.SetActive(false);
        }
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _backgroundColor;
        }
        if (_promptText != null)
        {
            _promptText.color = Color.white;
        }
        _contentRect.anchoredPosition = _contentAnchoredPosition;
        if (_contentCanvasGroup != null)
        {
            _contentCanvasGroup.alpha = 0f; // 등장 애니메이션에서 페이드 인
        }
    }

    /// <summary>
    /// 아이템 내용을 채웁니다. keyPaths에는 "&lt;Keyboard&gt;/w" 형태의 바인딩 경로가 담긴다.
    /// </summary>
    public void Setup(string description, string[] keyPaths)
    {
        CaptureDefaults();

        if (_promptText != null)
        {
            _promptText.text = description;
        }
        BuildKeyChips(keyPaths);
    }

    /// <summary>
    /// 등장 연출: 화면 밖(우측)에서 안으로 슬라이드 인 + 페이드 인.
    /// </summary>
    public void PlaySlideIn(float slideOffset, float delay)
    {
        if (_contentRect == null)
        {
            return;
        }

        KillSlideTween();

        _contentRect.anchoredPosition = _contentAnchoredPosition + new Vector2(slideOffset, 0f);
        if (_contentCanvasGroup != null)
        {
            _contentCanvasGroup.alpha = 0f;
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Insert(delay, _contentRect.DOAnchorPos(_contentAnchoredPosition, _slideInDuration)
            .SetEase(Ease.OutCubic));
        if (_contentCanvasGroup != null)
        {
            seq.Insert(delay, _contentCanvasGroup.DOFade(1f, _slideInDuration)
                .SetEase(Ease.OutCubic));
        }
        _slideTween = seq;
    }

    /// <summary>
    /// 완료 연출: 체크박스 체크 팝 + 아이템 딤다운.
    /// </summary>
    public void PlayComplete()
    {
        IsCompleted = true;

        // 슬라이드 인이 끝나지 않은 상태에서 체크될 수 있으므로 즉시 완료 상태로 만든다
        KillSlideTween();
        _contentRect.anchoredPosition = _contentAnchoredPosition;
        if (_contentCanvasGroup != null)
        {
            _contentCanvasGroup.DOKill();
            _contentCanvasGroup.alpha = 1f;
        }

        // 1) 체크 아이콘 팝 (OutBack)
        if (_checkedImage != null)
        {
            _checkedImage.gameObject.SetActive(true);
            _checkedImage.transform.localScale = Vector3.one * 0.3f;
            _checkTween?.Kill();
            _checkTween = _checkedImage.transform.DOScale(Vector3.one, _checkPopDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        // 2) 체크박스 프레임 펀치
        if (_checkboxRect != null)
        {
            _checkTween?.Kill();
            _checkTween = _checkboxRect.DOPunchScale(Vector3.one * 0.25f, _checkPopDuration, 2, 0.5f)
                .SetUpdate(true);
        }

        // 3) 아이템 딤다운
        if (_backgroundImage != null)
        {
            Color dim = _backgroundColor;
            dim.a = _completedDimAlpha * _backgroundColor.a;
            _backgroundImage.DOColor(dim, _checkPopDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        if (_promptText != null)
        {
            _promptText.DOColor(new Color(1f, 1f, 1f, 0.45f), _checkPopDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    //--- Private Methods ---//
    /// <summary>프리팹의 기본 시각 상태를 캡처합니다. (리셋/딤 대비)</summary>
    private void CaptureDefaults()
    {
        if (_contentRect == null)
        {
            return;
        }
        _contentAnchoredPosition = _contentRect.anchoredPosition;
        _backgroundImage = _contentRect.GetComponent<ProceduralImage>();
        if (_backgroundImage != null)
        {
            _backgroundColor = _backgroundImage.color;
        }
    }

    private void KillSlideTween()
    {
        _slideTween?.Kill();
        _slideTween = null;
    }

    /// <summary>바인딩 경로 목록을 키캡 칩으로 절차 생성합니다. 프리팹의 첫 번째 자식 칩을 템플릿으로 복제한다.</summary>
    private void BuildKeyChips(string[] keyPaths)
    {
        if (_keyGuideContainer == null)
        {
            return;
        }

        // 키 가이드가 없는 목표면 칩 영역을 통째로 숨겨 텍스트 정렬을 유지한다
        if (keyPaths == null || keyPaths.Length == 0)
        {
            _keyGuideContainer.gameObject.SetActive(false);
            return;
        }
        _keyGuideContainer.gameObject.SetActive(true);

        if (_keyGuideContainer.childCount == 0)
        {
            return;
        }

        // 이전 Setup에서 복제된 칩을 모두 제거 (첫 자식 = 템플릿 보존)
        Transform template = _keyGuideContainer.GetChild(0);
        for (int i = _keyGuideContainer.childCount - 1; i >= 1; i--)
        {
            Destroy(_keyGuideContainer.GetChild(i).gameObject);
        }

        // 모든 키를 템플릿 복제로 생성한다. 템플릿 자체는 마지막에 숨겨 다음 Setup의 원본 역할을 유지.
        for (int i = 0; i < keyPaths.Length; i++)
        {
            RectTransform chipRect = Instantiate(template, _keyGuideContainer) as RectTransform;
            chipRect.gameObject.SetActive(true);

            TMP_Text chipText = chipRect.GetComponentInChildren<TMP_Text>(true);
            if (chipText != null)
            {
                chipText.text = TutorialKeyLabelUtil.GetLabel(keyPaths[i]);
            }
        }
        template.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        KillSlideTween();
        _checkTween?.Kill();
        _checkTween = null;
    }
}
