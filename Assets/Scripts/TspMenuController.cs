using UnityEngine;
using UnityEngine.SceneManagement;

public class TspMenuController : MonoBehaviour
{
    [SerializeField] private GameObject howToPlayPanel;

    public void PlayGame()
    {
        SceneManager.LoadScene("TspGameScene");
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }
}