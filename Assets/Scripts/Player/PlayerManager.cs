using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public Vector3[] spawnPositions;
    public GameObject[] playerPrefabs;
    public static List<ProtagonistController> players = new List<ProtagonistController>();

    public static ProtagonistController player;

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            var spawned = Instantiate(playerPrefabs[i], spawnPositions[i], Quaternion.identity, this.transform);
            spawned.name = spawned.name.Substring(0, 8);
            players.Add(spawned.GetComponent<ProtagonistController>());
        }
        player = players[0];
    }

    public static void SyncCameraMove(Vector2 input)
    {
        var currentPlayer = player;

        switch (player.gameObject.name)
        {
            case "Player 1":
                if (input == new Vector2(1,0)) { player = players[1]; }
                else if (input == new Vector2(0,-1)) { player = players[2]; }

                break;
            case "Player 2":
                if (input == new Vector2(-1, 0)) { player = players[0]; }
                else if (input == new Vector2(0, -1)) { player = players[3]; }

                break;
            case "Player 3":
                if (input == new Vector2(1, 0)) { player = players[3]; }
                else if (input == new Vector2(0, 1)) { player = players[0]; }

                break;
            case "Player 4":
                if (input == new Vector2(-1, 0)) { player = players[2]; }
                else if (input == new Vector2(0, 1)) { player = players[1]; }

                break;
        }

        if (player != currentPlayer)
        {
            AkUnitySoundEngine.PostEvent("Switch_Camera", player.gameObject);
        }
    }


    private void Update()
    {
        ProtagonistController.Instance = player;
    }
}
