using UnityEngine;

/// <summary>
/// 맵 선택 빔의 표시 정책. MapManager의 이벤트를 구독해 빔을 선택된 맵에 맞추고,
/// 교체 중에는 숨겼다가 쿨타임 종료 시 다시 보여주며, 플레이어가 선택된 맵 위에 있으면 붉게 표시한다.
/// MapManager는 입력/맵 교체만 담당하고 빔 연출은 이 클래스가 전담한다.
/// </summary>
public class MapSelectionBeamPresenter
{
    // 플레이어 위치에 따른 교체 불가(붉은색) 표시 갱신 주기
    private const float BlockedCheckInterval = 0.1f;

    private readonly MapManager _mapManager;
    private readonly MapSelectionBeam _beam;

    private bool _isRefitScheduled;
    private float _refitAt;
    private float _nextBlockedCheckAt;

    public MapSelectionBeamPresenter(MapManager mapManager, MapSelectionBeam beam)
    {
        _mapManager = mapManager;
        _beam = beam;

        _mapManager.OnCursorMoved += HandleCursorMoved;
        _mapManager.OnMapSwapStarting += HandleMapSwapStarting;
        _mapManager.OnMapSwapBlocked += HandleMapSwapBlocked;
        _mapManager.OnStateReset += HandleStateReset;
    }

    public void Dispose()
    {
        _mapManager.OnCursorMoved -= HandleCursorMoved;
        _mapManager.OnMapSwapStarting -= HandleMapSwapStarting;
        _mapManager.OnMapSwapBlocked -= HandleMapSwapBlocked;
        _mapManager.OnStateReset -= HandleStateReset;
    }

    /// <summary>MapManager.Update에서 매 프레임 호출합니다.</summary>
    public void Tick()
    {
        if (_isRefitScheduled && Time.time >= _refitAt)
        {
            // 낙하 애니메이션 완료 후 최종 위치 기준으로 재측정한 뒤 페이드인
            _isRefitScheduled = false;
            FitToSelectedMap();
            _beam.FadeIn();
        }

        // 플레이어가 맵에 오르거나 내리는 동안에도 빔 색상 피드백이 갱신되도록 주기적으로 체크
        if (Time.time >= _nextBlockedCheckAt)
        {
            RefreshBlockedState();
        }
    }

    //--- Event Handlers ---//
    private void HandleCursorMoved()
    {
        FitToSelectedMap();
        RefreshBlockedState();
    }

    private void HandleMapSwapStarting()
    {
        // 교체 시작: 빔을 페이드아웃 (맵 낙하 애니메이션과 겹치는 글리치 방지).
        // 이후의 Fit 요청은 빔이 완전히 사라진 뒤 적용되고, 쿨타임이 끝나면 다시 나타난다.
        _beam.FadeOut();
        _isRefitScheduled = true;
        _refitAt = Time.time + _mapManager.MapChangeCooldownTime;
    }

    private void HandleMapSwapBlocked()
    {
        _beam.SetBlocked(true);
    }

    private void HandleStateReset()
    {
        // 재시작으로 페이드 코루틴이 끊겼을 수 있으므로 표시 상태를 강제로 복구한다
        _isRefitScheduled = false;
        _beam.ResetFade();
    }

    //--- Private Methods ---//
    /// <summary>
    /// 선택된 그룹의 현재 활성 맵 인스턴스를 빔에 적용합니다.
    /// 콜라이더 footprint를 따라가며, 실패 시 렌더러 AABB, 맵이 없으면 기본 타일 규격으로 폴백합니다.
    /// </summary>
    private void FitToSelectedMap()
    {
        PathGroup group = _mapManager.SelectedGroup;
        if (group == null || group.SpawnPoint == null)
        {
            return;
        }

        if (group.CurrentActivePath == null)
        {
            _beam.FitToBounds(GetFallbackBounds(group));
            return;
        }

        if (!TryGetRendererBounds(group.CurrentActivePath, out Bounds bounds))
        {
            _beam.FitToBounds(GetFallbackBounds(group));
            return;
        }

        _beam.FitToMap(group.CurrentActivePath, bounds);
    }

    private Bounds GetFallbackBounds(PathGroup group)
    {
        Vector3 tileSize = _mapManager.GetTileSize();
        Vector3 center = group.SpawnPoint.position - Vector3.up * (tileSize.y * 0.5f);
        return new Bounds(center, tileSize);
    }

    /// <summary>맵 전체의 월드 바운딩 박스를 수집합니다. (파티클 등 과대 바운드 제외)</summary>
    private static bool TryGetRendererBounds(GameObject map, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in map.GetComponentsInChildren<Renderer>())
        {
            // 파티클/트레일 렌더러는 이펙트 재생 상태에 따라 바운드가 크게 변하므로 제외
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return hasBounds;
    }

    private void RefreshBlockedState()
    {
        _nextBlockedCheckAt = Time.time + BlockedCheckInterval;
        _beam.SetBlocked(_mapManager.IsPlayerOnSelectedMap());
    }
}
