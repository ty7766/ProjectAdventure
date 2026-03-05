using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("이동할 목적지 텔레포터")]
    [SerializeField]
    private Teleporter _destination;

    [Tooltip("이동 후 목적지에서 다시 즉시 이동되지 않도록 막는 시간 (초)")]
    [SerializeField] 
    private float _cooldownTime = 1.0f;

    [Tooltip("단방향 설정")]
    [SerializeField]
    private bool _isOneWay;

    [Header("위치 보정")]
    [Tooltip("텔레포트 시 Y축을 얼마나 띄워줄지 설정 (바닥 끼임 방지)")]
    [SerializeField] private float _yOffset = 0.3f;

    // 현재 작동 가능한 상태인지 확인
    private bool _isReady = true;

    private Coroutine _cooldownCoroutine;
    private WaitForSeconds _waitCooldown;

    private void Awake()
    {
        _waitCooldown = new WaitForSeconds(_cooldownTime);
    }

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
            if(!_isOneWay)
            {
                CustomDebug.LogWarning($"[Teleporter] {name} : 목적지가 설정되지 않았습니다.");
            }
            return false;
        }

        return true;
    }

    private void TeleportPlayer(Transform player)
    {
        Vector3 targetPosition = _destination.transform.position;
        targetPosition.y += _yOffset; // 바닥에 묻히지 않게 살짝 띄움

        SoundManager.Instance.PlaySFX_3D(SoundType.SFX_Teleport, transform.position);

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if(playerMovement != null)
        {
            playerMovement.TeleportTo(targetPosition);
        }

        _destination.ReceivePlayer();
    }

    private void ReceivePlayer()
    {
        if(_cooldownCoroutine != null)
        {
            StopCoroutine(_cooldownCoroutine);
        }
        _cooldownCoroutine = StartCoroutine(ApplyCooldown());
    }

    private IEnumerator ApplyCooldown()
    {
        _isReady = false;
        yield return _waitCooldown;
        _isReady = true;
        _cooldownCoroutine = null;
    }
}