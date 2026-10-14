using System;
using UnityEngine;

/// <summary>
/// 튜토리얼 전용 이동 입력 감지기.
/// 4방향 중 어떤 방향이 '새로 눌렸는지'를 감지해 이벤트로 통보한다.
/// 같은 섹션 안에서 여러 방향 목표가 순서 없이 완료될 수 있어야 하므로
/// 방향별로 독립적으로, 그리고 반복적으로(다시 누르면 다시) 이벤트를 발생시킨다.
/// </summary>
public class TutorialInputNode : IDisposable
{
    //--- Events ---//
    public event Action<TutorialMoveDirection> OnMoveInDirection;

    //--- Fields ---//
    private readonly GameControls _controls;
    private Vector2 _prevMove;
    private bool _enabled;

    //--- Properties ---//
    /// <summary>입력 감지 활성화 여부</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            _prevMove = Vector2.zero;
        }
    }

    public TutorialInputNode(GameControls controls)
    {
        _controls = controls;
    }

    //--- Public Methods ---//
    /// <summary>방향 입력 변화를 감지합니다. (TutorialManager.Update에서 호출)</summary>
    public void Tick()
    {
        if (!_enabled || _controls == null)
        {
            return;
        }

        Vector2 move = _controls.Player.Move.ReadValue<Vector2>();
        if (move == _prevMove)
        {
            return;
        }

        // 새로 눌린 방향만 통보 (이전 프레임에는 눌리지 않았던 방향)
        if (move.y > 0.5f && _prevMove.y <= 0.5f)
        {
            OnMoveInDirection?.Invoke(TutorialMoveDirection.Up);
        }
        if (move.y < -0.5f && _prevMove.y >= -0.5f)
        {
            OnMoveInDirection?.Invoke(TutorialMoveDirection.Down);
        }
        if (move.x < -0.5f && _prevMove.x >= -0.5f)
        {
            OnMoveInDirection?.Invoke(TutorialMoveDirection.Left);
        }
        if (move.x > 0.5f && _prevMove.x <= 0.5f)
        {
            OnMoveInDirection?.Invoke(TutorialMoveDirection.Right);
        }

        _prevMove = move;
    }

    public void Dispose()
    {
        _enabled = false;
        OnMoveInDirection = null;
    }
}
