using UnityEngine;
using UnityEngine.InputSystem;

public interface IMovementController
{
    bool CanMove { get; set; }
    void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse);
    Vector3 GetMovementDirection();
    Vector3 GetVelocity();
    void SetVelocity(Vector3 velocity); 
}