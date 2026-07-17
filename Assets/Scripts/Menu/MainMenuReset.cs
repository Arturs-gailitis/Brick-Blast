using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuResetButton : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    private Button resetButton;
    private UIButtonClickSound buttonClickSound;
    private bool isLoadingScene;

    private void Awake()
    {
        resetButton = GetComponent<Button>();
        buttonClickSound = GetComponent<UIButtonClickSound>();

        resetButton.onClick.AddListener(RestartFromFirstLevel);
    }

    private void Start()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.enabled = false;
        }
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

        if (buttonClickSound != null)
        {
            buttonClickSound.OnPointerClick(null);
        }

        resetButton.interactable = false;

        LevelManager.ResetSavedProgress();
        ScoreManager.ResetSavedScore();

        SceneManager.LoadScene(gameSceneName);
    }
}