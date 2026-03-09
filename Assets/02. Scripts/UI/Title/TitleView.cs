using UnityEngine;
using UnityEngine.Events;
public class TitleView : MonoBehaviour
{
    //--- Settings ---//
    [Header("Title Buttons")]
    [SerializeField] private UnityEngine.UI.Button _playButton;
    [SerializeField] private UnityEngine.UI.Button _optionsButton;
    [SerializeField] private UnityEngine.UI.Button _exitButton;

    //--- Dependency Injection ---//
    [SerializeField] private TitleFlowController _flowController;

    //--- Fields ---//
    private TitlePresenter _titlePresenter;
    private UnityAction _onPlayButtonClicked;
    private UnityAction _onOptionsButtonClicked;
    private UnityAction _onExitButtonClicked;


    //--- Unity Methods ---//
    private void Awake()
    {
        if (IsCheckFailed())
        {
            this.enabled = false;
            return;
        }

        InitializeView();
    }

    private void Start()
    {
        SoundManager.Instance.PlayBGM(SoundType.BGM_TitleSceneMusic);
    }

    private void OnDestroy()
    {
        DisposeButtonHandlers();
    }

    //--- Private Methods ---// 
    private bool IsCheckFailed()
    {
        if (_playButton == null)
        {
            CustomDebug.LogError("Play Button is not assigned in the inspector.");
            return true;
        }
        if (_optionsButton == null)
        {
            CustomDebug.LogError("Options Button is not assigned in the inspector.");
            return true;
        }
        if (_exitButton == null)
        {
            CustomDebug.LogError("Exit Button is not assigned in the inspector.");
            return true;
        }
        if (_flowController == null)
        {
            CustomDebug.LogError("TitleFlowController is not assigned in the inspector.");
            return true;
        }
        return false;
    }

    private void InitializeView()
    {
        _titlePresenter = new TitlePresenter(this, _flowController);
        _exitButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnExitButtonClicked(); });
        _playButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnPlayButtonClicked(); });
        _optionsButton.onClick.AddListener(() => { PlayClickSound(); _titlePresenter.OnOptionsButtonClicked(); });
        GraphicManager.Instance.SetDoFMode("Title");
    }

    private void DisposeButtonHandlers()
    {
        if (_playButton)
        {
            _playButton.onClick.RemoveAllListeners();
        }

        if (_optionsButton)
        {
            _optionsButton.onClick.RemoveAllListeners();
        }

        if (_exitButton)
        {
            _exitButton.onClick.RemoveAllListeners();
        }
    }
    private void PlayClickSound()
    {
        SoundManager.Instance.PlaySFX(SoundType.SFX_ButtonClick);
    }
}
