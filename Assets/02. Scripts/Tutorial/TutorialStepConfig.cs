using UnityEngine;

[System.Serializable]
public class TutorialStepConfig
{
    public enum TargetType { WorldObject, UIElement}

    [Header("스텝 정보")]
    [SerializeField]
    private string _stepName;

    [Header("하이라이트 대상")]
    [SerializeField]
    private TargetType _targetType;
    [SerializeField]
    private Transform _worldTarget;
    [SerializeField]
    private RectTransform _uiTarget;

    [Header("스포트라이트")]
    [SerializeField]
    private Vector2 _holeSize = new Vector2(200f, 200f);

    [Header("화살표 & 텍스트")]
    [SerializeField]
    private RectTransform _arrowTipTarget;
    [SerializeField]
    [TextArea]
    private string _descriptionText;

    public string StepName => _stepName;
    public TargetType Target => _targetType;
    public Transform WorldTarget => _worldTarget;
    public RectTransform UiTarget => _uiTarget;
    public Vector2 HoleSize => _holeSize;
    public RectTransform ArrowTipTarget => _arrowTipTarget;
    public string DescriptionText => _descriptionText;
}
