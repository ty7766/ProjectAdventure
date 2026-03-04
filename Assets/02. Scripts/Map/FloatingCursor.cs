using UnityEngine;

public class FloatingCursor : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField]
    private float _cursorMoveSpeed = 2f;
    [SerializeField]
    private float _cursorBobbingHeight = 0.5f;

    private Vector3 _basePosition;
    private bool _isInitialized = false;

    private void Update()
    {
        if(!_isInitialized)
        {
            return;
        }

        ProcessFloatingCursor();
    }

    /// <summary>
    /// 맵 어디에 커서를 고정시킬지 설정하는 메소드
    /// </summary>
    /// <param name="newPosition">커서를 고정할 위치</param>
    public void SetBasePositionCursor(Vector3 newPosition)
    {
        _basePosition = newPosition;
        _isInitialized = true;
        transform.position = _basePosition;
    }
    
    private void ProcessFloatingCursor()
    {
        transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * _cursorMoveSpeed) * _cursorBobbingHeight);
    }
}
