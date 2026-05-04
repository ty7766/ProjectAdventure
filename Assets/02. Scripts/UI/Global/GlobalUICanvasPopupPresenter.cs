using System;

public class GlobalUICanvasPopupPresenter
{
    private GlobalUICanvasView _view;

    public GlobalUICanvasPopupPresenter(GlobalUICanvasView view)
    {
        _view = view;
    }

    /// <summary>
    /// 팝업을 표시합니다.
    /// </summary>
    /// <param name="title">팝업 제목</param>
    /// <param name="message">팝업 메시지</param>
    /// <param name="buttons">버튼 목록 (버튼 텍스트, 클릭 콜백)</param>
    public void ShowPopup(string title, string message, params (string buttonText, Action onClickCallback)[] buttons)
    {
        _view.SetPopupContent(title, message);
        _view.ClearButtons();

        foreach (var button in buttons)
        {
            _view.CreateButton(button.buttonText, button.onClickCallback);
        }

        _view.ShowPopup();
    }

    /// <summary>
    /// 팝업을 숨깁니다.
    /// </summary>
    public void HidePopup()
    {
        _view.HidePopup();
    }
}
