using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public void ReceiveMovementInput(InputAction.CallbackContext input)
    {
        ProtagonistController.SyncMovementInput(input);
    }

    public void RecieveLookInput(InputAction.CallbackContext input)
    {
        ProtagonistController.SyncLookInput(input.ReadValue<Vector2>());
    }

    //public void RecieveInteractInput(InputAction.CallbackContext input)
    //{
    //    if (!input.started) return;

    //    ProtagonistController.Interact();
    //    if (ProtagonistController.Instance.perspectivePuzzle != null)
    //    {
    //        ProtagonistController.Instance.perspectivePuzzle.SolvePuzzle();
    //    }
    //}
}