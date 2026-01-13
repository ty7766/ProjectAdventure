using UnityEngine;

//PathGroup 데이터 클래스
[System.Serializable]
public class PathGroup
{
    private const string DefaultGroupName = "New Path Group";

    public string GroupName = DefaultGroupName;
    public Transform SpawnPoint;
    public GameObject[] PathPrefabs;

    [HideInInspector]
    public GameObject CurrentActivePath;
    [HideInInspector]
    public int CurrentPathIndex = 0;
}

public class MapManager : MonoBehaviour
{
    [Header("맵 선택 이펙트 설정")]
    [SerializeField]
    private Transform _selectionCursor;
    [SerializeField]
    private Vector3 _cursorOffset = Vector3.zero;

    [Header("맵 그룹 설정")]
    [SerializeField]
    private PathGroup[] _pathGroups;

    [Header("맵 변경 안전장치 설정")]
    [SerializeField]
    //해당 맵 사이즈로 변경 필수
    private Vector3 _detectionSize = Vector3.zero;

    private int _selectedSlotIndex = 0;

    void Start()
    {
        if(_pathGroups == null)
        {
            return;
        }

        // 등록된 모든 'PathGroup'을 순회하며 맵 생성
        foreach (PathGroup group in _pathGroups)
        {
            if (group.PathPrefabs.Length > 0 && group.SpawnPoint != null)
            {
                // 각 그룹의 0번째(첫 번째) 맵 생성 (Init)
                SpawnPath(group, group.CurrentPathIndex);
            }
        }

        UpdateCursorPosition();
    }

    void Update()
    {
        if (_pathGroups == null || _pathGroups.Length == 0)
        {
            return;
        }

        HandleSelectionInput();
        HandleMapChangeInput();
    }

    private void HandleSelectionInput()
    {
        bool selectionChanged = false;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _selectedSlotIndex--;
            if (_selectedSlotIndex < 0)
            {
                _selectedSlotIndex = _pathGroups.Length - 1;
            }

            selectionChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _selectedSlotIndex++;
            if (_selectedSlotIndex >= _pathGroups.Length)
            {
                _selectedSlotIndex = 0;
            }

            selectionChanged = true;
        }

        //선택 되었을 때만 이펙트 생성
        if (selectionChanged)
        {
            UpdateCursorPosition();
        }
    }
    private void UpdateCursorPosition()
    {
        if (_selectionCursor == null)
        {
            return;
        }

        Transform targetSpawnPoint = _pathGroups[_selectedSlotIndex].SpawnPoint;
        //SpawnPoint가 할당되지 않은 맵 방지
        if (targetSpawnPoint == null)
        {
            CustomDebug.LogWarning($"[MapManager] PathGroup[{_selectedSlotIndex}]의 spawnPoint가 설정되지 않았습니다.");
            return;
        }
        _selectionCursor.position = targetSpawnPoint.position + _cursorOffset;
    }

    private void HandleMapChangeInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PathGroup targetGroup = _pathGroups[_selectedSlotIndex];

            //플레이어가 해당 맵 위에 있는지 확인
            if (CheckPlayerOnMap(targetGroup))
            {
                CustomDebug.Log("플레이어가 현재 해당 맵 위에 있습니다.");
                CustomDebug.Log("해당 맵을 교체할 수 없습니다!");
                return;
            }

            if (targetGroup.PathPrefabs.Length == 0)
            {
                return;
            }

            targetGroup.CurrentPathIndex = (targetGroup.CurrentPathIndex + 1) % targetGroup.PathPrefabs.Length;
            SpawnPath(targetGroup, targetGroup.CurrentPathIndex);
        }
    }

    private bool CheckPlayerOnMap(PathGroup group)
    {
        //SpawnPoint가 할당되지 않은 맵 방지
        if (group.SpawnPoint == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[MapManager] '{group.GroupName}' 그룹에 Spawn Point가 없습니다! Inspector를 확인하세요.");
#endif
            return false;
        }
        //해당 슬롯 위치에 가상 박스를 만들어 검사
        Collider[] hitColliders = Physics.OverlapBox(
            group.SpawnPoint.position,
            _detectionSize * 0.5f,
            group.SpawnPoint.rotation);

        foreach(Collider col in hitColliders)
        {
            if(col.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnPath(PathGroup group, int index)
    {
        if (group.CurrentActivePath != null)
        {
            Destroy(group.CurrentActivePath);
        }

        GameObject pathPrefabToSpawn = group.PathPrefabs[index];

        //이 'group'의 'spawnPoint' 위치/회전 값으로 새 길을 생성
        group.CurrentActivePath = Instantiate(pathPrefabToSpawn, group.SpawnPoint.position, group.SpawnPoint.rotation);
        group.CurrentActivePath.transform.SetParent(this.transform);

        CustomDebug.Log($"[슬롯 변경] {group.GroupName} -> {pathPrefabToSpawn.name}");
    }
}