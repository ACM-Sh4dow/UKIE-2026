using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ProtagonistController>() == null) return;

        var collidingPlayer = other.GetComponent<ProtagonistController>();
        int index = 0; 

        switch (collidingPlayer.name[-1])
        {
            case '1':
                index = 0; break;
            case '2':
                index = 1; break;
            case '3':
                index = 2; break;
            case '4':
                index = 3; break;
        }

        collidingPlayer.transform.position = PlayerManager.Instance.spawnPositions[index];
    }
}
