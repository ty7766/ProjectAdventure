using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isMissing = false;
    public static T Instance
    {
        get
        {
            //Lazy Initialization
            if (_instance == null && !_isMissing)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    Debug.LogError($"씬에 {typeof(T).Name} 이 없습니다. 하이어라키 창에 올려주세요.");
                    _isMissing = true;
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
        _isMissing = false;
        DontDestroyOnLoad(gameObject);
    }
}
