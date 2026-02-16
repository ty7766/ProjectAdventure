using UnityEngine;

public class GemAnimation : MonoBehaviour
{
    [Header("회전 속도/각도 설정")]
    [SerializeField]
    private Vector3 _rotationAxis = Vector3.up;
    [SerializeField]
    private float _rotationSpeed = 50.0f;

    [Tooltip("떠다니는 높이 범위")]
    [SerializeField]
    private float _floatAmplitude = 0.25f;

    [Tooltip("떠다니는 속도")]
    [SerializeField]
    private float _floatFrequency = 1.0f;

    private Vector3 _initialLocalPosition;

    private void Start()
    {
        _initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        ApplyRotationForGem();
        ApplyFloatingForGem();
    }

    private void ApplyRotationForGem()
    {
        transform.Rotate(_rotationAxis * _rotationSpeed * Time.deltaTime);
    }

    private void ApplyFloatingForGem()
    {
        float newY = _initialLocalPosition.y + (Mathf.Sin(Time.time * _floatFrequency) * _floatAmplitude);

        Vector3 newPosition = new Vector3(_initialLocalPosition.x, newY, _initialLocalPosition.z);
        transform.localPosition = newPosition;
    }
}