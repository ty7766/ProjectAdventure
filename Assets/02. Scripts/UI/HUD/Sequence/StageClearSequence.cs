using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 화면 시퀀셜 연출. 게임은 이미 timeScale 0으로 멈춘 상태. 클릭/터치/키 입력으로 건너뛸 수 있다.
/// 1) 딤 -> 2) 보더(패널 프레임) -> 3) 헤더(스테이지 번호) -> 4) 빈 별 3개
/// -> 5) 달성한 개수만큼 채워진 별이 크게 떠 있다가 가속하며 내리꽂혀 박힘 (별마다 임팩트 + 패널 흔들림)
/// -> 6) STAGE CLEAR 문구, 기록 -> 7) 버튼이 시각적 위계 순서대로 (다음 스테이지 -> 다시하기)
/// </summary>
public class StageClearSequence : UISequenceScreen
{
    //--- Settings ---//
    [Header("Frame")]
    [SerializeField, Tooltip("패널 보더/배경. 자식(헤더/별/기록)을 품고 있어도 된다")] private RectTransform _border;
    [SerializeField] private RectTransform _header;
    [SerializeField, Tooltip("흔들림 연출 대상 (패널 컨테이너)")] private RectTransform _shakeTarget;

    [Header("Stars")]
    [SerializeField, Tooltip("빈 별 슬롯 (왼쪽 -> 오른쪽)")] private List<RectTransform> _starSlots = new List<RectTransform>();
    [SerializeField, Tooltip("각 슬롯 위에 겹쳐지는 채워진 별 이미지 (슬롯과 같은 순서)")] private List<Image> _starFills = new List<Image>();

    [Header("Result")]
    [SerializeField] private RectTransform _clearText;
    [SerializeField] private RectTransform _record;
    [SerializeField, Tooltip("시각적 위계 순서 (주 버튼 먼저)")] private List<RectTransform> _buttons = new List<RectTransform>();

    [Header("Star Impact")]
    [SerializeField, Range(1.5f, 4f)] private float _starDropScale = 2.2f;
    [SerializeField, Range(0.1f, 0.6f)] private float _starDropDuration = 0.26f;
    [SerializeField, Range(0.1f, 1f)] private float _starInterval = 0.38f;
    [SerializeField, Tooltip("별 박힐 때 패널 흔들림 세기 (별 순서대로 점점 세짐)")] private float _shakeStrength = 7f;

    //--- Fields ---//
    private Element _borderElement;
    private Element _headerElement;
    private readonly List<Element> _starSlotElements = new List<Element>();
    private Element _clearTextElement;
    private Element _recordElement;
    private readonly List<Element> _buttonElements = new List<Element>();
    private Vector2 _shakeHome;
    private readonly HashSet<int> _earnedStars = new HashSet<int>();

    //--- Public Methods ---//
    /// <summary>등장 연출에서 해당 슬롯에 별이 박힐지 지정한다.</summary>
    public void SetStarEarned(int index, bool isEarned)
    {
        if (isEarned)
        {
            _earnedStars.Add(index);
        }
        else
        {
            _earnedStars.Remove(index);
        }
    }

    //--- Protected Methods ---//
    protected override void OnInitialize()
    {
        _borderElement = Register(_border);
        _headerElement = Register(_header);
        foreach (var slot in _starSlots)
        {
            _starSlotElements.Add(Register(slot));
        }
        _clearTextElement = Register(_clearText);
        _recordElement = Register(_record);
        foreach (var button in _buttons)
        {
            if (button != null)
            {
                _buttonElements.Add(Register(button));
            }
        }
        if (_shakeTarget != null)
        {
            _shakeHome = _shakeTarget.anchoredPosition;
        }
    }

    protected override void BuildEnter(Sequence sequence)
    {
        if (_shakeTarget != null)
        {
            _shakeTarget.anchoredPosition = _shakeHome;
        }

        // 1) 딤
        _rootGroup.alpha = 0f;
        sequence.Insert(0f, _rootGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));

