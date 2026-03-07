using System;
using System.Collections;
using UnityEngine;

public class TutorialCameraController : MonoBehaviour
{
    //--- Serialized Fields ---//
    [Header("카메라 설정")]
    [SerializeField]
    private CameraFollow _cameraFollow;
    [SerializeField]
    private float _cameraMoveSmooth = 0.3f;

    //--- Fields ---//
    private Coroutine _moveCoroutine;

    //--- Public Methods ---//
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

    //--- Private Methods ---//
    private IEnumerator MoveCameraRoutine(Transform target, Action onArrived)
    {
        _cameraFollow.enabled = false;

        Camera cam = Camera.main;
        Vector3 targetPos = target.position + _cameraFollow.PlayerOffset;
        Vector3 velocity = Vector3.zero;

        while (Vector3.Distance(cam.transform.position, targetPos) > 0.05f)
        {
            cam.transform.position = Vector3.SmoothDamp(
                cam.transform.position,
                targetPos,
                ref velocity,
                _cameraMoveSmooth,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
            yield return null;
        }

        cam.transform.position = targetPos;
        _moveCoroutine = null;
        onArrived?.Invoke();
    }
}
