using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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
    [Tooltip("_기믹에 사용할 Rune 등록")]
    [SerializeField]
    private List<RunePrefabMapping> _prefabDatabase;

    [Header("VFX 설정")]
    [SerializeField]
    private VFXType _runeChangeVFX = VFXType.RuneChange;
    [SerializeField]
    private Vector3 _runeChangeVFXPosition = new Vector3(0, 1.5f, 0);

    private int _currentIndex = 0;
    private GameObject _currentRuneInstance;
    private Coroutine _runeChangeRoutine;

    private Dictionary<RuneType, GameObject> _prefabDictionary;
    private WaitForSeconds _waitInterval;

    private void Awake()
    {
        Assert.IsNotNull(_runeSequence, "[RuneStone] _runeSequence가 설정되지 않았습니다.");
        Assert.IsTrue(_runeSequence.Length > 0, "[RuneStone] _runeSequence가 비어있습니다.");
        Assert.IsNotNull(_prefabDatabase, "[RuneStone] _prefabDatabase가 설정되지 않았습니다.");

        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.enabled = false;
        }

        _waitInterval = new WaitForSeconds(_changeInterval);
        InitializePrefabDictionary();

        foreach (var rune in _runeSequence)
        {
            Assert.IsTrue(_prefabDictionary.ContainsKey(rune),
                $"[RuneStone] '{rune}'에 해당하는 프리팹이 _prefabDatabase에 등록되지 않았습니다.");
        }
    }

    private void OnEnable()
    {
        _currentIndex = -1; // 루프 시작 시 0번부터 시작하도록 -1로 초기화

        if (_runeChangeRoutine != null)
        {
            StopCoroutine(_runeChangeRoutine);
        }
        _runeChangeRoutine = StartCoroutine(RuneChangeLoop());
    }

    private void OnDisable()
    {
        if (_runeChangeRoutine != null)
        {
            StopCoroutine(_runeChangeRoutine);
            _runeChangeRoutine = null;
        }

        DestroyCurrentRune();
    }

    private void InitializePrefabDictionary()
    {
        _prefabDictionary = new Dictionary<RuneType, GameObject>();
        foreach (var mapping in _prefabDatabase)
        {
            _prefabDictionary.TryAdd(mapping.type, mapping.prefab);
        }
    }

    private IEnumerator RuneChangeLoop()
    {
        while (true)
        {
            //인덱스 증가
            _currentIndex = (_currentIndex + 1) % _runeSequence.Length;
            RuneType currentRune = _runeSequence[_currentIndex];

            //룬 생성 및 이벤트 발생
            SpawnRuneObject(currentRune);
            PlayRuneEffect();
            OnRuneChanged?.Invoke(currentRune);

            //대기
            yield return _waitInterval;
        }
    }
    private void SpawnRuneObject(RuneType targetType)
    {
        //룬 삭제
        DestroyCurrentRune();

        //딕셔너리에서 룬 검색
        if (_prefabDictionary.TryGetValue(targetType, out GameObject targetPrefab))
        {
            if (targetPrefab != null)
            {
                _currentRuneInstance = Instantiate(targetPrefab, transform.position + _runeChangeVFXPosition, targetPrefab.transform.rotation, transform);
            }
        }
        else
        {
            Debug.LogWarning($"[RuneStone] '{targetType}'에 해당하는 프리팹을 찾을 수 없습니다.");
        }
    }

    private void DestroyCurrentRune()
    {
        if (_currentRuneInstance != null)
        {
            Destroy(_currentRuneInstance);
            _currentRuneInstance = null;
        }
    }

    private void PlayRuneEffect()
    {
        VFXManager.Instance.PlayVFX(_runeChangeVFX, transform.position + _runeChangeVFXPosition, Quaternion.identity);
        SoundManager.Instance.PlaySFX_3D(SoundType.SFX_Rune, transform.position, 1f, 10f);
    }
}