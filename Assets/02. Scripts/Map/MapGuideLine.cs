using UnityEngine;

//각 맵의 범위를 보여주는 가상 가이드라인
public class MapGuideLine : MonoBehaviour
{
    [Header("기준점 설정")]
    [SerializeField]
    private Transform _originTransform;

    [Header("정렬할 SpawnPoint")]
    [SerializeField]
    private Transform[] _spawnPointToAlign;

    [Header("맵 설정")]
    [SerializeField]
    private Vector3 _tileSize = new Vector3(15, 1, 10);
    [SerializeField]
    private Vector3 _startOffset = Vector3.zero;  //시작위치 보정

    [Header("가이드라인 색상")]
    [SerializeField]
    private Color _gizmoColor = Color.yellow;

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;

        //기준점 설정 (설정되지 않으면 해당 오브젝트 기준으로 정렬)
        Vector3 basePosition = (_originTransform != null) ? _originTransform.position : transform.position;

        int count = (_spawnPointToAlign != null && _spawnPointToAlign.Length > 0) ? _spawnPointToAlign.Length : 5;
        for(int i = 0; i < count; i++)
        {
            //현재는 -z 방향으로 맵이 이어지므로 -z 축으로 설정
            Vector3 pos = CalculatePosition(basePosition, i);
            Gizmos.DrawWireCube(pos, _tileSize);
            Gizmos.DrawSphere(pos, 0.3f);
        }
    }


    /// <summary>
    /// SpawnPoint 배열에 등록된 오브젝트들을 설정된 타일 크기에 맞춰 일렬로 자동 정렬
    /// </summary>
    [ContextMenu("위치 자동 정렬")]
    public void AlignPositions()
    {
        if(_spawnPointToAlign == null || _spawnPointToAlign.Length == 0)
        {
            Clog.LogWarning("정렬할 대상 (SpawnPoint) 가 비어있습니다.");
            return;
        }

        Vector3 basePosition = (_originTransform != null) ? _originTransform.position : transform.position;

        for(int i = 0; i < _spawnPointToAlign.Length;i++)
        {
            //계산된 중심 위치로 강제 이동
            if (_spawnPointToAlign[i] != null)
            {
                _spawnPointToAlign[i].position = CalculatePosition(basePosition, i);
            }
        }
        Clog.Log("모든 SpawnPoint 정렬 완료!");
    }
    private Vector3 CalculatePosition(Vector3 basePos, int index)
    {
        float zPos = -(_tileSize.z * index);
        Vector3 offset = new Vector3(0,0, zPos);
        return basePos + _startOffset + offset;
    }
}
