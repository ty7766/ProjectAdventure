using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.UI.ProceduralImage;
using TMPro;

public class NotificationView : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private ProceduralImage _popupPanel;
    [SerializeField]
    private TextMeshProUGUI _messageText;
    [SerializeField]
    private Image _popupIcon;
    [SerializeField]
    private RectTransform _popupRect;

    public IEnumerator MovePopup(Vector2 start, Vector2 end, float animDuration)
    {
        if (!_popupRect)
        {
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < animDuration)
        {
            if (!_popupRect)
            {
                yield break;
            }
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / animDuration);
            float curveT = t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            _popupRect.anchoredPosition = Vector2.Lerp(start, end, curveT);
            yield return null;
        }

        if (!_popupRect)
        {
            yield break;
        }
        _popupRect.anchoredPosition = end;
    }

    public void ShowPopupAt(Vector2 position, PopupContext context)
    {
        if (context == null || !_messageText || !_popupPanel || !_popupIcon || !_popupRect)
        {
            return;
        }

        _messageText.text = context.Content;
        _popupPanel.color = context.BackgroundColor;
        _popupIcon.sprite = context.Icon;
        _popupRect.anchoredPosition = position;
    }

    public void HidePopup()
    {
        if (_popupRect)
        {
            _popupRect.anchoredPosition = new Vector2(999999, 99999);
        }
    }
}
