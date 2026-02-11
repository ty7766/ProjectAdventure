using UnityEngine;

public class FallingRockTrajectoryCalculator : MonoBehaviour
{
    /// <summary>
    /// 탄퍼짐을 적용한 발사 방향 벡터 계산
    /// </summary>
    public static Vector3 GetRandomLaunchDirection(Vector3 upDirection, float spread)
    {
        Vector3 randomSpread = Random.insideUnitSphere * spread;
        // y축(높이) 영향은 제거하여 수평으로만 퍼지게 하거나, 필요 시 조정
        randomSpread.y = 0;

        return (upDirection + randomSpread).normalized;
    }

    /// <summary>
    /// 물리 공식을 이용한 예상 착탄 지점 계산
    /// (h = v0*t + 0.5*g*t^2 공식을 역이용)
    /// </summary>
    public static Vector3 PredictLandingPosition(Vector3 startPos, Vector3 initialVelocity, float fallHeight)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float v0_y = initialVelocity.y;

        // 근의 공식 등을 응용하여 바닥(-fallHeight)에 도달하는 시간(t) 계산
        // t = (v0 + sqrt(v0^2 + 2gh)) / g
        float timeToImpact = (v0_y + Mathf.Sqrt((v0_y * v0_y) + (2 * gravity * fallHeight))) / gravity;

        // 위치 = 시작점 + (속도 * 시간)
        // 수평 이동은 등속 운동, 수직 이동은 등가속도 운동이나 위 공식으로 퉁쳐서 최종 위치 예측
        Vector3 landingPos = startPos + (initialVelocity * timeToImpact);

        // y값은 강제로 설정한 바닥 높이로 보정
        landingPos.y = startPos.y - fallHeight;

        return landingPos;
    }
}
