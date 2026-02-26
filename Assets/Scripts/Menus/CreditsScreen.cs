using UnityEngine;

public class CreditsScreen : MonoBehaviour
{
    public GameObject Title;

    public void BackButton()
    {
        Title.SetActive(true);
        gameObject.SetActive(false);
    }
}
