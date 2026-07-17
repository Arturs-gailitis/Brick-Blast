using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuResetButton : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    private Button resetButton;
    private bool isLoadingScene;

    private void Awake()
    {
        resetButton = GetComponent<Button>();
        resetButton.onClick.AddListener(RestartFromFirstLevel);
    }

    private void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(RestartFromFirstLevel);
        }
    }

    public void RestartFromFirstLevel()
    {
        if (isLoadingScene || string.IsNullOrWhiteSpace(gameSceneName))
        {
            return;
        }

        isLoadingScene = true;
        resetButton.interactable = false;

        LevelManager.ResetSavedProgress();
        ScoreManager.ResetSavedScore();

        SceneManager.LoadScene(gameSceneName);
    }
}