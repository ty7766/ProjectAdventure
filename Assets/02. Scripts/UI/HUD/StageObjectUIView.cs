using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageObjectUIView : MonoBehaviour
{
    [Header("Stage Object UI")]
    [SerializeField]
    private GameObject _stageObjectPanel;
    [SerializeField]
    private List<TextMeshProUGUI> _stageObjectTexts;
    [SerializeField]
    private List<Image> _stageObjectStarImages;

    /// <summary>
    /// 스테이지 도전과제 UI를 업데이트 합니다.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="text"></param>
    public void UpdateStageObjectUI(int index, string text, bool isCleared, Sprite starFilledSprite, Sprite starEmptySprite)
    {
        if (index < 0 || index >= _stageObjectTexts.Count || index >= _stageObjectStarImages.Count)
        {
            CustomDebug.LogWarning("UpdateStageObjectUI: Index out of range");
            return;
        }

        _stageObjectTexts[index].text = text;
        _stageObjectStarImages[index].sprite = isCleared ? starFilledSprite : starEmptySprite;
    }
}
