using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseScreen : MonoBehaviour
{
    public GameObject pauseMenu;

    private static bool toggling = false;
    
    public static void SyncPauseInput()
    {
        toggling = true;
    }

    private void Update()
    {
        if (toggling)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            toggling = false;
        }
    }

    public void ResumeButton()
    {
        toggling = true;
    }

    public void TitleButton()
    {
        SceneManager.LoadScene("TitleScreen");
    }
    
    public void QuitButton()
    {
        Application.Quit;
    }
}
