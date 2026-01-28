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

    /// <summary>
    /// Handles trigger entry to teleport the player to the linked destination when the teleporter is ready.
    /// </summary>
    /// <param name="other">The collider that entered the trigger; expected to belong to the player.</param>
    /// <remarks>
    /// If the teleporter is not ready or the collider is not the player, the method does nothing.
    /// If no destination is configured, a warning is logged and teleportation is skipped.
    /// </remarks>
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

    /// <summary>
    /// Moves the given player's transform to the linked destination's position plus the configured Y offset and instructs the destination to start its cooldown.
    /// </summary>
    /// <param name="player">The player's Transform to reposition.</param>
    private void TeleportPlayer(Transform player)
    {
        Vector3 targetPos = _destination.transform.position;
        targetPos.y += _yOffset;

        player.position = targetPos;

        _destination.ReceivePlayer(_cooldownTime);
    }

    /// <summary>
    /// 무한 루프 방지 쿨타임을 설정하는 메소드 Begins a cooldown during which this teleporter cannot be used.
    /// </summary>
    /// <param name="duration">Cooldown duration in seconds during which the teleporter is disabled.</param>
    public void ReceivePlayer(float duration)
    {
        StartCoroutine(CooldownRoutine(duration));
    }

    /// <summary>
    /// Disables the teleporter for the given duration and then restores its readiness.
    /// </summary>
    /// <param name="duration">Time in seconds to keep the teleporter disabled.</param>
    /// <returns>An IEnumerator that performs the cooldown when run as a coroutine.</returns>
    private IEnumerator CooldownRoutine(float duration)
    {
        _isReady = false;
        yield return new WaitForSeconds(duration);
        _isReady = true;
    }
}
