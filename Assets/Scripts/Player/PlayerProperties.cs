using UnityEngine;

public class PlayerProperties : MonoBehaviour
{
    [SerializeField]
    private float speed = 5.0f;
    [SerializeField]
    private int health = 3;
    [SerializeField]
    private float stamina = 100.0f;

    //--- Properties ---//
    public int Health
    {
        get { return health; }
        set { health = Mathf.Max(0, value); } // Ensure health doesn't go below 0
    }

    public float Speed
    {
        get { return speed; }
        set { speed = Mathf.Max(0.0f, value); } // Ensure speed is non-negative
    }

    public float Stamina
    {
        get { return stamina; }
        set { stamina = Mathf.Clamp(value, 0.0f, 100.0f); } // Clamp stamina between 0 and 100
    }
}
