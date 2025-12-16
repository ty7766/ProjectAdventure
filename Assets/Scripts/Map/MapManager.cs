using UnityEngine;

[System.Serializable]
public class PathGroup
{
    public string groupName = "New Path Group";
    public KeyCode triggerKey;
    public Transform spawnPoint;
    public GameObject[] pathPrefabs;

    [HideInInspector]
    public GameObject currentActivePath;
    [HideInInspector]
    public int currentPathIndex = 0;
}

public class MapManager : MonoBehaviour
{
    public PathGroup[] pathGroups;

    void Start()
    {
        // 등록된 모든 'PathGroup'을 순회하며 맵 생성
        foreach (PathGroup group in pathGroups)
        {
            if (group.pathPrefabs.Length > 0 && group.spawnPoint != null)
            {
                // 각 그룹의 0번째(첫 번째) 맵 생성 (Init)
                SpawnPath(group, group.currentPathIndex);
            }
            else
            {
                Debug.LogError($"MapManager의 '{group.groupName}' 그룹에 Prefabs 또는 Spawn Point가 설정되지 않았습니다!");
            }
        }
    }

    void Update()
    {
        //모든 PathGroup 을 순회하며 키 입력을 확인
        foreach (PathGroup group in pathGroups)
        {
            //triggerKey가 눌렸는지 확인
            if (Input.GetKeyDown(group.triggerKey))
            {
                group.currentPathIndex = (group.currentPathIndex + 1) % group.pathPrefabs.Length;

                //맵 교체
                SpawnPath(group, group.currentPathIndex);
            }
        }
    }

    void SpawnPath(PathGroup group, int index)
    {
        // 1. 이 'group'의 현재 활성화된 길이 있다면 파괴
        if (group.currentActivePath != null)
        {
            Destroy(group.currentActivePath);
        }

        // 2. 이 'group'의 프리팹 배열에서 'index'번째 프리팹을 가져오기
        GameObject pathPrefabToSpawn = group.pathPrefabs[index];

        // 3. 이 'group'의 'spawnPoint' 위치/회전 값으로 새 길을 생성
        group.currentActivePath = Instantiate(pathPrefabToSpawn, group.spawnPoint.position, group.spawnPoint.rotation);
        group.currentActivePath.transform.SetParent(this.transform);

        Debug.Log($"[{group.groupName}] '{pathPrefabToSpawn.name}' (으)로 길 교체! (트리거 키: {group.triggerKey})");
    }
}