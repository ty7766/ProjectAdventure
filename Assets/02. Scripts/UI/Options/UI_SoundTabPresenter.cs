using UnityEngine;

public class UI_SoundTabPresenter
{
    private UI_SoundTabView _view;
    private SoundManager _model;

    public UI_SoundTabPresenter(UI_SoundTabView view)
    {
        _view = view;
        _model = SoundManager.Instance;
    }

    public void Initialize()
    {
        SyncViewWithModel();
        BindEvents();
    }

    private void SyncViewWithModel()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        _view.BGMVolume = bgmVolume;
        _view.SFXVolume = sfxVolume;

        _model.SetBGMVolume(bgmVolume);
        _model.SetSFXVolume(sfxVolume);
    }

    private void BindEvents()
    {
        _view.BindSoundSettings(
            val => _model.SetBGMVolume(val),
            val => _model.SetSFXVolume(val)
        );

        _view.BindSFXVolumeDragEnd(PlayClickSound);
    }

    private void PlayClickSound()
    {
        _model.PlaySFX(SoundType.SFX_ButtonClick);
    }
}
