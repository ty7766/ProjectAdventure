public class UI_AccountTabPresenter
{
    private readonly UI_AccountTabView _view;
    private bool _linking;
    private bool _working;
    private bool _started;

    public UI_AccountTabPresenter(UI_AccountTabView view)
    {
        _view = view;
    }

    public void Start()
    {
        if (_started)
            return;
        _started = true;
        LandLinkerServices.StateChanged += Refresh;
        Refresh();
    }

    public void Cleanup()
    {
        if (!_started)
            return;
        _started = false;
        LandLinkerServices.StateChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_view == null)
            return;
        _view.SetState(new AccountTabState
        {
            Online = LandLinkerServices.IsEnabled,
            Linking = _linking,
            Working = _working,
            Linked = LandLinkerServices.GoogleLinked,
            Name = LandLinkerServices.GoogleDisplayName,
            Id = LandLinkerServices.ShortAccountNumber,
            GoogleAvailable = LandLinkerServices.GoogleAvailable,
            AdPrivacy = LandLinkerServices.PrivacyOptionsRequired
        });
    }

    public void Google()
    {
        if (_linking || _working)
            return;
        if (!LandLinkerServices.IsEnabled)
        {
            LandLinkerServices.RequestConsentPrompt();
            return;
        }

        _linking = true;
        Refresh();
        LandLinkerServices.SignInGoogle(error =>
        {
            _linking = false;
            if (_view == null)
                return;
            Refresh();
            if (!string.IsNullOrEmpty(error) && error != "google_cancelled")
                _view.LinkFailed();
        });
    }

    public void Copy()
    {
        if (string.IsNullOrEmpty(LandLinkerServices.PlayerId))
            return;
        LandLinkerServices.CopyAccountNumberToClipboard();
        _view.CopyMessage();
    }

    public void Delete()
    {
        if (!_linking && !_working)
            _view.ConfirmDelete(ExecuteDelete);
    }

    private void ExecuteDelete()
    {
        _working = true;
        Refresh();
        LandLinkerServices.DeleteAccount((ok, error) =>
        {
            _working = false;
            if (_view == null)
                return;
            Refresh();
            _view.DeleteResult(ok);
        });
    }

    public void Support()
    {
        if (!_linking && !_working && GlobalUICanvasView.HasInstance && GlobalUICanvasView.Instance.SupportModal != null)
            GlobalUICanvasView.Instance.SupportModal.Show();
    }

    public void AdPrivacy()
    {
        if (_linking || _working)
            return;
        _working = true;
        Refresh();
        LandLinkerServices.ShowPrivacyOptions(() =>
        {
            _working = false;
            Refresh();
        });
    }
}
