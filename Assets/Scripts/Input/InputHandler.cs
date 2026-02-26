using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public void ReceiveMovementInput(InputAction.CallbackContext input)
    {
        ProtagonistController.Instance.SyncMovementInput(input);
    }

    public void RecieveLookInput(InputAction.CallbackContext input)
    {
        ProtagonistController.Instance.SyncLookInput(input.ReadValue<Vector2>());
    }

    public void RecieveCameraChangeInput(InputAction.CallbackContext input)
    {
        if (!input.started) return;

        PlayerManager.SyncCameraMove(input.ReadValue<Vector2>());
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