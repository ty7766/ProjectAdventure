using UnityEngine;

public class MapPlayerChecker : MonoBehaviour
{
    const float DETECTIONY = 200;       //체크할 높이

    private MapManager _mapManager;
    private Collider[] _hitBuffer = new Collider[5];    //GC 방지 위함

    private void Awake()
    {
        _mapManager = GetComponent<MapManager>();
    }
    /// <summary>
    /// 플레이어가 맵 위에 올라가 있는지 체크하는 메소드
    /// </summary>
    /// <param name="group">현재 위에 있는 맵</param>
    /// <returns></returns>
    public bool CheckPlayerOnThisMap(PathGroup group)
    {

        //SpawnPoint가 할당되지 않은 맵 방지
        if (_mapManager == null || group.SpawnPoint == null)
        {
            return false;
        }

        Vector3 checkSize = _mapManager.GetTileSize() + Vector3.up * DETECTIONY;

        //해당 슬롯 위치에 박스를 만들어 검사
        int hitCount = Physics.OverlapBoxNonAlloc(
            group.SpawnPoint.position,
            checkSize * 0.5f,
            _hitBuffer,
            group.SpawnPoint.rotation);

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
}
