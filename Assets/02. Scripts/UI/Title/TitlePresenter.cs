using UnityEngine;

public class TitlePresenter
{
    //--- Fields ---//
    private TitleView _titleView;

    public TitlePresenter(TitleView titleView)
    {
        _titleView = titleView;
    }

    public void OnPlayButtonClicked()
    {
        // Handle play button click logic here
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
