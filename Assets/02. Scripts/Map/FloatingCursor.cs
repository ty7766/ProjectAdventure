using UnityEngine;

public class FloatingCursor : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField]
    private float _speed = 2f;
    [SerializeField]
    private float _height = 0.5f;

    private Vector3 _basePosition;
    private bool _isInitialized = false;

    private void Update()
    {
        if(!_isInitialized)
        {
            return;
        }

        float newY = _basePosition.y + Mathf.Sin(Time.time * _speed) * _height;

        //y축만 수직 운동
        transform.position = new Vector3 (_basePosition.x, newY, _basePosition.z);
    }

    public void SetBasePositionCursor(Vector3 newPosition)
    {
        _basePosition = newPosition;
        _isInitialized = true;
        transform.position = _basePosition;
    }
}
