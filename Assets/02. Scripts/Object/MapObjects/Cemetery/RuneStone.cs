using System;
using System.Collections;
using UnityEngine;

public class RuneStone : MonoBehaviour
{
    public static event Action<RuneType> OnRuneChanged;

    [Header("룬 주기")]
    [SerializeField]
    private float _changeInterval = 5.0f;

    [Header("사용할 룬 타입")]
    [SerializeField]
    private RuneType[] _runeTypes;

    [Header("사용할 룬 오브젝트")]
    [Tooltip("RuneType 순서와 일치해야함")]
    [SerializeField]
    private GameObject[] _runeObject;

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
        if(meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }
    private void OnEnable()
    {
        _currentIndex = 0;

        // 시작하자마자 첫 번째 룬 생성
        if (_runeTypes != null && _runeTypes.Length > 0)
        {
            SpawnRuneObject(_runeTypes[0]);
        }

        if (_runeChangeroutine != null) StopCoroutine(_runeChangeroutine);
        _runeChangeroutine = StartCoroutine(RuneChangeRoutine());
    }

    private void OnDisable()
    {
        if (_runeChangeroutine != null)
        {
            StopCoroutine(_runeChangeroutine);
            _runeChangeroutine = null;
        }

        // 맵 꺼질 때 룬 삭제
        if (_currentRuneInstance != null)
        {
            Destroy(_currentRuneInstance);
        }
    }
    //중복 방지 (이벤트 구독 해제)
    private void OnDestroy()
    {
        OnRuneChanged = null;
    }

    private IEnumerator RuneChangeRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_changeInterval);

        while(true)
        {
            yield return wait;
            
            //룬 변경
            _currentIndex = (_currentIndex + 1) % _runeTypes.Length;
            RuneType currentRune = _runeTypes[_currentIndex];

            SpawnRuneObject(currentRune);

            if(VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayVFX(_runeChangeVFX, transform.position +  _runeChangeVFXPosition, Quaternion.identity);
            }

            OnRuneChanged?.Invoke(currentRune);
        }
    }

    private void SpawnRuneObject(RuneType type)
    {
        //기존에 떠있는 룬이 있으면 제거
        if (_currentRuneInstance != null)
        {
            Destroy(_currentRuneInstance);
            _currentRuneInstance = null;
        }

        //배열이 비었으면 넘기기
        if (_runeObject == null)
        {
            return;
        }

        //룬의 고유 이름을 정수로 변환하여 인덱스로 결정

        int objectIndex = (int)type - 1;

        if (objectIndex < 0 || objectIndex >= _runeObject.Length)
        {
            return;
        }

        GameObject prefabToSpawn = _runeObject[objectIndex];

        if (prefabToSpawn != null)
        {
            _currentRuneInstance = Instantiate(prefabToSpawn, transform.position + _runeChangeVFXPosition, Quaternion.identity);
            _currentRuneInstance.transform.SetParent(this.transform);
        }
    }
}
