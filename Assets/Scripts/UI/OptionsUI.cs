using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject optionsPanel;

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

        SetOptionsVisible(false);
    }

    public void ToggleOptions()
    {
        if (optionsPanel == null)
        {
            return;
        }

        SetOptionsVisible(!optionsPanel.activeSelf);
    }

    public void HideOptions()
    {
        SetOptionsVisible(false);
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

        SetOptionsVisible(false);
    }

    private void SetOptionsVisible(bool isVisible)
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(isVisible);
        }

        if (playerBall == null || !playerBall.gameObject.scene.IsValid())
        {
            playerBall = FindFirstObjectByType<PlayerBallTrajectory>();
        }

        if (playerBall != null)
        {
            playerBall.SetGameplayInputEnabled(!isVisible);
        }
    }
}