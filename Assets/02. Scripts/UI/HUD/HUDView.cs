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

    //--- Public Methods ---//
    /// <summary>
    /// UI의 체력 표시를 업데이트 합니다.
    /// </summary>
    /// <param name="currentHealth"></param>
    public void UpdateHealthUI(int currentHealth)
    {
        for (int i = 0; i < _heartImages.Count; i++)
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
    }
}
