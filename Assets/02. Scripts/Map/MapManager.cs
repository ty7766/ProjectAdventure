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
    private MapSelectionBeam _selectionBeam;

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

    /// <summary>
    /// 맵 교체 쿨다운 잔여 비율(1 = 방금 교체됨, 0 = 사용 가능).
    /// HUD(모바일 터치 UI)의 쿨다운 Fill 연출에 사용합니다.
    /// </summary>
    public float MapChangeCooldownRemaining01
    {
        get
        {
            float remaining = _nextAllowedMapChangeTime - Time.time;
            if (remaining <= 0f)
            {
                return 0f;
            }
            return Mathf.Clamp01(remaining / _mapChangeCooldownTime);
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
            _selectionBeam = _selectionCursor.GetComponentInChildren<MapSelectionBeam>(true);
            if (_selectionBeam != null)
            {
                // 초기 기본 규격 (첫 FitToBounds 전까지의 폴백)
                _selectionBeam.SetTileSize(_tileSize);
            }
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

    private void Update()
    {
        // 플레이어가 맵에 오르거나 내리는 동안에도 빔 색상 피드백이 갱신되도록 주기적으로 체크
        RefreshSelectionBeamState();
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

    /// <summary>
    /// 키보드 입력 외(터치 UI 등)에서 맵 선택 커서를 이동시키는 public 진입점.
    /// direction: -1 = 왼쪽, 1 = 오른쪽
    /// </summary>
    public void SelectMap(int direction)
    {
        ChangeSelection(direction);
    }

    /// <summary>
    /// 키보드 입력 외(터치 UI 등)에서 선택된 맵 그룹의 맵을 교체하는 public 진입점.
    /// direction: -1 = 이전 맵, 1 = 다음 맵
    /// </summary>
    public void SwapMap(int direction)
    {
        TryChangeMap(direction);
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

        // 선택된 맵의 실제 크기를 측정해 빔을 맵에 꼭 맞게 적응시킴
        UpdateSelectionBeamFit(currentGroup);

        RefreshSelectionBeamState();
    }

    /// <summary>
    /// 그룹의 현재 활성 맵 인스턴스를 빔에 적용합니다.
    /// 콜라이더 footprint(마칭 스퀘어)를 따라가며, 실패 시 렌더러 AABB로 폴백합니다.
    /// </summary>
    private void UpdateSelectionBeamFit(PathGroup group)
    {
        if (_selectionBeam == null)
        {
            return;
        }

        if (group.CurrentActivePath == null)
        {
            // 맵이 아직 없으면 SpawnPoint 중심의 기본 타일 규격으로 폴백
            Vector3 fallbackCenter = group.SpawnPoint.position - Vector3.up * (_tileSize.y * 0.5f);
            var fallbackBounds = new Bounds(fallbackCenter, _tileSize);
            _selectionBeam.FitToBounds(fallbackBounds);
            return;
        }

        // 맵 전체의 월드 바운딩 박스를 수집 (파티클 등 과대 바운드 제외)
        var renderers = group.CurrentActivePath.GetComponentsInChildren<Renderer>();
        Bounds bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            // 파티클/트레일 렌더러는 이펙트 재생 상태에 따라 바운드가 크게 변하므로 제외
            if (renderers[i] is UnityEngine.ParticleSystemRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
        {
            Vector3 fallbackCenter = group.SpawnPoint.position - Vector3.up * (_tileSize.y * 0.5f);
            var fallbackBounds = new Bounds(fallbackCenter, _tileSize);
            _selectionBeam.FitToBounds(fallbackBounds);
            return;
        }

        _selectionBeam.FitToMap(group.CurrentActivePath, bounds);
    }

    /// <summary>
    /// 선택된 맵 위에 플레이어가 올라가 있어 교체가 불가능한 경우 빔을 붉게 표시합니다.
    /// </summary>
    private void RefreshSelectionBeamState()
    {
        if (_selectionBeam == null || _playerCheckerScript == null)
        {
            return;
        }

        PathGroup currentGroup = _pathGroups[_selectedSlotIndex];
        if (currentGroup == null || currentGroup.SpawnPoint == null)
        {
            return;
        }

        bool playerOnMap = _playerCheckerScript.CheckPlayerOnThisMap(currentGroup, _tileSize);
        _selectionBeam.SetBlocked(playerOnMap);
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
            _selectionBeam?.SetBlocked(true);
            return;
        }

        int totalCount = targetGroup.Maps.Length;

        targetGroup.CurrentPathIndex = (targetGroup.CurrentPathIndex + direction + totalCount) % totalCount;

        // 교체 시작: 빔을 페이드아웃 (맵 낙하 애니메이션과 겹치는 글리치 방지)
        _selectionBeam?.FadeOut();
        StopCoroutine(RefitBeamIdle());

        TransitionPath(targetGroup, targetGroup.CurrentPathIndex);

        UpdateCursorPosition();
        // 새 맵 인스턴스의 실제 크기에 빔 재적응 (페이드아웃 중이므로 보이지 않음)
        UpdateSelectionBeamFit(targetGroup);
        SoundManager.Instance.PlaySFX(SoundType.SFX_MapChange);

        _nextAllowedMapChangeTime = Time.time + _mapChangeCooldownTime;

        // 쿨타임이 끝나는 시점에 맞춰 빔을 페이드인
        StartCoroutine(RefitBeamIdle());
    }

    /// <summary>
    /// 쿨타임이 끝나 빔이 다시 보여도 되는 시점까지 대기한 뒤, 최종 맵 위치로 재적응하고 페이드인합니다.
    /// </summary>
    private System.Collections.IEnumerator RefitBeamIdle()
    {
        yield return new WaitForSeconds(_mapChangeCooldownTime);

        // 낙하 애니메이션 완료 후 최종 위치 기준으로 재측정한 뒤 페이드인
        if (_selectedSlotIndex < _pathGroups.Length)
        {
            UpdateSelectionBeamFit(_pathGroups[_selectedSlotIndex]);
        }
        _selectionBeam?.FadeIn();
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
        if (_mapChangeEffect != null)
            _mapChangeEffect.PlayEnter(group.CurrentActivePath, finalPosition);
    }
}