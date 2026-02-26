using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public string GameSceneName;
    public GameObject Credits;

    public void CreditsButton()
    {
        Credits.SetActive(true);
        gameObject.SetActive(false);
    }

    public void StartButton()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
