using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 인게임 HUD 등장 연출. 카운트다운의 "GO!" 직후 재생되므로 짧게, 등장 중에도 조작 가능.
/// 각 요소가 자신이 붙어 있는 화면 가장자리 바깥쪽에서 안으로 들어온다.
/// </summary>
public class HUDGroupSequence : UISequenceScreen
{
    //--- Settings ---//
    [Header("Targets (화면 위쪽 가장자리)")]
    [SerializeField, Tooltip("왼쪽 -> 오른쪽 순서")] private List<RectTransform> _topItems = new List<RectTransform>();
    [Header("Targets (화면 아래쪽 가장자리)")]
    [SerializeField] private List<RectTransform> _bottomItems = new List<RectTransform>();
    [Header("Targets (페이드만)")]
    [SerializeField] private List<RectTransform> _fadeItems = new List<RectTransform>();

    [Header("Motion")]
    [SerializeField] private float _slideDistance = 120f;
    [SerializeField, Range(0.1f, 1f)] private float _slideDuration = 0.45f;
    [SerializeField, Range(0f, 0.2f)] private float _itemInterval = 0.06f;

    //--- Fields ---//
    private readonly List<Element> _topElements = new List<Element>();
    private readonly List<Element> _bottomElements = new List<Element>();
    private readonly List<Element> _fadeElements = new List<Element>();

    //--- Protected Methods ---//
    protected override void OnInitialize()
    {
        RegisterAll(_topItems, _topElements);
        RegisterAll(_bottomItems, _bottomElements);
        RegisterAll(_fadeItems, _fadeElements);
    }

    protected override void BuildEnter(Sequence sequence)
    {
        _rootGroup.alpha = 0f;
        sequence.Insert(0f, _rootGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));

        float at = 0f;
        foreach (var element in _topElements)
        {
            InsertSlideIn(sequence, element, at, new Vector2(0f, _slideDistance), _slideDuration, Ease.OutCubic);
            at += _itemInterval;
        }
        foreach (var element in _bottomElements)
        {
            InsertSlideIn(sequence, element, at, new Vector2(0f, -_slideDistance), _slideDuration, Ease.OutCubic);
            at += _itemInterval;
        }
        foreach (var element in _fadeElements)
        {
            InsertFadeIn(sequence, element, at, _slideDuration);
        }
    }

    //--- Private Methods ---//
    private static void RegisterAll(List<RectTransform> targets, List<Element> elements)
    {
        foreach (var target in targets)
        {
            if (target != null)
            {
                elements.Add(Register(target));
            }
        }
    }
}
