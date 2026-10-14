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
    [SerializeField, Tooltip("키캡 칩 (KeyGuide). 칩 배경이며, 아래 템플릿을 복제한 글자/아이콘이 자식으로 붙는다")]
    private RectTransform _keyGuideContainer;
    [SerializeField, Tooltip("칩 글자 템플릿 (KeyGuide/Text (TMP)). 런타임에 숨기고 복제해서 쓴다")]
    private TMP_Text _chipTextTemplate;
    [SerializeField, Tooltip("칩 아이콘 템플릿 (KeyGuide/Icon). 런타임에 숨기고 복제해서 쓴다")]
    private Image _chipIconTemplate;
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

    [Header("레이아웃 설정")]
    [SerializeField, Tooltip("목표 문구 최소 폭(px). 칩이 길어지면 칩 글자를 줄여서라도 문구 영역을 이만큼 확보한다")]
    private float _minPromptWidth = 160f;

    //--- Fields ---//
    private RectTransform _itemRect;
    private float _defaultItemHeight;
    private float _defaultPromptHeight;
    private bool _layoutDefaultsCaptured;
    private readonly List<GameObject> _chipClones = new List<GameObject>();
    private ProceduralImage _backgroundImage;
    private Vector2 _contentAnchoredPosition;
    private Color _backgroundColor;
    private Tween _slideTween;
    private Tween _checkPopTween;
    private Tween _checkPunchTween;

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
    /// 아이템 내용을 채웁니다. keyLabels에는 키캡 칩에 표시할 라벨("W", "←", "교체" 등)이 담긴다.
    /// iconSource(터치 버튼의 아이콘 Image)가 주어지면 라벨 대신 그 아이콘을 칩에 그대로 보여준다.
    /// (플랫폼별 라벨/아이콘 결정은 TutorialView가 담당)
    /// </summary>
    public void Setup(string description, string[] keyLabels, Image iconSource = null)
    {
        CaptureDefaults();

        if (_promptText != null)
        {
            _promptText.text = KeepWordsTogether(description);
        }
        BuildKeyChips(keyLabels, iconSource);
    }

    /// <summary>
    /// 띄어쓰기 단위 단어를 &lt;nobr&gt;로 감싸 줄바꿈이 단어 사이에서만 일어나게 합니다.
    /// TMP는 한글을 글자 단위로 끊을 수 있다고 보므로 그대로 두면 "없 / 어요!"처럼 단어 중간에서 줄이 바뀐다.
    /// 리치 텍스트 태그가 포함된 단어는 태그가 깨지지 않도록 그대로 둔다.
    /// </summary>
    private static string KeepWordsTogether(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length + 16);
        string[] lines = text.Split('\n');
        for (int l = 0; l < lines.Length; l++)
        {
            if (l > 0)
            {
                builder.Append('\n');
            }

            string[] words = lines[l].Split(' ');
            for (int w = 0; w < words.Length; w++)
            {
                if (w > 0)
                {
                    builder.Append(' ');
                }

                string word = words[w];
                if (word.Length <= 1 || word.IndexOf('<') >= 0 || word.IndexOf('>') >= 0)
                {
                    builder.Append(word);
                }
                else
                {
                    builder.Append("<nobr>").Append(word).Append("</nobr>");
                }
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 칩/문구 길이에 맞춰 한 줄 레이아웃을 맞춥니다. 부모 레이아웃이 아이템 폭을 확정한 뒤 호출해야 한다.
    /// - 문구 폭 = 행 폭 - (패딩 + 체크박스 + 칩 + 간격). 칩이 너무 길면 칩 글자를 줄여 문구 최소 폭을 확보한다
    /// - 최소 글자 크기로도 기본 높이에 다 들어가지 않으면 아이템 높이를 늘려 글자가 잘리지 않게 한다
    /// 아이템 높이가 바뀔 수 있으므로 호출 후 부모 Layout Group을 다시 계산해야 한다.
    /// </summary>
    public void FitLayout()
    {
        // 폭이 아직 확정되지 않았으면(레이아웃 전) 칩을 잘못 축소하지 않도록 건너뛴다
        if (_contentRect == null || _promptText == null || _contentRect.rect.width <= 0f)
        {
            return;
        }
        CaptureLayoutDefaults();

        RectTransform promptRect = _promptText.rectTransform;
        HorizontalLayoutGroup row = _contentRect.GetComponent<HorizontalLayoutGroup>();
        float spacing = row != null ? row.spacing : 0f;
        float rowWidth = _contentRect.rect.width - (row != null ? row.padding.horizontal : 0f);

        // 문구/칩 외의 고정 요소(체크박스 등)와 요소 사이 간격을 뺀다
        bool hasChips = _keyGuideContainer != null && _keyGuideContainer.gameObject.activeSelf;
        int laidOutCount = 0;
        for (int i = 0; i < _contentRect.childCount; i++)
        {
            RectTransform child = (RectTransform)_contentRect.GetChild(i);
            if (!child.gameObject.activeSelf || IsLayoutIgnored(child))
            {
                continue;
            }
            laidOutCount++;
            if (child != promptRect && child != _keyGuideContainer)
            {
                rowWidth -= child.rect.width;
            }
        }
        rowWidth -= spacing * Mathf.Max(0, laidOutCount - 1);

        if (hasChips)
        {
            FitChipsToWidth(rowWidth - _minPromptWidth);
            rowWidth -= _keyGuideContainer.rect.width;
        }

        float promptWidth = Mathf.Max(rowWidth, _minPromptWidth);
        float promptHeight = Mathf.Max(_defaultPromptHeight, MeasurePromptHeightAtMinSize(promptWidth));
        promptRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, promptWidth);
        promptRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, promptHeight);
        _itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            _defaultItemHeight + (promptHeight - _defaultPromptHeight));

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
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
            _checkPopTween?.Kill();
            _checkPopTween = _checkedImage.transform.DOScale(Vector3.one, _checkPopDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        // 2) 체크박스 프레임 펀치
        if (_checkboxRect != null)
        {
            _checkPunchTween?.Kill();
            _checkPunchTween = _checkboxRect.DOPunchScale(Vector3.one * 0.25f, _checkPopDuration, 2, 0.5f)
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

    /// <summary>프리팹에 지정된 기본 아이템/문구 높이를 최초 1회 캡처합니다. (FitLayout이 늘린 높이를 기본값으로 삼지 않도록)</summary>
    private void CaptureLayoutDefaults()
    {
        if (_layoutDefaultsCaptured)
        {
            return;
        }
        _itemRect = (RectTransform)transform;
        _defaultItemHeight = _itemRect.rect.height;
        _defaultPromptHeight = _promptText.rectTransform.rect.height;
        _layoutDefaultsCaptured = true;
    }

    private static bool IsLayoutIgnored(RectTransform rect)
    {
        LayoutElement element = rect.GetComponent<LayoutElement>();
        return element != null && element.ignoreLayout;
    }

    /// <summary>
    /// 칩 영역이 maxWidth보다 넓으면 넘친 만큼 칩 글자 크기를 줄입니다.
    /// 칩은 Setup마다 템플릿에서 새로 복제되므로 원래 크기로 되돌릴 필요가 없다.
    /// </summary>
    private void FitChipsToWidth(float maxWidth)
    {
        float overflow = _keyGuideContainer.rect.width - Mathf.Max(0f, maxWidth);
        if (overflow <= 0f)
        {
            return;
        }

        TMP_Text[] chipTexts = _keyGuideContainer.GetComponentsInChildren<TMP_Text>(false);
        float textWidth = 0f;
        foreach (TMP_Text chipText in chipTexts)
        {
            textWidth += chipText.preferredWidth;
        }
        if (textWidth <= 0f)
        {
            return;
        }

        // 글자 폭은 글자 크기에 비례하므로, 넘친 폭만큼 줄어들도록 비율을 맞춘다
        float scale = Mathf.Clamp01((textWidth - overflow) / textWidth);
        foreach (TMP_Text chipText in chipTexts)
        {
            chipText.fontSize = Mathf.Max(1f, chipText.fontSize * scale);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_keyGuideContainer);
    }

    /// <summary>문구를 자동 크기 조절의 최소 글자 크기로 그렸을 때 필요한 높이를 계산합니다.</summary>
    private float MeasurePromptHeightAtMinSize(float width)
    {
        bool autoSizing = _promptText.enableAutoSizing;
        float fontSize = _promptText.fontSize;

        _promptText.enableAutoSizing = false;
        if (autoSizing)
        {
            _promptText.fontSize = _promptText.fontSizeMin;
        }
        float height = _promptText.GetPreferredValues(_promptText.text, width, 0f).y;

        _promptText.enableAutoSizing = autoSizing;
        _promptText.fontSize = fontSize;
        return height;
    }

    private void KillSlideTween()
    {
        _slideTween?.Kill();
        _slideTween = null;
    }

    /// <summary>
    /// 칩(KeyGuide) 내용을 절차 생성합니다. 아이콘이 있으면 아이콘을, 없으면 키 라벨 글자를 템플릿 복제로 채운다.
    /// </summary>
    private void BuildKeyChips(string[] keyLabels, Image iconSource)
    {
        if (_keyGuideContainer == null)
        {
            return;
        }

        // 이전 Setup에서 복제한 칩 내용을 모두 제거 (템플릿은 보존)
        // Destroy는 프레임 끝에 실행되므로, 즉시 레이아웃 계산에 포함되지 않도록 먼저 비활성화한다
        foreach (GameObject clone in _chipClones)
        {
            if (clone != null)
            {
                clone.SetActive(false);
                Destroy(clone);
            }
        }
        _chipClones.Clear();
        if (_chipTextTemplate != null)
        {
            _chipTextTemplate.gameObject.SetActive(false);
        }
        if (_chipIconTemplate != null)
        {
            _chipIconTemplate.gameObject.SetActive(false);
        }

        bool useIcon = iconSource != null && iconSource.sprite != null && _chipIconTemplate != null;
        bool useLabels = !useIcon && keyLabels != null && keyLabels.Length > 0 && _chipTextTemplate != null;

        // 키 가이드가 없는 목표면 칩 영역을 통째로 숨겨 텍스트 정렬을 유지한다
        _keyGuideContainer.gameObject.SetActive(useIcon || useLabels);
        if (useIcon)
        {
            CreateIconChip(iconSource);
        }
        else if (useLabels)
        {
            // 키가 둘 이상이면(QE 등) 칩 하나에 "Q 또는 E"처럼 이어서 표시한다.
            CreateTextChip(keyLabels.Length == 1 ? keyLabels[0] : string.Join(" 또는 ", keyLabels));
        }
        else
        {
            return;
        }

        // 칩 내용 확정 후 컨테이너(KeyGuide)의 ContentSizeFitter를 즉시 재계산.
        // 자동 레이아웃 패스 전에 끝나지 않아 크기가 한 프레임 늦게 반영되는 문제를 해결한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(_keyGuideContainer);
    }

    /// <summary>글자 템플릿을 복제해 라벨을 채우고, 텍스트 기준 크기 계산을 즉시 반영합니다.</summary>
    private void CreateTextChip(string label)
    {
        TMP_Text chipText = Instantiate(_chipTextTemplate, _keyGuideContainer);
        chipText.gameObject.SetActive(true);
        chipText.text = label;
        _chipClones.Add(chipText.gameObject);

        // 텍스트 교체 후 ContentSizeFitter 계산을 즉시 반영
        LayoutRebuilder.ForceRebuildLayoutImmediate(chipText.rectTransform);
    }

    /// <summary>
    /// 아이콘 템플릿을 복제해 터치 버튼 아이콘과 같은 모양으로 채웁니다.
    /// 버튼이 같은 스프라이트를 좌우 반전해 쓰는 경우(◀/▶)도 있으므로 반전 여부까지 따라간다.
    /// 색은 템플릿 색(칩 글자색)을 유지해 흰 칩 위에서도 보이게 한다.
    /// </summary>
    private void CreateIconChip(Image iconSource)
    {
        Image chipIcon = Instantiate(_chipIconTemplate, _keyGuideContainer);
        chipIcon.gameObject.SetActive(true);
        chipIcon.sprite = iconSource.sprite;
        _chipClones.Add(chipIcon.gameObject);

        Vector3 sourceScale = iconSource.rectTransform.localScale;
        Vector3 scale = chipIcon.rectTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(sourceScale.x);
        scale.y = Mathf.Abs(scale.y) * Mathf.Sign(sourceScale.y);
        chipIcon.rectTransform.localScale = scale;
    }

    private void OnDestroy()
    {
        KillSlideTween();
        _checkPopTween?.Kill();
        _checkPopTween = null;
        _checkPunchTween?.Kill();
        _checkPunchTween = null;
    }
}
