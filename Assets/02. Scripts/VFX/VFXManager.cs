using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;

    public static VFXManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [System.Serializable]
    public struct VFXData
    {
        public VFXType Type;
        public GameObject Prefab;
        public int PoolSize;
    }

    [Header("이펙트 등록 (Inspector")]
    [SerializeField]
    private List<VFXData> _vfxList;

    private Dictionary<VFXType, Queue<GameObject>> _poolDictionary = new Dictionary<VFXType, Queue<GameObject>>();
    private Dictionary<VFXType, GameObject> _vfxPrefabDictionary = new Dictionary<VFXType, GameObject>();

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 이펙트 생성
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="position">이펙트 생성될 위치</param>
    public GameObject PlayVFX(VFXType type, Vector3 position, Quaternion rotation = default)
    {
        if(!_poolDictionary.ContainsKey(type))
        {
            CustomDebug.LogWarning($"VFXManager: {type} 타입의 풀이 존재하지 않습니다.");
            return null;
        }

        //대기열 비었으면 추가 생성
        if (_poolDictionary[type].Count == 0)
        {
            if(_vfxPrefabDictionary.TryGetValue(type, out GameObject vfxPrefab))
            {
                CreateNewObject(type, vfxPrefab);
            }
            else
            {
                CustomDebug.LogWarning($"VFXManager: 프리팹 타입 {type} 이 null 입니다.");
                return null;
            }
        }

        GameObject vfxObject = _poolDictionary[type].Dequeue();
        vfxObject.transform.position = position;
        vfxObject.transform.rotation = rotation.Equals(default(Quaternion)) ? Quaternion.identity : rotation;
        vfxObject.SetActive(true);

        return vfxObject;
    }

    /// <summary>
    /// 기존 호환성을 위한 오버로딩
    /// </summary>
    /// <param name="type">VFX 타입</param>
    /// <param name="position">VFX가 생성될 위치</param>
    /// <returns></returns>
    public GameObject PlayVFX(VFXType type, Vector3 position)
    {
        return PlayVFX(type, position, Quaternion.identity);
    }

    /// <summary>
    /// 이펙트를 풀에 반납
    /// </summary>
    /// <param name="type">VFX타입</param>
    /// <param name="vfxObject">반납할 이펙트</param>
    public void ReturnToPool(VFXType type, GameObject vfxObject)
    {
        if(!_poolDictionary.ContainsKey(type))
        {
            Destroy(vfxObject);
            return;
        }

        vfxObject.SetActive(false);
        _poolDictionary[type].Enqueue(vfxObject);
    }

    //설정된 개수만큼 미리 생성
    private void InitializePool()
    {
        //중복 방지
        _poolDictionary.Clear();
        _vfxPrefabDictionary.Clear();

        foreach(var vfxType in _vfxList)
        {
            if(_poolDictionary.ContainsKey(vfxType.Type))
            {
                continue;
            }

            if (vfxType.Prefab == null)
            {
                continue;
            }

            //Dictionary Init
            _poolDictionary.Add(vfxType.Type, new Queue<GameObject>());
            _vfxPrefabDictionary.Add(vfxType.Type, vfxType.Prefab);

            for (int i = 0; i < vfxType.PoolSize; i++)
            {
                CreateNewObject(vfxType.Type, vfxType.Prefab);
            }
        }
    }

    private GameObject CreateNewObject(VFXType type, GameObject prefab)
    {
        GameObject effectObject = Instantiate(prefab, transform);

        var returnScript = effectObject.GetComponent<VFXReturnToPool>();
        if(returnScript == null)
        {
            returnScript = effectObject.AddComponent<VFXReturnToPool>();
        }

        returnScript.Setup(type);

        effectObject.SetActive(false);
        _poolDictionary[type].Enqueue(effectObject);
        return effectObject;
    }
}
