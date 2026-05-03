using UnityEngine;
using UnityEngine.Assertions;

//맵 프리팹 클래스 
[System.Serializable]
public class MapInfo
{
    public GameObject Prefab;

    [Header("개별 위치/회전 보정")]
    public Vector3 OffsetPosition; // 이 맵만의 고유 위치 보정
    public Vector3 OffsetRotation; // 이 맵만의 고유 회전 보정
}

//PathGroup 데이터 클래스
[System.Serializable]
public class PathGroup
{
    private const string DefaultGroupName = "New Path Group";

    public string GroupName = DefaultGroupName;
    public Transform SpawnPoint;
    public MapInfo[] Maps;

    [HideInInspector]
    public GameObject CurrentActivePath;
    [HideInInspector]
    public int CurrentPathIndex = 0;
}

[RequireComponent(typeof(MapPlayerChecker))]
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

    [Header("맵 설정")]
    [SerializeField]
    private Vector3 _tileSize = new Vector3(12, 1, 10);
    [SerializeField]
    private Vector3 _startOffset = Vector3.zero;  //시작위치 보정

    [Header("맵 교체 쿨타임")]
    [SerializeField]
    private float _mapChangeCooldownTime = 1.0f;

    private MapChangeEffect _mapChangeEffect;

    private int _selectedSlotIndex = 0;
    private float _nextAllowedMapChangeTime;
    private FloatingCursor _cursorScript;
    private MapPlayerChecker _playerCheckerScript;
    private GameControls _controls;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> _onSelectMap;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> _onChangeMap;
    private bool _isControlEnabled = false;

    public bool IsControlEnabled
    {
        get => _isControlEnabled;
        set
        {
            _isControlEnabled = value;
        }
    }

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
        _playerCheckerScript = GetComponent<MapPlayerChecker>();

        _controls = InputManager.Instance.GameControls;
        _onSelectMap = ctx => ChangeSelection(Mathf.RoundToInt(ctx.ReadValue<float>()));
        _onChangeMap = ctx => TryChangeMap(Mathf.RoundToInt(ctx.ReadValue<float>()));
    }

    private void Start()
    {
        MapGeneration();
        UpdateCursorPosition();
    }

    private void OnEnable()
    {
        if(_controls != null)
        {
            _controls.Map.Enable();
            _controls.Map.SelectMap.started += _onSelectMap;
            _controls.Map.ChangeMap.started += _onChangeMap;
        }
    }

    private void OnDisable()
    {
        if(_controls != null)
        {
            _controls.Map.Disable();
            _controls.Map.SelectMap.started -= _onSelectMap;
            _controls.Map.ChangeMap.started -= _onChangeMap;
        }
    }

    private void OnDestroy()
    {
    }

    /// <summary>
    /// 외부(StageLoader 등)에서 MapChangeEffect 레퍼런스를 주입합니다.
    /// </summary>
    public void SetMapChangeEffect(MapChangeEffect effect)
    {
        _mapChangeEffect = effect;
    }

    /// <summary>
    /// 스테이지 재시작 시 맵 선택 상태를 초기화합니다.
    /// </summary>
    public void ResetState()
    {
        _selectedSlotIndex = 0;
        _nextAllowedMapChangeTime = 0f;
        foreach (PathGroup group in _pathGroups)
        {
            if (group.CurrentPathIndex != 0)
            {
                group.CurrentPathIndex = 0;
                if (group.Maps != null && group.Maps.Length > 0 && group.SpawnPoint != null)
                {
                    SpawnPath(group, 0);
                }
            }
        }
        UpdateCursorPosition();
    }

    /// <summary>
    /// MapGuideLine을 위한 타일 사이즈 리턴 메소드
    /// </summary>
    public Vector3 GetTileSize()
    {
        return _tileSize;
    }

    /// <summary>
    /// MapGuideLine을 위한 타일 이니셜 오프셋 메소드
    /// </summary>
    public Vector3 GetStartOffset()
    {
        return _startOffset;
    }
    private void MapGeneration()
    {
        // 등록된 모든 'PathGroup'을 순회하며 맵 생성
        foreach (PathGroup group in _pathGroups)
        {
            if (group.Maps != null && group.Maps.Length > 0 && group.SpawnPoint != null)
            {
                SpawnPath(group, group.CurrentPathIndex);
            }
        }
    }

    //direction이 -1이면 왼쪽, 1이면 오른쪽
    private void ChangeSelection(int direction)
    {
        if (!_isControlEnabled)
        {
            return;
        }

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

        SoundManager.Instance.PlaySFX(SoundType.SFX_MapSwitch);
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (_selectionCursor == null || _pathGroups.Length == 0)
        {
            return;
        }

        PathGroup currentGroup = _pathGroups[_selectedSlotIndex];
        if (currentGroup.SpawnPoint == null)
        {
            return;
        }

        Vector3 finalPos = currentGroup.SpawnPoint.position;
        Vector3 targetBasePos = finalPos + _cursorOffset;

        if (_cursorScript != null)
        {
            _cursorScript.SetBasePositionCursor(targetBasePos);
        }
        else
        {
            _selectionCursor.position = targetBasePos;
        }
    }

    private void TryChangeMap(int direction)
    {
        if (!_isControlEnabled)
        {
            return;
        }

        if (Time.time < _nextAllowedMapChangeTime)
        {
            SoundManager.Instance.PlaySFX(SoundType.SFX_MapChangeAlert);
            return;
        }

        PathGroup targetGroup = _pathGroups[_selectedSlotIndex];

        if (targetGroup.Maps == null || targetGroup.Maps.Length == 0)
        {
            CustomDebug.LogWarning($"[MapManager] '{targetGroup.GroupName}'에 교체할 맵 정보가 없습니다.");
            return;
        }

        //플레이어가 해당 맵 위에 있을 경우
        if (_playerCheckerScript.CheckPlayerOnThisMap(targetGroup, _tileSize))
        {
            SoundManager.Instance.PlaySFX(SoundType.SFX_MapChangeAlert);
            return;
        }

        int totalCount = targetGroup.Maps.Length;

        targetGroup.CurrentPathIndex = (targetGroup.CurrentPathIndex + direction + totalCount) % totalCount;

        TransitionPath(targetGroup, targetGroup.CurrentPathIndex);

        UpdateCursorPosition();
        SoundManager.Instance.PlaySFX(SoundType.SFX_MapChange);

        _nextAllowedMapChangeTime = Time.time + _mapChangeCooldownTime;
    }

    private void SpawnPath(PathGroup group, int index)
    {
        if (group.CurrentActivePath != null)
        {
            Destroy(group.CurrentActivePath);
        }

        MapInfo mapInfo = group.Maps[index];
        if (mapInfo.Prefab == null)
        {
            CustomDebug.LogWarning($"[MapManager] '{group.GroupName}'의 {index}번 프리팹이 비어있습니다.");
            return;
        }
        Vector3 finalPosition = group.SpawnPoint.position + mapInfo.OffsetPosition;

        //스폰 포인트 회전 * 개별 맵의 회전
        Quaternion finalRotation = group.SpawnPoint.rotation * Quaternion.Euler(mapInfo.OffsetRotation);

        group.CurrentActivePath = Instantiate(mapInfo.Prefab, finalPosition, finalRotation);
        group.CurrentActivePath.transform.SetParent(transform);
    }

    private void TransitionPath(PathGroup group, int index)
    {
        MapInfo mapInfo = group.Maps[index];
        if (mapInfo.Prefab == null)
        {
            CustomDebug.LogWarning($"[MapManager] '{group.GroupName}'의 {index}번 프리팹이 비어있습니다.");
            return;
        }

        Vector3 finalPosition = group.SpawnPoint.position + mapInfo.OffsetPosition;
        Quaternion finalRotation = group.SpawnPoint.rotation * Quaternion.Euler(mapInfo.OffsetRotation);

        if (group.CurrentActivePath != null)
        {
            if (_mapChangeEffect != null)
                _mapChangeEffect.PlayExit(group.CurrentActivePath);
            else
                Destroy(group.CurrentActivePath);
        }

        // 최종 위치에서 생성 → 자식 컴포넌트 OnEnable이 올바른 부모 위치 기준으로 초기화됨
        group.CurrentActivePath = Instantiate(mapInfo.Prefab, finalPosition, finalRotation);
        group.CurrentActivePath.transform.SetParent(transform);

        // PlayEnter 내부에서 위로 올린 뒤 내려오는 애니메이션 처리
        _mapChangeEffect?.PlayEnter(group.CurrentActivePath, finalPosition);
    }
}