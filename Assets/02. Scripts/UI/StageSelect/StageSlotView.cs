using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Button))]
public class StageSlotView : MonoBehaviour
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

    [SerializeField]
    private TextMeshProUGUI _stageNumberText;

    private Button _stageButton;
    public Button StageButton => _stageButton;

    public void Awake()
    {
        _stageButton = GetComponent<Button>();
    }

    public void UpdateStarFilled(int stars)
    {
        if(_starEmpty == null || _starFilled == null)
        {
            return;
        }

        stars = Mathf.Clamp(stars, 0, _starImages.Count);
        for(int i = 0; i < stars; i++)
        {
            _starImages[i].sprite = _starFilled;
        }

        for(int i = stars;  i < _starImages.Count; i++)
        {
            _starImages[i].sprite = _starEmpty;
        }
    }

    public void UpdateLockedBlocker(bool isUnlocked)
    {
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
}
