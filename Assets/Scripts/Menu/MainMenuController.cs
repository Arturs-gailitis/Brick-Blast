using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string SceneName;

    [Header("Progress")]
    [SerializeField] private bool resetProgressAfterGameOver;
    [SerializeField] private bool saveGameBeforeChangingScene;

    [Header("Sound")]
    [SerializeField] private UIButtonClickSound buttonClickSound;

    private Button button;
    private bool isLoading;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonClickSound == null)
        {
            buttonClickSound = GetComponent<UIButtonClickSound>();
        }
    }

    private void Start()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.enabled = false;
        }
    }

    public void SwitchGame()
    {
        if (isLoading || string.IsNullOrWhiteSpace(SceneName))
        {
            return;
        }

        isLoading = true;

        if (buttonClickSound != null)
        {
            buttonClickSound.OnPointerClick(null);
        }

        if (button != null)
        {
            button.interactable = false;
        }

        bool shouldResetProgress =
            resetProgressAfterGameOver &&
            LevelManager.Instance != null &&
            LevelManager.Instance.IsGameOver;

        if (shouldResetProgress)
        {
            LevelManager.ResetSavedProgress();
            ScoreManager.ResetSavedScore();
        }
        else
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SaveLevelStartBallAttackStrength();
            }

            if (saveGameBeforeChangingScene &&
                LevelManager.Instance != null)
            {
                LevelManager.Instance.SaveCurrentGame();
            }
        }

        SceneManager.LoadScene(SceneName);
    }
}