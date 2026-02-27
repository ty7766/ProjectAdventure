using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class RuneGate : MonoBehaviour
{
    [Header("이 문의 룬 설정")]
    [SerializeField]
    private RuneType _myRuneType;

    [Header("문 오브젝트 연결")]
    [SerializeField]
    private Transform _leftDoor;
    [SerializeField]
    private Transform _rightDoor;

    [Header("문 설정")]
    [SerializeField]
    private float _openAngle = -90f; // 열리는 각도
    [SerializeField]
    private float _duration = 1.0f; // 열리는 데 걸리는 시간

    private Quaternion _leftClosedRot;
    private Quaternion _rightClosedRot;
    private Quaternion _leftOpenRot;
    private Quaternion _rightOpenRot;

    private Coroutine _doorRoutine;
    private bool _isOpen = false;

    private void Awake()
    {
        Assert.IsNotNull(_leftDoor, $"[RuneGate] {_myRuneType} 문의 Left Door가 없습니다.");
        Assert.IsNotNull(_rightDoor, $"[RuneGate] {_myRuneType} 문의 Right Door가 없습니다.");
        InitialCalculateDoorRotation();
    }

    private void OnEnable()
    {
        RuneStone.OnRuneChanged += HandleRuneChange;
    }

    private void OnDisable()
    {
        RuneStone.OnRuneChanged -= HandleRuneChange;
    }

    private void InitialCalculateDoorRotation()
    {
        // 초기 닫힌 각도 저장
        _leftClosedRot = _leftDoor.localRotation;
        _rightClosedRot = _rightDoor.localRotation;

        _leftOpenRot = _leftClosedRot * Quaternion.Euler(0, _openAngle, 0);
        _rightOpenRot = _rightClosedRot * Quaternion.Euler(0, -_openAngle, 0);
    }

    // 중앙 돌에서 신호가 오면 실행되는 함수
    private void HandleRuneChange(RuneType currentRune)
    {
        if (currentRune == _myRuneType)
        {
            if (!_isOpen) OpenDoor();
        }
        else
        {
            if (_isOpen) CloseDoor();
        }
    }

    private void OpenDoor()
    {
        _isOpen = true;
        if (_doorRoutine != null) StopCoroutine(_doorRoutine);
        _doorRoutine = StartCoroutine(MoveDoorRoutine(_leftOpenRot, _rightOpenRot));
        SoundManager.Instance.PlaySFX_3D(SoundType.SFX_RuneDoor, transform.position, 20f, 60f);
    }

    private void CloseDoor()
    {
        _isOpen = false;
        if (_doorRoutine != null) StopCoroutine(_doorRoutine);
        _doorRoutine = StartCoroutine(MoveDoorRoutine(_leftClosedRot, _rightClosedRot));
    }

    private IEnumerator MoveDoorRoutine(Quaternion targetLeft, Quaternion targetRight)
    {
        float timer = 0f;
        Quaternion startLeft = _leftDoor.localRotation;
        Quaternion startRight = _rightDoor.localRotation;

        while (timer < _duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / _duration);

            // 부드러운 움직임을 위해서 추가
            float smoothStep = Mathf.SmoothStep(0f, 1f, t);

            _leftDoor.localRotation = Quaternion.Slerp(startLeft, targetLeft, smoothStep);
            _rightDoor.localRotation = Quaternion.Slerp(startRight, targetRight, smoothStep);

            yield return null;
        }

        // 확실하게 목표 각도로 고정
        _leftDoor.localRotation = targetLeft;
        _rightDoor.localRotation = targetRight;
    }
}