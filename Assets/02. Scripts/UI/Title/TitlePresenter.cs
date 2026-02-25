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
    }

    public void OnExitButtonClicked()
    {
        // Handle exit button click logic here
        ExitGame();
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


}
