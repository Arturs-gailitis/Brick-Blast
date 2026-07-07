using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Gameplay")]
    [SerializeField] private PlayerBallTrajectory playerBall;

    private void Awake()
    {
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