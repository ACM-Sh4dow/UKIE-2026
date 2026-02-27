using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Restart : MonoBehaviour
{
    public static void Retry()
    {
        if (SceneManager.GetActiveScene().name != "Main Level") return;

        for (int i = 0; i < 4; i++)
        {
            PlayerManager.players[i].transform.position = PlayerManager.Instance.spawnPositions[i];
            PlayerManager.players[i].transform.rotation = Quaternion.Euler(PlayerManager.Instance.spawnRotations[i]);
        }
    }
}
