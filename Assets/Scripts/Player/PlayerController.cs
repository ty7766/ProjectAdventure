using UnityEngine;
using UnityEngine.InputSystem.Processors;

public class PlayerController : MonoBehaviour
{
    //--- Fields ---//
    [SerializeField]
    private PlayerMovement movement;
    [SerializeField]
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

        if(movement != null)
        {
            HandleInputs();
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

    private void HandleInputs()
    {
        Vector3 inputDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, -Input.GetAxis("Vertical"));
        if(inputDirection != Vector3.zero)
        {
            movement.Move(inputDirection, properties.Speed, properties.TurnSpeed);
        }
    }
}
