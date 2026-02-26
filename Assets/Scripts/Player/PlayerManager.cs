using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public Vector3[] spawnPositions;
    public GameObject[] playerPrefabs;

    public static ProtagonistController player;

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            var spawned = Instantiate(playerPrefabs[i], spawnPositions[i], Quaternion.identity, this.transform);

            if (i == 0)
            {
                player = spawned.GetComponent<ProtagonistController>();
            }
        }
    }

    public static void SyncCameraMove(Vector2 input)
    {
        Debug.Log("Called");
        Debug.Log($"camera move input is {input.x}, {input.y}");

        switch (player.gameObject.name)
        {

        }
    }


    private void Update()
    {
        ProtagonistController.Instance = player;
    }
}
