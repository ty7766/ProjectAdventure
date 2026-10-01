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
        if (_model == null)
        {
            CustomDebug.LogWarning("[UI_SoundTabPresenter] SoundManager is not initialized.");
            return;
        }

        SyncViewWithModel();
        BindEvents();
    }

    private void SyncViewWithModel()
    {
        if (_model == null) return;

        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        _view.BGMVolume = bgmVolume;
        _view.SFXVolume = sfxVolume;

        _model.SetBGMVolume(bgmVolume);
        _model.SetSFXVolume(sfxVolume);

        if (_view.IsHapticsSectionVisible)
        {
            _view.HapticsIndex = Haptics.Enabled ? 1 : 0;
        }
    }

    private void BindEvents()
    {
        if (_model == null) return;

        _view.BindSoundSettings(
            val => { if (_model != null) _model.SetBGMVolume(val); },
            val => { if (_model != null) _model.SetSFXVolume(val); }
        );

        _view.BindSFXVolumeDragEnd(PlayClickSound);

        if (_view.IsHapticsSectionVisible)
        {
            _view.BindHapticsSetting(OnHapticsChanged);
        }
    }

    private void OnHapticsChanged(int index)
    {
        Haptics.Enabled = index == 1;
        // 켰을 때 바로 손끝으로 확인할 수 있도록 미리 한 번 울려준다
        Haptics.Play(HapticType.Tap);
    }

    private void PlayClickSound()
    {
        if (_model != null)
        {
            _model.PlaySFX(SoundType.SFX_ButtonClick);
        }
    }
}
