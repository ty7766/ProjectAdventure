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
    private Coroutine _runeChangeroutine;

    private void Start()
    {
        if (_runeTypes != null && _runeTypes.Length > 0)
        {
            _runeChangeroutine = StartCoroutine(RuneChangeRoutine());
        }
    }

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

            UpdateRuneObjects(currentRune);

            if(VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayVFX(_runeChangeVFX, transform.position +  _runeChangeVFXPosition, Quaternion.identity);
            }

            OnRuneChanged?.Invoke(currentRune);
        }
    }

    //해당 룬으로 바꾸는 메소드
    private void UpdateRuneObjects(RuneType activeRune)
    {
        for (int i = 0; i < _runeObject.Length; i++)
        {
            if (_runeObject[i] != null)
            {
                bool isActive = (i == _currentIndex);
                _runeObject[i].SetActive(isActive);
            }
        }
    }
}
