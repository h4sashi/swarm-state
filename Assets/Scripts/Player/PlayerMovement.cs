using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour, IMovementController, IConfigurable<PlayerConfig>
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private Rigidbody rb;
    
    private Vector2 inputVector;
    private Vector3 currentVelocity;
    private bool canMove = true;
    
    // Interface Properties
    public bool CanMove 
    { 
        get => canMove; 
        set => canMove = value; 
    }
    
    public PlayerConfig Config 
    { 
        get => config; 
        set => config = value; 
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        
        // CRITICAL: Proper constraints for top-down movement
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        
        ApplyConfig();
    }

    void FixedUpdate()
    {
        // ONLY handle movement if allowed
        if (canMove)
        {
            HandleMovement();
        }
        // If can't move, don't interfere with external velocity changes (like dash)
    }

    public void OnMove(InputValue value)
    {
        inputVector = value.Get<Vector2>();
    }

    // Interface Implementation
    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        rb.AddForce(force, mode);
    }
    
    public void SetVelocity(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    public Vector3 GetMovementDirection()
    {
        return new Vector3(inputVector.x, 0, inputVector.y).normalized;
    }

    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    public void ApplyConfig()
    {
        if (config == null) return;
    }

    private void HandleMovement()
    {
        // Convert 2D input to 3D world space (top-down)
        Vector3 targetDirection = new Vector3(inputVector.x, 0, inputVector.y).normalized;
        Vector3 targetVelocity = targetDirection * config.moveSpeed;
        
        // Smooth acceleration/deceleration
        float lerpRate = targetDirection.magnitude > 0.1f ? config.acceleration : config.deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpRate * Time.fixedDeltaTime);
        
        // Apply movement while preserving Y velocity
        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
        
        // Rotate player to face movement direction
        if (targetDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }
}



// public class PlayerMovement : MonoBehaviour, IMovementController, IConfigurable<PlayerConfig>
// {
//     [SerializeField] private PlayerConfig config;
//     [SerializeField] private Rigidbody rb;
    
//     private Vector2 inputVector;
//     private Vector3 currentVelocity;
//     private bool canMove = true;

//     // Interface Properties
//     public bool CanMove 
//     { 
//         get => canMove; 
//         set => canMove = value; 
//     }
    
//     public PlayerConfig Config 
//     { 
//         get => config; 
//         set => config = value; 
//     }

//     void Awake()
//     {
//         if (!rb) rb = GetComponent<Rigidbody>();
//         rb.freezeRotation = true;
//         rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
//         ApplyConfig();
//     }

//     void FixedUpdate()
//     {
//         HandleMovement();
//     }

//     public void OnMove(InputValue value)
//     {
//         inputVector = value.Get<Vector2>();
//     }

//     // Interface Implementation
//     public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
//     {
//         rb.AddForce(force, mode);
//     }

//     public Vector3 GetMovementDirection()
//     {
//         return new Vector3(inputVector.x, 0, inputVector.y).normalized;
//     }

//     public Vector3 GetVelocity()
//     {
//         return currentVelocity;
//     }

//     public void ApplyConfig()
//     {
//         if (config == null) return;
//         // Apply any config-specific setup
//     }

//     private void HandleMovement()
//     {
//         if (!canMove)
//         {
//             currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, config.deceleration * Time.fixedDeltaTime);
//             rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
//             return;
//         }

//         Vector3 targetDirection = new Vector3(inputVector.x, 0, inputVector.y).normalized;
//         Vector3 targetVelocity = targetDirection * config.moveSpeed;
        
//         float lerpRate = targetDirection.magnitude > 0.1f ? config.acceleration : config.deceleration;
//         currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpRate * Time.fixedDeltaTime);
        
//         rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

//         if (targetDirection.magnitude > 0.1f)
//         {
//             Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
//             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
//         }
//     }
// }
