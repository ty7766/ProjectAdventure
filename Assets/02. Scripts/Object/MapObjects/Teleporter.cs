using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("이동할 목적지 텔레포터")]
    [SerializeField] private Teleporter _destination;

    [Tooltip("이동 후 목적지에서 다시 즉시 이동되지 않도록 막는 시간 (초)")]
    [SerializeField] private float _cooldownTime = 1.0f;

    [Header("위치 보정")]
    [Tooltip("텔레포트 시 Y축을 얼마나 띄워줄지 설정 (바닥 끼임 방지)")]
    [SerializeField] private float _yOffset = 0.3f;

    // 현재 작동 가능한 상태인지 확인
    private bool _isReady = true;

    private void OnTriggerEnter(Collider other)
    {
        if(!CanTeleport(other))
        {
            return;
        }
        TeleportPlayer(other.transform);
    }

    private bool CanTeleport(Collider other)
    {
        if (!_isReady)
        {
            return false;
        }

        if (!other.CompareTag("Player"))
        {
            return false;
        }

        if (_destination == null)
        {
            CustomDebug.LogWarning($"[Teleporter] {name} : 목적지가 설정되지 않았습니다.");
            return false;
        }
        return true;
    }

    private void TeleportPlayer(Transform player)
    {
        Vector3 targetPos = _destination.transform.position;
        targetPos.y += _yOffset; // 바닥에 묻히지 않게 살짝 띄움

        player.position = targetPos;

        // 도착한 곳의 텔레포터가 나를 바로 다시 태우지 않도록 쿨타임 실행
        _destination.ReceivePlayer(_cooldownTime);
    }

    private void ReceivePlayer(float duration)
    {
        StartCoroutine(ApplyCooldown(duration));
    }

    private IEnumerator ApplyCooldown(float duration)
    {
        _isReady = false;
        yield return new WaitForSeconds(duration);
        _isReady = true;
    }
}