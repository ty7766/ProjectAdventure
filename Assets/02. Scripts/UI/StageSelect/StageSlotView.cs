using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Button))]
public class StageSlotView : MonoBehaviour, ISelectHandler
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField]
    List<Image> _starImages;

    [SerializeField]
    private Sprite _starFilled;

    [SerializeField]
    private Sprite _starEmpty;

    [SerializeField]
    private GameObject _lockedBlocker;

    [SerializeField, Tooltip("비워두면 Blocker의 첫 번째 자식을 사용")]
    private RectTransform _lockIcon;

    [SerializeField]
    private TextMeshProUGUI _stageNumberText;

    [Header("반응 연출")]
    [SerializeField]
    private float _unlockedHoverScale = 1.07f;

    [SerializeField]
    private float _lockedHoverScale = 1.02f;

    [SerializeField, Tooltip("hover 시 별이 하나씩 튀는 간격(초)")]
    private float _starBounceInterval = 0.05f;

    private Button _stageButton;
    private UIButtonFeedback _feedback;
    private bool _isUnlocked = true;
    private int _filledStarCount;
    private Tween _lockTween;
    private Sequence _starSequence;

    public Button StageButton => _stageButton;
    public bool IsUnlocked => _isUnlocked;

    public void Awake()
    {
        _stageButton = GetComponent<Button>();

        _feedback = GetComponent<UIButtonFeedback>();
        if (_feedback == null)
        {
            _feedback = gameObject.AddComponent<UIButtonFeedback>();
        }

        if (_lockIcon == null && _lockedBlocker != null && _lockedBlocker.transform.childCount > 0)
        {
            _lockIcon = _lockedBlocker.transform.GetChild(0) as RectTransform;
        }
    }

    private void OnDisable()
    {
        _lockTween?.Kill(true);
        _starSequence?.Kill(true);
    }

    public void UpdateStarFilled(int stars)
    {
        if(_starEmpty == null || _starFilled == null)
        {
            return;
        }

        _filledStarCount = Mathf.Clamp(stars, 0, _starImages.Count);
        for(int i = 0; i < _filledStarCount; i++)
        {
            _starImages[i].sprite = _starFilled;
        }

        for(int i = _filledStarCount;  i < _starImages.Count; i++)
        {
            _starImages[i].sprite = _starEmpty;
        }
    }

    public void UpdateLockedBlokcer(bool isUnlocked)
    {
        _isUnlocked = isUnlocked;
        _feedback.SetHoverScale(isUnlocked ? _unlockedHoverScale : _lockedHoverScale);

        if(_lockedBlocker == null)
        {
            return;
        }

        _lockedBlocker.SetActive(!isUnlocked);
    }

    public void UpdateStageNumber(int stageNumber)
    {
        if (_stageNumberText == null)
        {
            return;
        }

        _stageNumberText.text = $"Stage {stageNumber}";
    }

    /// <summary>
    /// 잠긴 슬롯 클릭 시 거부 연출: 슬롯 흔들림 + 자물쇠 덜컹.
    /// </summary>
    public void PlayLockedFeedback()
    {
        _feedback.PlayDenied();
        RattleLock(22f, 0.45f, 12);
        UIFeedbackUtility.PlaySound(SoundType.SFX_LockRattle);
    }

    //--- Event Handlers ---//
    public void OnSelect(BaseEventData eventData)
    {
        if (UIFeedbackUtility.IsTouch(eventData))
        {
            return;
        }

        if (_isUnlocked)
        {
            BounceStars();
        }
        else
        {
            RattleLock(8f, 0.3f, 8);
        }
    }

    //--- Private Methods ---//
    private void BounceStars()
    {
        if (_filledStarCount == 0)
        {
            return;
        }

        _starSequence?.Kill(true);
        _starSequence = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < _filledStarCount; i++)
        {
            Transform star = _starImages[i].transform;
            _starSequence.Insert(i * _starBounceInterval, star.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.6f));
        }
    }

    private void RattleLock(float angle, float duration, int vibrato)
    {
        if (_lockIcon == null || _isUnlocked)
        {
            return;
        }

        _lockTween?.Kill(true);
        _lockTween = _lockIcon.DOPunchRotation(new Vector3(0f, 0f, angle), duration, vibrato, 0.8f).SetUpdate(true);
    }
}
