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
    [SerializeField]
    private float respawnDelay = 2.0f;
    private Vector3 respawnPoint;
    private bool isAlive = true;
    private bool isMovable = true;


    //--- Unity Methods ---//
    private void Start()
    {
        if(!TryGetComponent<Animator>(out anim))
        {
            Debug.LogWarning("Animator component not found on Player.");
        }

        SetRespawnPoint();
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

    private void FixedUpdate()
    {
        
    }

    //--- Public Methods ---//
    // Apply Damage
    public void TakeDamage(int damage)
    {
        if(!isAlive)
        {
            return;
        }

        properties.Health -= damage;
        StartCoroutine(ApplyDamageFeedback(skinnedMeshRenderer, 0.1f, Color.red));
    }

    //Update checkpoint
    public void SetRespawnPoint(Vector3 newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }
    public void SetRespawnPoint()
    {
        respawnPoint = this.transform.position;
    }

    //--- Private Methods ---//
    private void Dead()
    {
        // Handle player death logic
        isAlive = false;
        movement.ResetSpeed();
        anim.SetTrigger("Dead");
    }

    private void Respawn()
    {
        TakeDamage(1);
        if (isAlive) anim.SetTrigger("GetUp");
        movement.TeleportTo(respawnPoint);
        

        //Disable Player Movement for a short duration
        isMovable = false;
        StartCoroutine(EnableMovementAfterDelay(respawnDelay));
    }

    private void HandleInputs()
    {
        if (!isMovable)
        {
            return;
        }

        Vector3 inputDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, -Input.GetAxis("Vertical"));
        if(inputDirection != Vector3.zero)
        {
            movement.Move(inputDirection, properties.Speed, properties.TurnSpeed);
        }
    }


    //--- Coroutines ---//
    private IEnumerator ApplyDamageFeedback(SkinnedMeshRenderer renderer, float duration, Color flashColor)
    {
        Color originalColor = renderer.material.color;
        renderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        renderer.material.color = originalColor;
    }

    private IEnumerator EnableMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isMovable = true;
    }

}
