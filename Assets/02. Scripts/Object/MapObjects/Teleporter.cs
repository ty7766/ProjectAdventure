using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("?곌껐 ?ㅼ젙")]
    [Tooltip("?대룞??紐⑹쟻吏 ?붾젅?ы꽣")]
    [SerializeField] private Teleporter _destination;

    [Tooltip("?대룞 ??紐⑹쟻吏?먯꽌 ?ㅼ떆 ?吏 紐삵븯寃?留됰뒗 ?쒓컙 (珥?")]
    [SerializeField] private float _cooldownTime = 1.0f;

    [Header("?꾩튂 蹂댁젙")]
    [Tooltip("?붾젅?ы듃 ??Y異뺤쓣 ?댁쭩 ?꾩썙以꾩? ?щ? (諛붾떏 ?쇱엫 諛⑹?)")]
    [SerializeField] private float _yOffset = 0.3f;

    // ?꾩옱 ?묐룞 媛?ν븳 ?곹깭?몄? ?뺤씤
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
            CustomDebug.LogWarning($"[Teleporter] {name}??紐⑹쟻吏媛 ?ㅼ젙?섏? ?딆븯?듬땲??");
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
    /// 臾댄븳 猷⑦봽 諛⑹? 荑⑦??꾩쓣 ?ㅼ젙?섎뒗 硫붿냼??Begins a cooldown during which this teleporter cannot be used.
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
