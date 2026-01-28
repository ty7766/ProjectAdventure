using UnityEngine;
using UnityEngine.Events;
public class TitleView : MonoBehaviour
{
    //--- Settings ---//
    [Header("Title Buttons")]
    [SerializeField] private UnityEngine.UI.Button _playButton;
    [SerializeField] private UnityEngine.UI.Button _optionsButton;
    [SerializeField] private UnityEngine.UI.Button _exitButton;

    //--- Fields ---//
    private TitlePresenter _titlePresenter;
    private UnityAction _onPlayButtonClicked;
    private UnityAction _onOptionsButtonClicked;
    private UnityAction _onExitButtonClicked;


    //--- Unity Methods ---//
    private void Awake()
    {
        if(IsCheckFailed())
        {
            this.enabled = false;
            return;
        }

        InitializeView();
    }

    private void OnDestroy()
    {
        DisposeButtonHandlers();
    }

    //--- Private Methods ---// 
    bool IsCheckFailed()
    {
        if(_playButton == null)
        {
            CustomDebug.LogError("Play Button is not assigned in the inspector.");
            return true;
        }
        if(_optionsButton == null)
        {
            CustomDebug.LogError("Options Button is not assigned in the inspector.");
            return true;
        }
        if(_exitButton == null)
        {
            CustomDebug.LogError("Exit Button is not assigned in the inspector.");
            return true;
        }
        return false;
    }

    void InitializeView()
    {
        _titlePresenter = new TitlePresenter(this);
        _onExitButtonClicked = _titlePresenter.OnExitButtonClicked;
        _exitButton.onClick.AddListener(_onExitButtonClicked);
    }

    private void DisposeButtonHandlers()
    {
        if (_playButton != null && _onPlayButtonClicked != null)
        {
            _playButton.onClick.RemoveListener(_onPlayButtonClicked);
        }
        if (_optionsButton != null && _onOptionsButtonClicked != null)
        {
            _optionsButton.onClick.RemoveListener(_onOptionsButtonClicked);
        }
        if (_exitButton != null && _onExitButtonClicked != null)
        {
            _exitButton.onClick.RemoveListener(_onExitButtonClicked);
        }
    }
}
