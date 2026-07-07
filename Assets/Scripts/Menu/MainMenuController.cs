using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string SceneName;

    public void SwitchGame()
    {
        SceneManager.LoadScene(SceneName);
    }
}