using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("이동할 목적지 텔레포터")]
    [SerializeField] private Teleporter _destination;

    [Tooltip("이동 후 목적지에서 다시 타지 못하게 막는 시간 (초)")]
    [SerializeField] private float _cooldownTime = 1.0f;

    [Header("위치 보정")]
    [Tooltip("텔레포트 시 Y축을 살짝 띄워줄지 여부 (바닥 끼임 방지)")]
    [SerializeField] private float _yOffset = 0.3f;

    // 현재 작동 가능한 상태인지 확인
    private bool _isReady = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!_isReady || !other.CompareTag("Player"))
        {
            return;
        }

        if (_destination == null)
        {
            CustomDebug.LogWarning($"[Teleporter] {name}의 목적지가 설정되지 않았습니다!");
            return;
        }

        TeleportPlayer(other.transform);
    }

    private void TeleportPlayer(Transform player)
    {
        Vector3 targetPos = _destination.transform.position;
        targetPos.y += _yOffset;

        player.position = targetPos;

        _destination.ReceivePlayer(_cooldownTime);
    }

    /// <summary>
    /// 무한 루프 방지 쿨타임을 설정하는 메소드
    /// </summary>
    /// <param name="duration">쿨타임</param>
    public void ReceivePlayer(float duration)
    {
        StartCoroutine(CooldownRoutine(duration));
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        _isReady = false;
        yield return new WaitForSeconds(duration);
        _isReady = true;
    }
}