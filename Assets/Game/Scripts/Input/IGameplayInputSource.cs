using UnityEngine;

namespace PiGame.Input
{
    public interface IGameplayInputSource
    {
        Vector2 Move { get; }
        Vector2 AimDirection { get; }
        Vector2 PointerPosition { get; }
        bool IsAimFirePressed { get; }
        bool IsCrouchPressed { get; }
        bool WasJumpPressedThisFrame();
        bool WasCancelAimPressedThisFrame();
        bool WasAbilityPressedThisFrame();
    }
}
