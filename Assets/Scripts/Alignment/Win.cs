using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Win : MonoBehaviour
{
    public static PerspectivePuzzleSolve[] completions = new PerspectivePuzzleSolve[4];
    private static int index = 0;

    public static void RegisterCompletion(PerspectivePuzzleSolve solved)
    {
        if (solved == null && !completions.Contains(solved))
        {
            completions[index] = solved;
        }

        index = index + 1;

        if (index >= completions.Length)
        {
            // WIN SOUND GO BRRRRRRR

            SceneManager.LoadScene("WinScreen");
            SceneManager.UnloadSceneAsync("MainLevel");
            index = 0;
        }
    }
}