        // 2) 보더: 살짝 작게 시작해 약하게 튕기며 자리 잡음 (Back Out)
        InsertPopIn(sequence, _borderElement, 0.1f, 0.85f, 0.45f, Ease.OutBack);

        // 3) 헤더: 위에서 내려옴
        InsertSlideIn(sequence, _headerElement, 0.35f, new Vector2(0f, 30f), 0.35f, Ease.OutCubic);

        // 4) 빈 별: 가볍게 팝
        float at = 0.5f;
        for (int i = 0; i < _starSlotElements.Count; i++)
        {
            ResetStarFill(i);
            InsertPopIn(sequence, _starSlotElements[i], at + i * 0.06f, 0.5f, 0.3f, Ease.OutBack);
        }

        // 5) 달성한 별이 박힘
        at += 0.45f;
        int impactCount = 0;
        for (int i = 0; i < _starSlots.Count; i++)
        {
            if (_earnedStars.Contains(i))
            {
                InsertStarImpact(sequence, i, at, impactCount++);
                at += _starInterval;
            }
        }
        if (impactCount > 0)
        {
            at += 0.1f;
        }

        // 6) 결과 문구와 기록
        InsertPopIn(sequence, _clearTextElement, at, 0.7f, 0.4f, Ease.OutBack);
        InsertSlideIn(sequence, _recordElement, at + 0.12f, new Vector2(-40f, 0f), 0.35f, Ease.OutCubic);

        // 7) 버튼: 위계 순서대로 떠오름
        at += 0.4f;
        for (int i = 0; i < _buttonElements.Count; i++)
        {
            InsertSlideIn(sequence, _buttonElements[i], at + i * 0.12f, new Vector2(0f, -30f), 0.35f, Ease.OutCubic);
        }
    }

    //--- Private Methods ---//
    private void ResetStarFill(int index)
    {
        Image fill = GetStarFill(index);
        if (fill == null)
        {
            return;
        }
        fill.rectTransform.localScale = Vector3.one;
        fill.rectTransform.localRotation = Quaternion.identity;
        fill.color = WithAlpha(fill.color, 0f);
    }

    /// <summary>
    /// 채워진 별이 크게/기울어진 채 나타나 가속(Cubic In)하며 슬롯에 내리꽂힌 뒤, 슬롯이 충격으로 출렁이고 패널이 흔들린다.
    /// </summary>
    private void InsertStarImpact(Sequence sequence, int index, float at, int impactOrder)
    {
        Image fill = GetStarFill(index);
        if (fill == null || index >= _starSlots.Count || _starSlots[index] == null)
        {
            return;
        }

        RectTransform fillRect = fill.rectTransform;
        float tilt = index % 2 == 0 ? -25f : 25f;
        fillRect.localScale = Vector3.one * _starDropScale;
        fillRect.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // 떨어지는 별이 옆 슬롯(뒤 형제)에 가려지지 않도록 맨 앞으로
        RectTransform slot = _starSlots[index];
        sequence.InsertCallback(at, () => slot.SetAsLastSibling());
        sequence.Insert(at, fillRect.DOScale(1f, _starDropDuration).SetEase(Ease.InCubic));
        sequence.Insert(at, fillRect.DOLocalRotate(Vector3.zero, _starDropDuration).SetEase(Ease.OutQuad));
        sequence.Insert(at, fill.DOFade(1f, _starDropDuration * 0.5f).SetEase(Ease.OutQuad));

        float impact = at + _starDropDuration;
        InsertSound(sequence, impact, SoundType.SFX_StarPop);
        sequence.Insert(impact, slot.DOPunchScale(Vector3.one * 0.22f, 0.35f, 7, 0.6f));
        if (_shakeTarget != null)
        {
            float strength = _shakeStrength * (1f + impactOrder * 0.4f);
            sequence.Insert(impact, _shakeTarget.DOShakeAnchorPos(0.25f, strength, 24, 90f, false, true));
        }
    }

    private Image GetStarFill(int index)
    {
        return index >= 0 && index < _starFills.Count ? _starFills[index] : null;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
