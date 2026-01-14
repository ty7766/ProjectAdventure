using UnityEngine;
using System.Diagnostics;

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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawMapGuideLinesWithGizmos();
    }
#endif

    /// <summary>
    /// SpawnPoint 배열에 등록된 오브젝트들을 설정된 타일 크기에 맞춰 일렬로 자동 정렬
    /// </summary>
    [ContextMenu("위치 자동 정렬")]
    public void AlignPositions()
    {
        if(_spawnPointToAlign == null || _spawnPointToAlign.Length == 0)
        {
            CustomDebug.LogWarning("정렬할 대상 (SpawnPoint) 가 비어있습니다.");
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
        CustomDebug.Log("모든 SpawnPoint 정렬 완료!");
    }

    [Conditional("UNITY_EDITOR")]
    private void DrawMapGuideLinesWithGizmos()
    {
        Gizmos.color = _gizmoColor;

        //기준점 설정 (설정되지 않으면 해당 오브젝트 기준으로 정렬)
        Vector3 basePosition = (_originTransform != null) ? _originTransform.position : transform.position;

        int count = (_spawnPointToAlign != null && _spawnPointToAlign.Length > 0) ? _spawnPointToAlign.Length : 5;
        for (int i = 0; i < count; i++)
        {
            //현재는 -z 방향으로 맵이 이어지므로 -z 축으로 설정
            Vector3 position = CalculatePosition(basePosition, i);
            Gizmos.DrawWireCube(position, _tileSize);
            Gizmos.DrawSphere(position, 0.3f);
        }
    }

    private Vector3 CalculatePosition(Vector3 basePosition, int index)
    {
        float zPosition = -(_tileSize.z * index);
        Vector3 offset = new Vector3(0,0, zPosition);
        return basePosition + _startOffset + offset;
    }
}
