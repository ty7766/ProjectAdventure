using UnityEngine;

//각 맵의 범위를 보여주는 가상 가이드라인
public class MapGuideLine : MonoBehaviour
{
    [Header("기준점 설정")]
    public Transform originTransform;   //시작 기준점

    [Header("정렬할 SpawnPoint")]
    public Transform[] spawnPointToAlign;

    [Header("맵 설정")]
    public Vector3 tileSize = new Vector3(15, 1, 10);
    public Vector3 startOffset = Vector3.zero;  //시작위치 보정

    [Header("가이드라인 색상")]
    public Color gizmoColor = Color.yellow;

    //Scene 뷰에서 게임 미 실행시 그림을 그리는 메소드
    private void OnDrawGizmos()
    {
        //컬러 세팅
        Gizmos.color = gizmoColor;

        //기준점 설정 (설정되지 않으면 해당 오브젝트 기준으로 정렬)
        Vector3 basePoisition = (originTransform != null) ? originTransform.position : transform.position;

        int count = (spawnPointToAlign != null && spawnPointToAlign.Length > 0) ? spawnPointToAlign.Length : 5;
        for(int i = 0; i < count; i++)
        {
            //현재는 -z 방향으로 맵이 이어지므로 -z 축으로 설정
            Vector3 pos = CalculatePosition(basePoisition, i);
            //박스 및 중심점 그리기
            Gizmos.DrawWireCube(pos, tileSize);
            Gizmos.DrawSphere(pos, 0.3f);
        }
    }

    //SpawnPoint 자동 정렬
    [ContextMenu("위치 자동 정렬")]
    public void AlignPositions()
    {
        if(spawnPointToAlign == null || spawnPointToAlign.Length == 0)
        {
            Debug.LogWarning("정렬할 대상 (SpawnPoint) 가 비어있습니다.");
            return;
        }

        Vector3 basePosition = (originTransform != null) ? originTransform.position : transform.position;

        for(int i = 0; i < spawnPointToAlign.Length;i++)
        {
            //계산된 중심 위치로 강제 이동
            if (spawnPointToAlign[i] != null)
                spawnPointToAlign[i].position = CalculatePosition(basePosition, i);
        }
        Debug.Log("모든 SpawnPoint 정렬 완료!");
    }
    private Vector3 CalculatePosition(Vector3 basePos, int index)
    {
        float zPos = -(tileSize.z * index);
        Vector3 offset = new Vector3(0,0, zPos);
        return basePos + startOffset + offset;
    }
}
