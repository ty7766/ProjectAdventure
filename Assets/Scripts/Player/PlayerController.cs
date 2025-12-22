using UnityEngine;
using UnityEngine.InputSystem.Processors;

public class PlayerController : MonoBehaviour
{
    //--- Fields ---//
    [SerializeField]
    private PlayerMovement movement;
    private PlayerProperties properties;
    private bool isAlive = true;

    //--- Unity Methods ---//
    private void Start()
    {
        
    }

    private void Update()
    {
        if(!isAlive)
        {
            return;
        }

        if(properties.Health <= 0)
        {
            Dead();
        }
    }

    private void FixedUpdate()
    {
        
    }

    //--- Methods ---//
    private void Dead()
    {
        // Handle player death logic
        isAlive = false;
    }

}
