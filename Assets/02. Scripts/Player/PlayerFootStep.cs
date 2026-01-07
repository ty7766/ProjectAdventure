using UnityEngine;

public class PlayerFootstep : MonoBehaviour
{
    [Header("발 뼈대 연결 (Hierarchy에서 드래그)")]
    [SerializeField]
    private Transform _leftFoot;  //Toe.Ctrl.L
    [SerializeField]
    private Transform _rightFoot; //Toe.Ctrl.R

    [Header("설정")]
    [SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private float _rayDistance = 0.3f;


    /// <summary>
    /// 애니메이션 이벤트에서 호출할 함수 (0: 왼쪽, 1: 오른쪽)
    /// </summary>
    /// <param name="isLeft">왼쪽발 오른쪽발 구분하기 위한 변수</param>
    public void OnFootstep(int isLeft)
    {
        Transform foot = (isLeft == 0) ? _leftFoot : _rightFoot;
        if (foot == null)
        {
            return;
        }

        if (Physics.Raycast(foot.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, _rayDistance, _groundLayer))
        {
            if (System.Enum.TryParse(hit.collider.tag, out VFXType groundType))
            {
                //눈 이펙트 발생 실행
                if(VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayVFX(groundType, hit.point + Vector3.up * 0.02f);
                }
            }
        }
    }
}