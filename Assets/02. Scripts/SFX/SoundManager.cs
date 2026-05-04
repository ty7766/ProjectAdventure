using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Audio3DSourcePool))]
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
    private Audio3DSourcePool _audio3DPool;

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this)
        {
            return;
        }

        InitializeAudioDictionary();
        _audio3DPool = GetComponent<Audio3DSourcePool>();
    }

    /// <summary>
    /// BGM을 재생하는 메소드
    /// </summary>
    /// <param name="soundType">재생할 사운드 enum 타입</param>
    public void PlayBGM(SoundType soundType)
    {
        if (soundType == SoundType.None)
        {
            _bgmSource.Stop();
            return;
        }

        if (_soundDictionary.TryGetValue(soundType, out AudioClip audioClip))
        {
            if(_bgmSource.clip == audioClip && _bgmSource.isPlaying)
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
        if (soundType == SoundType.None)
        {
            return;
        }

        if(_soundDictionary.TryGetValue(soundType, out AudioClip audioClip))
        {
            _sfxSource.PlayOneShot(audioClip);
        }
        else
        {
            CustomDebug.LogWarning("해당 사운드 타입이 없습니다.");
        }
    }

    /// <summary>
    /// 특정 3D 좌표에서 사운드를 내는 함수
    /// </summary>
    /// <param name="soundType">재생할 사운드 enum 타입</param>
    /// <param name="position">사운드를 실행할 위치</param>
    public void PlaySFX_3D(SoundType soundType, Vector3 position, float minDistance = 15f, float maxDistance = 50f)
    {
        if (soundType == SoundType.None)
        {
            return;
        }

        if (!_soundDictionary.TryGetValue(soundType, out AudioClip audioClip))
        {
            CustomDebug.LogWarning("해당 사운드 타입이 없습니다.");
            return;
        }

        AudioSource source = _audio3DPool.Acquire();
        if (source == null)
        {
            return;
        }

        source.transform.position = position;
        source.clip = audioClip;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.Play();
    }

    /// <summary>
    /// BGM 볼륨을 설정하는 메서드 (0~1)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", clampedVolume);

        if (_bgmSource != null)
        {
            _bgmSource.volume = clampedVolume;
        }
        else
        {
            CustomDebug.LogWarning("[SoundManager] BGM AudioSource가 할당되지 않았습니다. 볼륨이 저장되었지만 적용되지 않았습니다.");
        }
    }

    /// <summary>
    /// SFX 볼륨을 설정하는 메서드 (0~1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", clampedVolume);

        if (_sfxSource != null)
        {
            _sfxSource.volume = clampedVolume;
        }
        else
        {
            CustomDebug.LogWarning("[SoundManager] SFX AudioSource가 할당되지 않았습니다. 볼륨이 저장되었지만 적용되지 않았습니다.");
        }
    }

    /// <summary>
    /// BGM 볼륨을 가져오는 메서드
    /// </summary>
    public float GetBGMVolume()
    {
        return _bgmSource != null ? _bgmSource.volume : 0f;
    }

    /// <summary>
    /// SFX 볼륨을 가져오는 메서드
    /// </summary>
    public float GetSFXVolume()
    {
        return _sfxSource != null ? _sfxSource.volume : 0f;
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
