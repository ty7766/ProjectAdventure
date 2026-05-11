using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CameraFollow))]
public class TutorialCameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField]
    private float _cameraMoveSmooth = 0.3f;
    [SerializeField]
    private float _smoothDampthreshold = 0.05f;

    private Coroutine _moveCoroutine;
    private Camera _camera;
    private CameraFollow _cameraFollow;

    private void Awake()
    {
        _camera = Camera.main;
        _cameraFollow = GetComponent<CameraFollow>();
    }
    /// <summary>
    /// 카메라를 대상 위치로 스무스하게 이동합니다. 도착 시 onArrived가 호출됩니다.
    /// </summary>
    public void MoveToTarget(Transform target, Action onArrived)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        _moveCoroutine = StartCoroutine(MoveCameraRoutine(target, onArrived));
    }

    /// <summary>
    /// CameraFollow 컴포넌트를 다시 활성화하고 이동 코루틴을 중단합니다.
    /// </summary>
    public void RestoreFollow()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
        _cameraFollow.enabled = true;
    }

    private IEnumerator MoveCameraRoutine(Transform target, Action onArrived)
    {
        _cameraFollow.enabled = false;

        Vector3 targetPos = _camera.transform.position;
        targetPos.z = target.position.z + _cameraFollow.PlayerOffset.z;

        Vector3 velocity = Vector3.zero;

        while (Vector3.Distance(_camera.transform.position, targetPos) > _smoothDampthreshold)
        {
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                targetPos,
                ref velocity,
                _cameraMoveSmooth,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
            yield return null;
        }

        _camera.transform.position = targetPos;
        _moveCoroutine = null;
        onArrived?.Invoke();
    }
}
