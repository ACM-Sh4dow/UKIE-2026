using System.Collections;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public string GameSceneName;
    public GameObject Credits;

    public GameObject fade;

    public void CreditsButton()
    {
        Credits.SetActive(true);
        gameObject.SetActive(false);
    }

    public void StartButton()
    {
        StartCoroutine(FadeToBlackAndLoadScene());
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    private IEnumerator FadeToBlackAndLoadScene()
    {
        AkUnitySoundEngine.PostEvent("Music_Title_Stop", gameObject);
        fade.SetActive(true);
        Image fadeImage = fade.GetComponent<Image>();
        
        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha += 0.01f;

            fadeImage.color = new Color(0,0,0, alpha);

            yield return new WaitForSeconds(0.015f);
        }

        SceneManager.LoadScene(GameSceneName);
    }
}
