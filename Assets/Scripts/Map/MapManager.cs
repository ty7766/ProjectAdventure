using UnityEngine;

[System.Serializable]
public class PathGroup
{
    public string groupName = "New Path Group";
    public Transform spawnPoint;
    public GameObject[] pathPrefabs;

    [HideInInspector]
    public GameObject currentActivePath;
    [HideInInspector]
    public int currentPathIndex = 0;
}

public class MapManager : MonoBehaviour
{
    [Header("맵 선택 이펙트 설정")]
    public Transform selectionCursor;
    public Vector3 cursorOffset = new Vector3(0, 0, 0);

    [Header("맵 그룹 설정")]
    public PathGroup[] pathGroups;

    [Header("맵 변경 안전장치 설정")]
    //해당 맵 사이즈로 변경 필수
    public Vector3 detectionSize = new Vector3(0, 0, 0);

    private int selectedSlotIndex = 0;

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
        }

        UpdateCursorPosition();
    }

    void Update()
    {
        if (pathGroups.Length == 0) return;
        HandleSelectionInput();
        HandleMapChangeInput();
    }

    //맵 선택 기능
    void HandleSelectionInput()
    {
        bool selectionChanged = false;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedSlotIndex--;
            if (selectedSlotIndex < 0)
                selectedSlotIndex = pathGroups.Length - 1;

            selectionChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedSlotIndex++;
            if (selectedSlotIndex >= pathGroups.Length)
                selectedSlotIndex = 0;

            selectionChanged = true;
        }

        //선택 되었을 때만 이펙트 생성
        if (selectionChanged)
        {
            UpdateCursorPosition();
        }
    }

    //커서 이펙트를 선택된 맵으로 이동
    void UpdateCursorPosition()
    {
        if (selectionCursor == null) return;

        Transform targetSpawnPoint = pathGroups[selectedSlotIndex].spawnPoint;
        selectionCursor.position = targetSpawnPoint.position + cursorOffset;
    }

    //선택된 맵 변경
    void HandleMapChangeInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PathGroup targetGroup = pathGroups[selectedSlotIndex];

            //플레이어가 해당 맵 위에 있는지 확인
            if (CheckPlayerOnMap(targetGroup))
            {
                Debug.Log("플레이어가 현재 해당 맵 위에 있습니다.");
                Debug.Log("해당 맵을 교체할 수 없습니다!");
                return;
            }

            if (targetGroup.pathPrefabs.Length == 0) return;

            targetGroup.currentPathIndex = (targetGroup.currentPathIndex + 1) % targetGroup.pathPrefabs.Length;
            SpawnPath(targetGroup, targetGroup.currentPathIndex);
        }
    }

    //플레이어 감지용 메소드
    bool CheckPlayerOnMap(PathGroup group)
    {
        //해당 슬롯 위치에 가상 박스를 만들어 검사
        Collider[] hitColliders = Physics.OverlapBox(
            group.spawnPoint.position,
            detectionSize * 0.5f,
            group.spawnPoint.rotation);

        //감지된 물체 중 Player 태그 검사
        foreach(Collider col in hitColliders)
        {
            if(col.CompareTag("Player"))
                return true;
        }
        return false;
    }

    void SpawnPath(PathGroup group, int index)
    {
        //이 'group'의 현재 활성화된 길이 있다면 파괴
        if (group.currentActivePath != null)
        {
            Destroy(group.currentActivePath);
        }

        // 2. 이 'group'의 프리팹 배열에서 'index'번째 프리팹을 가져오기
        GameObject pathPrefabToSpawn = group.pathPrefabs[index];

        // 3. 이 'group'의 'spawnPoint' 위치/회전 값으로 새 길을 생성
        group.currentActivePath = Instantiate(pathPrefabToSpawn, group.spawnPoint.position, group.spawnPoint.rotation);
        group.currentActivePath.transform.SetParent(this.transform);

        Debug.Log($"[슬롯 변경] {group.groupName} -> {pathPrefabToSpawn.name}");
    }
}