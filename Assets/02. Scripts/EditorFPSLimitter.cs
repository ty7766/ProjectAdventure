using UnityEngine;

public class EditorFPSLimitter : MonoBehaviour
{

#if UNITY_EDITOR
    [SerializeField] private bool _isLimitFPS = true;
    [SerializeField] private int _maxFPS = 15;
    void Start()
    {
        if (_isLimitFPS)
        {
            Application.targetFrameRate = _maxFPS;
        }
    }

    private void OnValidate()
    {
        if (_maxFPS < 1)
        {
            _maxFPS = 1;
        }
        if (_isLimitFPS)
        {
            Application.targetFrameRate = _maxFPS;
        }
    }
#endif
}
