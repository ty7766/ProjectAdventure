using UnityEngine;

//각 맵의 범위를 보여주는 가상 가이드라인
public class MapGuideLine : MonoBehaviour
{
    [Header("맵 설정")]
    public int slotCount = 2;
    public Vector3 tileSize = new Vector3(10, 1, 20);
    public Vector3 startOffset = Vector3.zero;  //시작위치 보정

    [Header("가이드라인 색상")]
    public Color gizmoColor = Color.yellow;

    //Scene 뷰에서 게임 미 실행시 그림을 그리는 메소드
    private void OnDrawGizmos()
    {
        //컬러 세팅
        Gizmos.color = gizmoColor;

        for(int i = 0; i < slotCount; i++)
        {
            //현재는 -z 방향으로 맵이 이어지므로 -z 축으로 설정
            float zPos = -(tileSize.z * i);
            Vector3 offset = new Vector3(0,0,zPos);
            Vector3 centerPos = transform.position + startOffset + offset;

            //박스 및 중심점 그리기
            Gizmos.DrawWireCube(centerPos, tileSize);
            Gizmos.DrawSphere(centerPos, 0.5f);
        }
    }
}
