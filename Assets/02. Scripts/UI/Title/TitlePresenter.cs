using UnityEngine;

public class TitlePresenter
{
    //--- Fields ---//
    private TitleView _titleView;
    private TitleFlowController _flowController;

    public TitlePresenter(TitleView titleView, TitleFlowController flowController)
    {
        _titleView = titleView;
        _flowController = flowController;
    }

    public void OnPlayButtonClicked()
    {
        if(!_flowController)
        {
            CustomDebug.LogWarning("[TitlePresenter.cs] flowController was null!!");
            return;
        }
        _flowController.GoToStageSelect();
    }

    public void OnOptionsButtonClicked()
    {
        // Handle options button click logic here
        if(!_flowController)
        {
            CustomDebug.LogWarning("[TitlePresenter.cs] flowController was null!!");
            return;
        }
        _flowController.GoToOptions();

    }

    public void OnExitButtonClicked()
    {
        // Handle exit button click logic here
        ExitGame();
    }

    void ExitGame()
    {
        if (GlobalUICanvasView.Instance != null && GlobalUICanvasView.Instance.PopupPresenter != null)
        {
            GlobalUICanvasView.Instance.PopupPresenter.ShowPopup(
                "경고",
                "정말 게임을 종료하시겠습니까?",
                ("확인", () =>
                {
                    GlobalUICanvasView.Instance.PopupPresenter.HidePopup();
                    PerformQuit();
                }
            ),
                ("취소", () => GlobalUICanvasView.Instance.PopupPresenter.HidePopup())
            );
        }
        else
        {
            PerformQuit();
        }
    }

    private void PerformQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


}
