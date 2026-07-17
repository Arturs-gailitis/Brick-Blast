using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private SettingsUI settingsUI;

    [Header("Buttons")]
    [SerializeField] private Button retryFromFirstLevelButton;
    [SerializeField] private Button openSettingsButton;

    [Header("Gameplay")]
    [SerializeField] private PlayerBallTrajectory playerBall;

    private void Awake()
    {
        if (retryFromFirstLevelButton != null)
        {
            retryFromFirstLevelButton.onClick.RemoveAllListeners();
            retryFromFirstLevelButton.onClick.AddListener(RetryFromFirstLevel);
        }

        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveAllListeners();
            openSettingsButton.onClick.AddListener(OpenSettings);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (settingsUI != null)
        {
            settingsUI.HideSettings();
        }

        UpdateGameplayInput();
    }

    public void ToggleOptions()
    {
        bool optionsAreVisible = optionsPanel != null && optionsPanel.activeSelf;

        bool settingsAreVisible = settingsUI != null && settingsUI.IsVisible;

        if (optionsAreVisible || settingsAreVisible)
        {
            HideAllPanels();
        }
        else
        {
            ShowOptions();
        }
    }

    public void ShowOptions()
    {
        if (settingsUI != null)
        {
            settingsUI.HideSettings();
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }

        UpdateGameplayInput();
    }

    public void OpenSettings()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (settingsUI != null)
        {
            settingsUI.ShowSettings();
        }

        UpdateGameplayInput();
    }

    public void HideAllPanels()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (settingsUI != null)
        {
            settingsUI.HideSettings();
        }

        UpdateGameplayInput();
    }

    private void RetryFromFirstLevel()
    {
        PlayerBallTrajectory runtimeBall = FindFirstObjectByType<PlayerBallTrajectory>();

        if (runtimeBall != null)
        {
            playerBall = runtimeBall;
            playerBall.ResetBallsForRetry();
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RetryFromFirstLevel();
        }

        HideAllPanels();
    }

    private void UpdateGameplayInput()
    {
        bool menuIsVisible = (optionsPanel != null && optionsPanel.activeSelf) ||
            (settingsUI != null && settingsUI.IsVisible);

        if (playerBall == null || !playerBall.gameObject.scene.IsValid())
        {
            playerBall = FindFirstObjectByType<PlayerBallTrajectory>();
        }

        if (playerBall != null)
        {
            playerBall.SetGameplayInputEnabled(!menuIsVisible);
        }
    }
}