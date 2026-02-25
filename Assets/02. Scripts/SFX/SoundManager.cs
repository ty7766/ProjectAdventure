using UnityEngine;
using System.Collections.Generic;

public class SoundManager : Singleton<SoundManager>
{
    [System.Serializable]
    public struct SoundData
    {
        public SoundType SoundType;
        public AudioClip AudioClip;
    }

    [Header("오디오 플레이어 등록")]
    [SerializeField]
    private AudioSource _bgmSource;
    [SerializeField]
    private AudioSource _sfxSource;

    [Header("오디오 데이터 등록")]
    [SerializeField]
    private List<SoundData> _soundDataList;

    private Dictionary<SoundType, AudioClip> _soundDictionary = new Dictionary<SoundType, AudioClip>();

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this)
        {
            return;
        }

        InitializeAudioDictionary();
    }

    /// <summary>
    /// BGM을 재생하는 메소드
    /// </summary>
    /// <param name="soundType">재생할 사운드 enum 타입</param>
    public void PlayBGM(SoundType soundType)
    {
        if(_soundDictionary.TryGetValue(soundType, out AudioClip audioClip))
        {
            if(_bgmSource.clip == audioClip && _bgmSource.isPlaying == true)
            {
                return;
            }

            _bgmSource.clip = audioClip;
            _bgmSource.Play();
        }
        else
        {
            CustomDebug.LogWarning("해당 사운드 타입이 없습니다.");
        }

    }

    /// <summary>
    /// SFX를 재생하는 메소드
    /// </summary>
    /// <param name="soundType">재생할 사운드 enum 타입</param>
    public void PlaySFX(SoundType soundType)
    {
        if(_soundDictionary.TryGetValue(soundType, out AudioClip audioClip))
        {
            _sfxSource.PlayOneShot(audioClip);
        }
        else
        {
            CustomDebug.LogWarning("해당 사운드 타입이 없습니다.");
        }
    }

    //구현부
    private void InitializeAudioDictionary()
    {
        //먼저 클리어
        _soundDictionary.Clear();

        foreach (var soundData in _soundDataList)
        {
            if (_soundDictionary.ContainsKey(soundData.SoundType))
            {
                continue;
            }

            _soundDictionary.Add(soundData.SoundType, soundData.AudioClip);
        }
    }
}
