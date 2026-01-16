using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HUDView : MonoBehaviour
{
    //--- Settings ---//
    [Header("UI Components")]
    [SerializeField] private List<Image> _heartImages; // 하트 아이콘 리스트
    [SerializeField] private Sprite _fullHeart; // 꽉 찬 하트 이미지
    [SerializeField] private Sprite _emptyHeart; // 빈 하트 이미지
    [SerializeField] private List<Image> _gemImages; // 보석 아이콘 리스트
    [SerializeField] private Color _fullGemColor; // 수집한 보석 색상
    [SerializeField] private Color _emptyGemColor; // 빈 보석 이미지 색상


    //--- Public Methods ---//
    /// <summary>
    /// UI의 체력 표시를 업데이트 합니다.
    /// </summary>
    /// <param name="currentHealth"></param>
    public void UpdateHealthUI(int currentHealth)
    {
        for (int i = 0; i < _heartImages.Count; i++)
        {
            UpdateHealthIcons(currentHealth, i);
        }
    }

    /// <summary>
    /// 보석 수집 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="collectedGems">수집한 보석 개수</param>
    /// <param name="requiredGems">스테이지 클리어에 필요한 보석 개수</param>
    public void UpdateGemUI(int collectedGems, int requiredGems)
    {
        for(int i=0; i< requiredGems; i++)
        {
            UpdateGemIcons(collectedGems, i);
        }
        for (int i = requiredGems; i< _gemImages.Count; i++)
        {
            _gemImages[i].gameObject.SetActive(false);
        }
    }

    //--- Private Methods ---//
    private void UpdateHealthIcons(int currentHealth, int i)
    {
        if (i < currentHealth)
        {
            _heartImages[i].sprite = _fullHeart;
        }
        else
        {
            _heartImages[i].sprite = _emptyHeart;
        }
    }

    private void UpdateGemIcons(int collectedGems, int i)
    {
        _gemImages[i].gameObject.SetActive(true);
        if (i < collectedGems)
        {
            _gemImages[i].color = _fullGemColor;
        }
        else
        {
            _gemImages[i].color = _emptyGemColor;
        }
    }
}
