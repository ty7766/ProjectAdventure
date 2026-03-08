using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class CameraPoint
{
    public string pointName;
    public Vector3 position;
    public Vector3 rotation;
    public float focusDistance;
}

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class TitleCameraController : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField]
    private List<CameraPoint> points = new List<CameraPoint>();

    [Header("Focus Settings (Sync)")]
    [SerializeField, Tooltip("여기에 DoF가 있는 Global Volume을 넣어줘!")]
    private Volume postProcessVolume;

    [SerializeField, Min(0.1f), Tooltip("여기서 초점을 조절하면 물리 카메라와 볼륨이 같이 움직여!")]
    private float currentFocusDistance = 10f;

    [Header("Transition Settings")]
    [SerializeField, Range(0.1f, 5f)]
    private float transitionDuration = 1.5f;

    private Camera _mainCamera;
    private DepthOfField _dof;
    private Coroutine _transitionCoroutine;

    private void OnEnable()
    {
        _mainCamera = GetComponent<Camera>();
        _mainCamera.usePhysicalProperties = true;
    }

    private void Update()
    {
        SyncFocusDistance();
    }

    private void OnValidate()
    {
        SyncFocusDistance();
    }

    public void DoTransition(string pointName)
    {
        CameraPoint targetPoint = points.Find(p => p.pointName == pointName);

        if (targetPoint == null)
        {
            Debug.LogWarning($"<color=red>어라?</color> '{pointName}'(이)라는 이름의 포인트가 없는데 형?");
            return;
        }

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(targetPoint));
    }

    public void SetCameraInstantly(string pointName)
    {
        CameraPoint targetPoint = points.Find(p => p.pointName == pointName);
        if (targetPoint == null)
        {
            Debug.LogWarning($"<color=red>어라?</color> '{pointName}'(이)라는 이름의 포인트가 없는데 형?");
            return;
        }
        transform.position = targetPoint.position;
        transform.rotation = Quaternion.Euler(targetPoint.rotation);
        currentFocusDistance = targetPoint.focusDistance;
    }

    private IEnumerator TransitionRoutine(CameraPoint target)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(target.rotation);
        float startFocus = currentFocusDistance;

        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;

            // Quadratic Ease In-Out
            float easeT = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            transform.position = Vector3.Lerp(startPos, target.position, easeT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, easeT);
            currentFocusDistance = Mathf.Lerp(startFocus, target.focusDistance, easeT);

            yield return null;
        }

        transform.position = target.position;
        transform.rotation = targetRot;
        currentFocusDistance = target.focusDistance;
    }

    private void SyncFocusDistance()
    {
        if (_mainCamera == null) _mainCamera = GetComponent<Camera>();
        _mainCamera.focusDistance = currentFocusDistance;
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (postProcessVolume.profile.TryGet(out _dof))
            {
                _dof.focusDistance.value = currentFocusDistance;
            }
        }
    }

    [ContextMenu("현재 카메라 트랜스폼 & 초점 추가")]
    private void AddCurrentTransformToPoints()
    {
        CameraPoint newPoint = new CameraPoint();
        newPoint.pointName = "Point " + points.Count;
        newPoint.position = transform.position;
        newPoint.rotation = transform.eulerAngles;
        newPoint.focusDistance = currentFocusDistance;

        points.Add(newPoint);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log($"<color=yellow>{newPoint.pointName}</color>이(가) 리스트에 추가됐어! 위치, 회전, 초점 거리 폼 미쳤다 ✨");
    }
}