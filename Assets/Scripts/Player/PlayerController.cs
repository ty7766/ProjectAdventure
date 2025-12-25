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
    [SerializeField] [Header("리스폰후 경직시간")]
    private float respawnDelay = 2.0f;
    [SerializeField] [Header("피격 후 무적시간")]
    private float invincibleTime = 3.0f;
    private float invTimer;
    [SerializeField] [Header("피격 후 경직시간")]
    private float stunTime = 0.39f;
    private WaitForSeconds stunDelay;

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
        stunDelay = new WaitForSeconds(stunTime);
        invTimer = invincibleTime;
    }

    private void Update()
    {
        if(!isMovable)
        {
            movement.Move(Vector3.zero, properties.Speed, properties.TurnSpeed);
        }

        if(!isAlive)
        {
            return;
        }

        if (this.transform.position.y < -10f)
        {
            Respawn();
        }

        if(movement != null)
        {
            HandleInputs();
        }

        UpdateTimer();

    }

    //--- Public Methods ---//
    // Apply Damage
    public void TakeDamage(int damage)
    {
        if(!isAlive || invTimer < invincibleTime)
        {
            return;
        }

        properties.Health -= damage;

        invTimer = 0f;

        if (properties.Health <= 0)
        {
            Dead();
        }else
        {
            anim.SetTrigger("Damage");
            isMovable = false;
            StartCoroutine(EnableMovementAfterDelay(stunDelay));
        }
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
        isMovable = false;
        movement.ResetSpeed();
        anim.SetTrigger("Dead");
    }

    //reset player position to respawnpoint with damage penalty
    private void Respawn()
    {
        isMovable = false;
        movement.TeleportTo(respawnPoint);
        TakeDamage(1);
        if (isAlive) anim.SetTrigger("GetUp");
        

        //Disable Player Movement for a short duration
        StartCoroutine(EnableMovementAfterDelay(respawnDelay));
    }

    private void HandleInputs()
    {
        if (!isMovable)
        {
            return;
        }

        Vector3 inputDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, -Input.GetAxis("Vertical"));
        movement.Move(inputDirection, properties.Speed, properties.TurnSpeed);

        //For Debug : Simulate Damage Scenario
        if(Input.GetKeyDown(KeyCode.Space)){
            Debug.Log("Damage Taken Simulated");
            TakeDamage(1);
        }
    }

    private void UpdateTimer(){
        invTimer += Time.deltaTime;
    }


    //--- Coroutines ---//
    private IEnumerator EnableMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isMovable = true;
    }

    private IEnumerator EnableMovementAfterDelay(WaitForSeconds wfs)
    {
        yield return wfs;
        isMovable = true;
    }

}
