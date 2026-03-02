using UnityEngine;
using System.Collections.Generic;

public class VFXManager : Singleton<VFXManager>
{
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

    private Dictionary<VFXType, Queue<GameObject>> _poolDictionary;
    private Dictionary<VFXType, GameObject> _vfxPrefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        InitializePool();
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
                CreateNewVFXObject(type, vfxPrefab);
            }
            else
            {
                CustomDebug.LogWarning($"VFXManager: 프리팹 타입 {type} 이 null 입니다.");
                return null;
            }
        }

        GameObject vfxObject = _poolDictionary[type].Dequeue();
        if (vfxObject == null)
        {
            return PlayVFX(type, position, rotation);
        }
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
        _poolDictionary = new Dictionary<VFXType, Queue<GameObject>>();
        _vfxPrefabDictionary = new Dictionary<VFXType, GameObject>();

        foreach (var vfxData in _vfxList)
        {
            if (!ValidateVFXData(vfxData))
            {
                continue;
            }

            InitializeVFXType(vfxData);
        }
    }

    private bool ValidateVFXData(VFXData data)
    {
        if(_poolDictionary.ContainsKey(data.Type))
        {
            CustomDebug.LogWarning($"VFXManager: {data.Type}이 중복 등록되었습니다.");
            return false;
        }

        if(data.Prefab == null)
        {
            CustomDebug.LogWarning($"VFXManager: {data.Type}의 Prefab이 null입니다.");
            return false;
        }

        return true;
    }

    private void InitializeVFXType(VFXData data)
    {
        //Dictionary Init
        _poolDictionary.Add(data.Type, new Queue<GameObject>());
        _vfxPrefabDictionary[data.Type] = data.Prefab;

            for (int i = 0; i < data.PoolSize; i++)
            {
                CreateNewVFXObject(data.Type, data.Prefab);
            }
    }

    private GameObject CreateNewVFXObject(VFXType type, GameObject prefab)
    {
        GameObject vfxObject = Instantiate(prefab, transform);

        SetupVFXReturnToPool(vfxObject, type);

        vfxObject.SetActive(false);
        _poolDictionary[type].Enqueue(vfxObject);

        return vfxObject;
    }

    private void SetupVFXReturnToPool(GameObject vfxObject, VFXType type)
    {
        var returnScript = vfxObject.GetComponent<VFXReturnToPool>();
        if(returnScript == null)
        {
            returnScript = vfxObject.AddComponent<VFXReturnToPool>();
        }

        returnScript.Setup(type);
    }
}
