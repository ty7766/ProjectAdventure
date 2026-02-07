using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneStone : MonoBehaviour
{
    [System.Serializable]
    public struct RunePrefabMapping
    {
        public RuneType type;       // 룬 이름
        public GameObject prefab;   // 연결할 프리팹
    }

    public static event Action<RuneType> OnRuneChanged;

    [Header("설정")]
    [SerializeField]
    private float _changeInterval = 5.0f;

    [Header("순서 설정")]
    [SerializeField]
    private RuneType[] _runeSequence;

    [Header("프리팹 도감")]
    [Tooltip("모든 룬 프리팹을 여기에 등록하세요.")]
    [SerializeField]
    private List<RunePrefabMapping> _prefabDatabase;

    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _runeChangeVFX = VFXType.RuneChange;
    [SerializeField]
    private Vector3 _runeChangeVFXPosition = new Vector3(0, 1.5f, 0);

    private int _currentIndex = 0;
    private GameObject _currentRuneInstance;
    private Coroutine _runeChangeroutine;

    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void OnEnable()
    {
        _currentIndex = 0;

        if (_runeChangeroutine != null)
        {
            StopCoroutine(_runeChangeroutine);
        }
        _runeChangeroutine = StartCoroutine(RuneChangeRoutine());
    }

    private void OnDisable()
    {
        if (_runeChangeroutine != null)
        {
            StopCoroutine(_runeChangeroutine);
            _runeChangeroutine = null;
        }

        if (_currentRuneInstance != null)
        {
            Destroy(_currentRuneInstance);
        }
    }

    private void OnDestroy()
    {
        OnRuneChanged = null;
    }

    private IEnumerator RuneChangeRoutine()
    {
        yield return null;

        //시작하자마자 바로 한 번 실행
        if (_runeSequence != null && _runeSequence.Length > 0)
        {
            RuneType firstRune = _runeSequence[0];
            SpawnRuneObject(firstRune);
            PlayRuneVFX();

            OnRuneChanged?.Invoke(firstRune);
        }

        WaitForSeconds wait = new WaitForSeconds(_changeInterval);

        while (true)
        {
            yield return wait;

            _currentIndex = (_currentIndex + 1) % _runeSequence.Length;
            RuneType currentRune = _runeSequence[_currentIndex];

            SpawnRuneObject(currentRune);
            PlayRuneVFX();
            OnRuneChanged?.Invoke(currentRune);
        }
    }
    private void SpawnRuneObject(RuneType targetType)
    {
        //룬 삭제
        if (_currentRuneInstance != null)
        {
            Destroy(_currentRuneInstance);
            _currentRuneInstance = null;
        }

        //프리팹 찾기
        GameObject targetPrefab = null;

        foreach (var mapping in _prefabDatabase)
        {
            if (mapping.type == targetType)
            {
                targetPrefab = mapping.prefab;
                break;
            }
        }

        //찾았으면 생성
        if (targetPrefab != null)
        {
            _currentRuneInstance = Instantiate(targetPrefab, transform.position + _runeChangeVFXPosition, targetPrefab.transform.rotation);
            _currentRuneInstance.transform.SetParent(this.transform);
        }
    }

    private void PlayRuneVFX()
    {
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayVFX(_runeChangeVFX, transform.position + _runeChangeVFXPosition, Quaternion.Euler(-90, 0, 0));
        }
    }
}