using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 사망(스테이지 실패) 화면 등장 연출. 게임은 이미 timeScale 0으로 멈춘 상태.
/// 붉은 딤이 천천히 깔림 -> "GAME OVER"가 크게 떠 있다가 무겁게 내려앉음 -> 버튼이 위계 순서대로 떠오름
/// </summary>
public class StageFailSequence : UISequenceScreen
{
    //--- Settings ---//
    [Header("Targets")]
    [SerializeField] private RectTransform _title;
    [SerializeField, Tooltip("시각적 위계 순서 (다시하기 -> 타이틀로)")] private List<RectTransform> _buttons = new List<RectTransform>();

    [Header("Timing")]
    [SerializeField, Range(0.1f, 1f)] private float _dimDuration = 0.45f;
    [SerializeField, Range(0f, 0.3f)] private float _buttonInterval = 0.1f;

    //--- Fields ---//
    private Element _titleElement;
    private readonly List<Element> _buttonElements = new List<Element>();

    //--- Protected Methods ---//
    protected override void OnInitialize()
    {
        _titleElement = Register(_title);
        foreach (var button in _buttons)
        {
            if (button != null)
            {
                _buttonElements.Add(Register(button));
            }
        }
    }

    protected override void BuildEnter(Sequence sequence)
    {
        _rootGroup.alpha = 0f;
        sequence.Insert(0f, _rootGroup.DOFade(1f, _dimDuration).SetEase(Ease.OutSine));

        if (_titleElement != null)
        {
            // 크게 떠 있던 글자가 감속하며 제자리에 내려앉는 느낌 (Cubic Out) + 살짝 아래로 가라앉음 (Sine InOut)
            InsertPopIn(sequence, _titleElement, 0.2f, 1.35f, 0.6f, Ease.OutCubic);
            _titleElement.Rect.anchoredPosition = _titleElement.HomePosition + new Vector2(0f, 24f);
            sequence.Insert(0.2f, _titleElement.Rect.DOAnchorPos(_titleElement.HomePosition, 0.6f).SetEase(Ease.InOutSine));
        }

        float buttonStart = 0.65f;
        for (int i = 0; i < _buttonElements.Count; i++)
        {
            InsertSlideIn(sequence, _buttonElements[i], buttonStart + i * _buttonInterval, new Vector2(0f, -36f), 0.35f, Ease.OutCubic);
        }
    }
}
