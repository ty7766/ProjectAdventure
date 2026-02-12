using UnityEngine;

public class MapPlayerChecker : MonoBehaviour
{
    const float DETECTIONY = 200;       //체크할 높이
    private Collider[] _hitBuffer = new Collider[100];
    /// <summary>
    /// 플레이어가 맵 위에 올라가 있는지 체크하는 메소드
    /// </summary>
    /// <param name="group">현재 위에 있는 맵</param>
    /// <returns></returns>
    public bool CheckPlayerOnThisMap(PathGroup group, Vector3 tileSize)
    {
        //SpawnPoint가 할당되지 않은 맵 방지
        if (group.SpawnPoint == null)
        {
            return false;
        }
        Vector3 checkSize = tileSize + Vector3.up * DETECTIONY;

        int hitCount = Physics.OverlapBoxNonAlloc(
            group.SpawnPoint.position,
            checkSize * 0.5f,
            _hitBuffer,
            group.SpawnPoint.rotation
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];
            if (hit.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
}
