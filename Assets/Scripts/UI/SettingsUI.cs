using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button retryFromFirstLevelButton;

    [Header("Gameplay")]
    [SerializeField] private PlayerBallTrajectory playerBall;

    private void Awake()
    {
        if (retryFromFirstLevelButton != null)
        {
            retryFromFirstLevelButton.onClick.RemoveAllListeners();
            retryFromFirstLevelButton.onClick.AddListener(RetryFromFirstLevel);
        }

        SetSettingsVisible(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        SetSettingsVisible(!settingsPanel.activeSelf);
    }

    public void ShowSettings()
    {
        SetSettingsVisible(true);
    }

    public void HideSettings()
    {
        SetSettingsVisible(false);
    }

    private void RetryFromFirstLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RetryFromFirstLevel();
        }

        SetSettingsVisible(false);
    }

    private void SetSettingsVisible(bool isVisible)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(isVisible);
        }

        if (playerBall == null)
        {
            playerBall = FindFirstObjectByType<PlayerBallTrajectory>();
        }

        if (playerBall != null)
        {
            playerBall.SetGameplayInputEnabled(!isVisible);
        }
    }
}