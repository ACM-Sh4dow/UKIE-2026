using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public ProtagonistController player;

    private void Start()
    {
        ProtagonistController.Instance = player;
    }
}
