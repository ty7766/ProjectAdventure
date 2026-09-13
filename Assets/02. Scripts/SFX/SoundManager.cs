using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Audio3DSourcePool))]
public class SoundManager : Singleton<SoundManager>
{
    //구조체가 아닌 클래스인 이유: 필드 초기값(Volume = 1f)이 역직렬화 때 적용되어야
    //기존에 등록된 사운드들이 볼륨 0(무음)으로 살아나지 않는다.
    [System.Serializable]
    public class SoundData
    {
        public SoundType SoundType;
        public AudioClip AudioClip;

        [Range(0f, 1f)]
        [Tooltip("이 사운드 고유의 볼륨. 옵션의 전역 BGM/SFX 볼륨에 곱해집니다.")]
        public float Volume = 1f;
    }

    [Header("오디오 플레이어 등록")]
    [SerializeField]
    private AudioSource _bgmSource;
    [SerializeField]
    private AudioSource _sfxSource;

    [Header("오디오 데이터 등록")]
    [SerializeField]
    private List<SoundData> _soundDataList;

    private Dictionary<SoundType, SoundData> _soundDictionary = new Dictionary<SoundType, SoundData>();
    private Audio3DSourcePool _audio3DPool;

    //옵션 창에서 조절하는 전역 볼륨(0~1). 사운드별 Volume과 곱해져 최종 볼륨이 된다.
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    //현재 재생 중인 BGM 고유 볼륨. 전역 볼륨만 바뀌어도 다시 곱해줘야 하므로 보관한다.
    private float _currentBGMVolume = 1f;

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this)
        {
            return;
        }

        _audio3DPool = GetComponent<Audio3DSourcePool>();
        InitializeAudioDictionary();
        LoadSettings();
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

        if (_soundDictionary.TryGetValue(soundType, out SoundData soundData))
        {
            if(_bgmSource.clip == soundData.AudioClip && _bgmSource.isPlaying)
            {
                return;
            }

            _currentBGMVolume = soundData.Volume;
            _bgmSource.clip = soundData.AudioClip;
            ApplyBGMVolume();
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

        if(_soundDictionary.TryGetValue(soundType, out SoundData soundData))
        {
            //PlayOneShot의 두 번째 인자는 _sfxSource.volume(전역 SFX 볼륨)에 곱해진다
            _sfxSource.PlayOneShot(soundData.AudioClip, soundData.Volume);
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

        if (!_soundDictionary.TryGetValue(soundType, out SoundData soundData))
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
        source.clip = soundData.AudioClip;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        //풀에서 꺼낸 AudioSource는 PlayOneShot이 아니므로 전역 볼륨을 직접 곱해준다
        source.volume = _sfxVolume * soundData.Volume;
        source.Play();
    }

    /// <summary>
    /// BGM 전역 볼륨을 설정하는 메서드 (0~1)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", _bgmVolume);

        if (_bgmSource != null)
        {
            ApplyBGMVolume();
        }
        else
        {
            CustomDebug.LogWarning("[SoundManager] BGM AudioSource가 할당되지 않았습니다. 볼륨이 저장되었지만 적용되지 않았습니다.");
        }
    }

    /// <summary>
    /// SFX 전역 볼륨을 설정하는 메서드 (0~1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);

        if (_sfxSource != null)
        {
            //PlayOneShot이 이 값에 사운드별 Volume을 곱해 재생한다
            _sfxSource.volume = _sfxVolume;
        }
        else
        {
            CustomDebug.LogWarning("[SoundManager] SFX AudioSource가 할당되지 않았습니다. 볼륨이 저장되었지만 적용되지 않았습니다.");
        }
    }

    /// <summary>
    /// BGM 전역 볼륨을 가져오는 메서드
    /// </summary>
    public float GetBGMVolume()
    {
        return _bgmVolume;
    }

    /// <summary>
    /// SFX 전역 볼륨을 가져오는 메서드
    /// </summary>
    public float GetSFXVolume()
    {
        return _sfxVolume;
    }

    /// <summary>
    /// 저장된 사운드 설정을 불러와 게임 시작 시 적용합니다.
    /// </summary>
    private void LoadSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    //구현부

    //전역 BGM 볼륨과 현재 곡의 고유 볼륨을 곱해 실제 AudioSource에 반영
    private void ApplyBGMVolume()
    {
        _bgmSource.volume = _bgmVolume * _currentBGMVolume;
    }

    private void InitializeAudioDictionary()
    {
        //먼저 클리어
        _soundDictionary.Clear();

        foreach (var soundData in _soundDataList)
        {
            //클래스로 바뀌면서 인스펙터의 빈 항목이 null로 들어올 수 있다
            if (soundData == null || soundData.AudioClip == null)
            {
                continue;
            }

            if (!_soundDictionary.TryAdd(soundData.SoundType, soundData))
            {
                CustomDebug.LogWarning($"[SoundManager] '{soundData.SoundType}'이 중복 등록되어 뒤쪽 항목을 무시합니다.");
            }
        }
    }
}
