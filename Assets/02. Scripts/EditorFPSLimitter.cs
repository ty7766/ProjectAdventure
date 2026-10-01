using UnityEngine;

public class EditorFPSLimitter : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private bool _isLimitFPS = true;
    [SerializeField] private int _maxFPS = 15;

    private void Start()
    {
        ApplyFrameRate();
    }

    private void Update()
    {
        if (_isLimitFPS && (Application.targetFrameRate != _maxFPS || QualitySettings.vSyncCount != 0))
        {
            ApplyFrameRate();
        }
    }

    private void OnValidate()
    {
        if (_maxFPS < 1)
        {
            _maxFPS = 1;
        }
    }

    private void ApplyFrameRate()
    {
        if (_isLimitFPS)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _maxFPS;
        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }
#endif
}
