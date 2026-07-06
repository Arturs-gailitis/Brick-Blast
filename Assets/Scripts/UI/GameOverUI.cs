using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button retryButton;

    [Header("Audio")]
    [SerializeField] private LevelStatussPlayer uiSoundPlayer;

    private LevelManager levelManager;

    private void Awake()
    {
        Hide();
    }

    public void Initialize(LevelManager manager)
    {
        levelManager = manager;

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(levelManager.RetryCurrentLevel);
        }

        Hide();
    }

    public void Show(int failedLevel)
    {
        if (gameOverText != null)
        {
            gameOverText.text = "Game Over!\nLevel " + failedLevel + " Failed";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        uiSoundPlayer?.PlayGameOverSound();
    }

    public void Hide()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
}