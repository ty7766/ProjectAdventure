using UnityEngine;
using UnityEngine.Assertions;

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
    private FloatingCursor _cursorScript;

    private void Awake()
    {
        Assert.IsNotNull(_pathGroups, $"[MapManager] '{name}'에 Path Groups가 할당되지 않았습니다.");
        Assert.IsTrue(_pathGroups.Length > 0, $"[MapManager] '{name}'의 Path Groups 배열이 비어있습니다.");
        Assert.IsNotNull(_selectionCursor, $"[MapManager] '{name}'에 Selection Cursor가 할당되지 않았습니다.");

        // 커서 스크립트 캐싱
        if (_selectionCursor != null)
        {
            _cursorScript = _selectionCursor.GetComponent<FloatingCursor>();
        }
    }
    private void Start()
    {
        MapGeneration();
        UpdateCursorPosition();
    }

    private void Update()
    {
        HandleSelectionInput();
        HandleMapChangeInput();
    }

    private void MapGeneration()
    {
        // 등록된 모든 'PathGroup'을 순회하며 맵 생성
        foreach (PathGroup group in _pathGroups)
        {
            if (group.PathPrefabs.Length > 0 && group.SpawnPoint != null)
            {
                SpawnPath(group, group.CurrentPathIndex);
            }
        }
    }

    private void HandleSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeSelection(1);
        }
    }

    //direction이 -1이면 왼쪽, 1이면 오른쪽
    private void ChangeSelection(int direction)
    {
        _selectedSlotIndex += direction;

        // 인덱스 순환 처리 (Wrap around)
        if (_selectedSlotIndex < 0)
        {
            _selectedSlotIndex = _pathGroups.Length - 1;
        }
        else if (_selectedSlotIndex >= _pathGroups.Length)
        {
            _selectedSlotIndex = 0;
        }

        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (_selectionCursor == null || _pathGroups.Length == 0)
        {
            return;
        }

        Transform targetSpawnPoint = _pathGroups[_selectedSlotIndex].SpawnPoint;

        if (targetSpawnPoint == null)
        {
            CustomDebug.LogWarning($"[MapManager] SpawnPoint 누락: 인덱스 {_selectedSlotIndex}");
            return;
        }

        Vector3 targetBasePos = targetSpawnPoint.position + _cursorOffset;

        // 커서 스크립트 캐싱된 것 사용 (없으면 Transform 직접 이동)
        if (_cursorScript != null)
        {
            _cursorScript.SetBasePositionCursor(targetBasePos); // 함수명 변경 반영
        }
        else
        {
            _selectionCursor.position = targetBasePos;
        }
    }

    private void HandleMapChangeInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryChangeMap(-1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TryChangeMap(1);
        }
    }

    private void TryChangeMap(int direction)
    {
        PathGroup targetGroup = _pathGroups[_selectedSlotIndex];

        if (targetGroup.PathPrefabs == null || targetGroup.PathPrefabs.Length == 0)
        {
            CustomDebug.LogWarning($"[MapManager] '{targetGroup.GroupName}'에 교체할 맵 프리팹이 없습니다.");
            return;
        }

        if (CheckPlayerOnMap(targetGroup))
        {
            CustomDebug.Log("플레이어가 해당 맵 위에 있어 교체할 수 없습니다!");
            return;
        }

        int totalCount = targetGroup.PathPrefabs.Length;

        targetGroup.CurrentPathIndex = (targetGroup.CurrentPathIndex + direction + totalCount) % totalCount;

        SpawnPath(targetGroup, targetGroup.CurrentPathIndex);
    }

    private bool CheckPlayerOnMap(PathGroup group)
    {
        //SpawnPoint가 할당되지 않은 맵 방지
        if (group.SpawnPoint == null)
        {
            CustomDebug.LogWarning($"[MapManager] '{group.GroupName}' 그룹에 Spawn Point가 없습니다! Inspector를 확인하세요.");
            return false;
        }
        //해당 슬롯 위치에 박스를 만들어 검사
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