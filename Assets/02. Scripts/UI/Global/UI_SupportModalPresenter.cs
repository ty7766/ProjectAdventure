using UnityEngine;

public class UI_SupportModalPresenter
{
    private readonly UI_SupportModalView _view;
    private bool _sending;
    private string _threadUrl;
    private int _generation;

    public UI_SupportModalPresenter(UI_SupportModalView view)
    {
        _view = view;
    }

    public void Start()
    {
    }

    public void Cleanup()
    {
        _generation++;
    }

    public void Reset()
    {
        _generation++;
        _sending = false;
        _threadUrl = null;
        _view.ShowForm();
    }

    public void Submit()
    {
        if (_sending)
            return;
        var error = SupportTicketValidation.Validate(_view.Body, _view.Email);
        if (error != SupportError.None)
        {
            _view.ShowError(error);
            return;
        }

        _sending = true;
        _view.ShowForm();
        _view.SetSending(true);
        int generation = _generation;
        LandLinkerServices.SubmitSupportTicket(_view.Category, _view.Body, _view.Email, result =>
        {
            if (_view == null || generation != _generation)
                return;
            _sending = false;
            _view.SetSending(false);
            if (!result.Ok)
            {
                _view.ShowError(result.Error);
                return;
            }

            _threadUrl = result.ThreadUrl;
            _view.ShowDone(result.Reference, !string.IsNullOrEmpty(_threadUrl));
        });
    }

    public void OpenThread()
    {
        if (!string.IsNullOrEmpty(_threadUrl))
            Application.OpenURL(_threadUrl);
    }
}
