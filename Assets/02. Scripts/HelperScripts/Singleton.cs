using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isMissing = false;
    // 제네릭 타입 T에서 != null은 C# 기본 참조 비교를 사용해 파괴된 Unity 오브젝트를 null로 인식하지 못함.
    // UnityEngine.Object로 캐스팅해 Unity의 오버로드된 연산자를 강제 사용.
    private static bool IsUnityAlive => (UnityEngine.Object)(object)_instance != null;
    public static bool HasInstance => IsUnityAlive;
    protected virtual bool PersistAcrossScenes => true;

    public static T Instance
    {
        get
        {
            //Lazy Initialization
            if (!IsUnityAlive && !_isMissing)
            {
                _instance = FindAnyObjectByType<T>();
                if ((UnityEngine.Object)(object)_instance == null)
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
        if (IsUnityAlive && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
        _isMissing = false;

        if(PersistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
