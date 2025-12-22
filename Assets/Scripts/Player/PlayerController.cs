using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

public class PlayerController : MonoBehaviour
{
    //--- Componenents ---//
    [SerializeField]
    private PlayerMovement movement;
    [SerializeField]
    private PlayerProperties properties;
    [SerializeField]
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Animator anim;

    //--- Fields ---//
    private Vector3 respawnPoint;
    private bool isAlive = true;
    
    //--- Unity Methods ---//
    private void Start()
    {
        if(!TryGetComponent<Animator>(out anim))
        {
            Debug.LogWarning("Animator component not found on Player.");
        }

        respawnPoint = transform.position;
    }

    private void Update()
    {
        if(!isAlive)
        {
            return;
        }

        if (this.transform.position.y < -10f)
        {
            Respawn();
        }

        if (properties.Health <= 0)
        {
            Dead();
            return;
        }

        if(movement != null)
        {
            HandleInputs();
        }



    }

    private void Respawn()
    {
        TakeDamage(1);
        movement.ResetSpeed();
        if(isAlive)anim.SetTrigger("GetUp");
        this.transform.position = respawnPoint;
    }

    private void FixedUpdate()
    {
        
    }

    //--- Public Methods ---//
    public void TakeDamage(int damage)
    {
        if(!isAlive)
        {
            return;
        }

        properties.Health -= damage;
        StartCoroutine(ApplyDamageFeedback(skinnedMeshRenderer, 0.1f, Color.red));
        if (properties.Health <= 0)
        {
            Dead();
        }
    }

    //--- Private Methods ---//
    private void Dead()
    {
        // Handle player death logic
        isAlive = false;
        movement.ResetSpeed();
        anim.SetTrigger("Dead");
    }

    private void HandleInputs()
    {
        Vector3 inputDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, -Input.GetAxis("Vertical"));
        if(inputDirection != Vector3.zero)
        {
            movement.Move(inputDirection, properties.Speed, properties.TurnSpeed);
        }
    }

    private IEnumerator ApplyDamageFeedback(SkinnedMeshRenderer renderer, float duration, Color flashColor)
    {
        Color originalColor = renderer.material.color;
        renderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        renderer.material.color = originalColor;
    }

}
