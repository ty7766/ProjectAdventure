using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 3D 사운드 재생용 AudioSource를 미리 생성해두고 재사용하는 풀
/// </summary>
public class Audio3DSourcePool : MonoBehaviour
{
    [Header("풀 설정")]
    [SerializeField, Tooltip("게임 시작 시 미리 생성할 AudioSource 개수")]
    private int _initialPoolSize = 8;
    [SerializeField, Tooltip("동시 재생 가능한 최대 개수")]
    private int _maxPoolSize = 32;

    private readonly List<AudioSource> _pool = new List<AudioSource>();
    private Transform _poolRoot;

    private void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// 사용 가능한 AudioSource 반환
    /// </summary>
    /// <returns></returns>
    public AudioSource Acquire()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
            {
                return _pool[i];
            }
        }

        if (_pool.Count < _maxPoolSize)
        {
            return CreateNewAudioSource();
        }

        return null;
    }

    private void InitializePool()
    {
        GameObject root = new GameObject("3D Audio Pool");
        root.transform.SetParent(transform);
        _poolRoot = root.transform;

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject host = new GameObject($"3DAudioSource_{_pool.Count}");
        host.transform.SetParent(_poolRoot);

        AudioSource source = host.AddComponent<AudioSource>();
        source.spatialBlend = 1.0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.playOnAwake = false;

        _pool.Add(source);
        return source;
    }
                
}
