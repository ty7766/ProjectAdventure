using UnityEngine;

public class PlayerProperties : MonoBehaviour
{
    [SerializeField]
    private float _speed = 5.0f;
    [SerializeField]
    private int _health = 3;
    [SerializeField]
    private float _stamina = 100.0f;
    [SerializeField]
    private float _turnSpeed = 15f;

    //--- Properties ---//
    public int Health
    {
        get { return _health; }
        set { _health = Mathf.Max(0, value); } // Ensure health doesn't go below 0
    }

    public float Speed
    {
        get { return _speed; }
        set { _speed = Mathf.Max(0.0f, value); } // Ensure speed is non-negative
    }

    public float Stamina
    {
        get { return _stamina; }
        set { _stamina = Mathf.Clamp(value, 0.0f, 100.0f); } // Clamp stamina between 0 and 100
    }

    public float TurnSpeed
    {
        get { return _turnSpeed; }
        set { _turnSpeed = Mathf.Max(0.0f, value); } // Ensure turn speed is non-negative
    }
}
