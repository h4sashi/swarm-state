// For different input systems or AI controllers
using UnityEngine;
public interface IInputHandler
{
    Vector2 MovementInput { get; }
    bool DashInput { get; }
    bool IsInputEnabled { get; set; }
}