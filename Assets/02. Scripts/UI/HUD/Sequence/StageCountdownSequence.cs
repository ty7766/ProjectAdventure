using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 시작 카운트다운 연출 (준비하세요 -> 3 -> 2 -> 1 -> GO!).
/// 타이밍(사운드 동기화)은 HUDFlowPresenter가 쥐고, 이 뷰는 각 박자의 비주얼만 담당한다.
/// - 등장: 딤 -> 도전과제 패널이 아래에서 올라옴
/// - 준비: 넓게 벌어진 자간이 좁혀지며 서서히 떠오름 (Cubic Out)
/// - 숫자: 직전 글자는 고스트로 커지며 흩어지고, 새 숫자는 작게 시작해 튕기며 자리 잡은 뒤(Back Out) 숨 쉬듯 살짝 줄어듦(Sine InOut)
///         + 링이 퍼져나가는 펄스
/// - GO: 강조색으로 더 크게 튕겨 등장 + 큰 링 / 퇴장: GO가 화면 쪽으로 커지며 사라지고 딤과 패널이 빠짐
/// </summary>
public class StageCountdownSequence : UISequenceScreen
{
    //--- Settings ---//
    [Header("Targets")]
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField, Tooltip("이전 글자가 흩어지는 연출용 복제 라벨")] private TextMeshProUGUI _ghostLabel;
    [SerializeField, Tooltip("박자마다 퍼지는 링")] private Graphic _ring;
    [SerializeField] private RectTransform _missionPanel;

    [Header("Typography")]
    [SerializeField] private float _readyFontSize = 80f;
    [SerializeField] private float _numberFontSize = 150f;
    [SerializeField] private float _goFontSize = 170f;
    [SerializeField, Tooltip("준비 문구가 등장할 때 시작 자간")] private float _readySpacingFrom = 60f;
    [SerializeField] private float _readySpacingTo = 6f;
    [SerializeField] private Color _goColor = new Color(1f, 0.84f, 0.29f, 1f);

    [Header("Beat")]
    [SerializeField, Range(0.1f, 0.6f)] private float _popDuration = 0.35f;
    [SerializeField, Range(0.5f, 1f), Tooltip("숫자가 자리 잡은 뒤 다음 박자까지 줄어드는 비율")] private float _breathScale = 0.92f;
    [SerializeField, Range(0.3f, 1.2f)] private float _breathDuration = 0.6f;
    [SerializeField] private float _ringMaxScale = 1.6f;
    [SerializeField] private float _goRingMaxScale = 2.4f;

    //--- Fields ---//
    private Element _missionElement;
    private Color _labelBaseColor = Color.white;
    private Color _ringBaseColor = Color.white;
    private Sequence _beatSequence;

    //--- Unity Methods ---//
    protected override void OnDestroy()
    {
        _beatSequence?.Kill();
        base.OnDestroy();
    }

    //--- Public Methods ---//
    /// <summary>준비 문구: 넓은 자간에서 좁혀지며 떠오른다.</summary>
    public void PlayReady(string text)
    {
        if (!BeginBeat())
        {
            return;
        }

        _label.text = text;
        _label.fontSize = _readyFontSize;
        _label.color = WithAlpha(_labelBaseColor, 0f);
        _label.characterSpacing = _readySpacingFrom;
        _label.rectTransform.localScale = Vector3.one * 1.05f;

        _beatSequence.Insert(0f, _label.DOFade(1f, 0.5f).SetEase(Ease.OutQuad));
        _beatSequence.Insert(0f, DOTween.To(() => _label.characterSpacing, value => _label.characterSpacing = value, _readySpacingTo, 0.9f).SetEase(Ease.OutCubic));
        _beatSequence.Insert(0f, _label.rectTransform.DOScale(1f, 0.9f).SetEase(Ease.OutCubic));
    }

    /// <summary>카운트 숫자 한 박자</summary>
    public void PlayTick(string text)
    {
        PlayBeat(text, _numberFontSize, _labelBaseColor, 0.5f, 1.8f, _ringMaxScale, 0.6f);
    }

    /// <summary>GO! 박자: 강조색으로 더 크게</summary>
    public void PlayGo(string text)
    {
        PlayBeat(text, _goFontSize, _goColor, 0.3f, 2.4f, _goRingMaxScale, 0.85f);
    }

    //--- Protected Methods ---//
    protected override void OnInitialize()
    {
        _missionElement = Register(_missionPanel);
        if (_label != null)
        {
            _labelBaseColor = _label.color;
        }
        if (_ring != null)
        {
            _ringBaseColor = _ring.color;
        }
    }

    protected override void BuildEnter(Sequence sequence)
    {
        _beatSequence?.Kill();
        ResetBeatVisuals();
        if (_label != null)
        {
            _label.color = WithAlpha(_labelBaseColor, 0f);
        }

        _rootGroup.alpha = 0f;
        sequence.Insert(0f, _rootGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        InsertSlideIn(sequence, _missionElement, 0.1f, new Vector2(0f, -80f), 0.5f, Ease.OutCubic);
    }

    /// <summary>GO가 화면 쪽으로 커지며 사라지고, 딤과 도전과제 패널이 빠진다.</summary>
    protected override void BuildExit(Sequence sequence)
    {
        _beatSequence?.Kill();

        if (_label != null)
        {
            RectTransform labelRect = _label.rectTransform;
            sequence.Insert(0f, labelRect.DOScale(labelRect.localScale * 1.5f, 0.4f).SetEase(Ease.OutCubic));
            sequence.Insert(0f, _label.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
        }
        if (_missionElement != null)
        {
            sequence.Insert(0f, _missionElement.Rect.DOAnchorPos(_missionElement.HomePosition + new Vector2(0f, -60f), 0.3f).SetEase(Ease.InQuad));
            sequence.Insert(0f, _missionElement.Group.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
        }
        sequence.Insert(0.05f, _rootGroup.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
    }

    //--- Private Methods ---//
    private void PlayBeat(string text, float fontSize, Color color, float fromScale, float overshoot, float ringScale, float ringAlpha)
    {
        if (!BeginBeat())
        {
            return;
        }

        SpawnGhost();

        RectTransform labelRect = _label.rectTransform;
        _label.text = text;
        _label.fontSize = fontSize;
        _label.characterSpacing = 0f;
        _label.color = WithAlpha(color, 0f);
        labelRect.localScale = Vector3.one * fromScale;

        _beatSequence.Insert(0f, _label.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));
        _beatSequence.Insert(0f, labelRect.DOScale(1f, _popDuration).SetEase(Ease.OutBack, overshoot));
        _beatSequence.Insert(_popDuration, labelRect.DOScale(_breathScale, _breathDuration).SetEase(Ease.InOutSine));

        if (_ring != null)
        {
            RectTransform ringRect = _ring.rectTransform;
            ringRect.localScale = Vector3.one * 0.6f;
            _ring.color = WithAlpha(color == _labelBaseColor ? _ringBaseColor : color, ringAlpha);
            _beatSequence.Insert(0f, ringRect.DOScale(ringScale, 0.7f).SetEase(Ease.OutCubic));
            _beatSequence.Insert(0f, _ring.DOFade(0f, 0.7f).SetEase(Ease.OutQuad));
        }
    }

    /// <summary>
    /// 지금 보이는 글자를 고스트 라벨로 옮겨 커지며 흩어지게 한다. (새 글자와 겹쳐 자연스럽게 교차)
    /// </summary>
    private void SpawnGhost()
    {
        if (_ghostLabel == null || _label.color.a <= 0.01f || string.IsNullOrEmpty(_label.text))
        {
            return;
        }

        RectTransform ghostRect = _ghostLabel.rectTransform;
        _ghostLabel.text = _label.text;
        _ghostLabel.fontSize = _label.fontSize;
        _ghostLabel.characterSpacing = _label.characterSpacing;
        _ghostLabel.color = _label.color;
        ghostRect.localScale = _label.rectTransform.localScale;

        // 새 글자가 튀어나오는 동안 빠르게 걷혀야 겹쳐 보이지 않는다
        _beatSequence.Insert(0f, ghostRect.DOScale(ghostRect.localScale * 1.3f, 0.25f).SetEase(Ease.OutCubic));
        _beatSequence.Insert(0f, _ghostLabel.DOFade(0f, 0.2f).SetEase(Ease.OutCubic));
    }

    /// <summary>진행 중인 박자를 끝내고 새 박자 시퀀스를 연다.</summary>
    private bool BeginBeat()
    {
        if (_label == null)
        {
            return false;
        }

        EnsureInitialized();
        _beatSequence?.Kill();
        ResetBeatVisuals();
        _beatSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        return true;
    }

    private void ResetBeatVisuals()
    {
        if (_ghostLabel != null)
        {
            _ghostLabel.color = WithAlpha(_ghostLabel.color, 0f);
        }
        if (_ring != null)
        {
            _ring.color = WithAlpha(_ring.color, 0f);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
