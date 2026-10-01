using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 일시정지 메뉴 등장 연출. 바로 조작해야 하는 화면이므로 짧고 가볍게, 등장 중에도 입력을 받는다.
/// 배경 딤 -> 타이틀(위에서 내려옴) -> 버튼(위에서부터 차례로 떠오름) + 도전과제 패널(오른쪽에서 밀려옴)
/// </summary>
public class PauseMenuSequence : UISequenceScreen
{
    //--- Settings ---//
    [Header("Targets")]
    [SerializeField] private RectTransform _title;
    [SerializeField, Tooltip("시각적 위계(위 -> 아래) 순서")] private List<RectTransform> _buttons = new List<RectTransform>();
    [SerializeField] private RectTransform _sidePanel;

    [Header("Timing")]
    [SerializeField, Range(0.05f, 0.5f)] private float _dimDuration = 0.15f;
    [SerializeField, Range(0f, 0.2f)] private float _buttonInterval = 0.04f;

    //--- Fields ---//
    private Element _titleElement;
    private readonly List<Element> _buttonElements = new List<Element>();
    private Element _sidePanelElement;

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
        _sidePanelElement = Register(_sidePanel);
    }

    protected override void BuildEnter(Sequence sequence)
    {
        _rootGroup.alpha = 0f;
        sequence.Insert(0f, _rootGroup.DOFade(1f, _dimDuration).SetEase(Ease.OutQuad));

        InsertSlideIn(sequence, _titleElement, 0.03f, new Vector2(0f, 40f), 0.3f, Ease.OutCubic);
        for (int i = 0; i < _buttonElements.Count; i++)
        {
            InsertSlideIn(sequence, _buttonElements[i], 0.06f + i * _buttonInterval, new Vector2(0f, -24f), 0.25f, Ease.OutCubic);
        }
        InsertSlideIn(sequence, _sidePanelElement, 0.08f, new Vector2(80f, 0f), 0.35f, Ease.OutCubic);
    }
}
