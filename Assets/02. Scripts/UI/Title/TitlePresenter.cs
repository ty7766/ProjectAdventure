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
        GlobalUICanvasView.Instance.Presenter.ShowPopup(
            "경고",
            "정말 게임을 종료하시겠습니까?",
            ("확인", () =>
            {
                GlobalUICanvasView.Instance.Presenter.HidePopup();
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        ),
            ("취소", () => GlobalUICanvasView.Instance.Presenter.HidePopup())
        );
    }


}
