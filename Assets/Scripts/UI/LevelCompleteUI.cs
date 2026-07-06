using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TMP_Text levelCompleteText;
    [SerializeField] private Button nextLevelButton;

    [Header("Audio")]
    [SerializeField] private LevelStatussPlayer uiSoundPlayer;

    private LevelManager levelManager;

    public void Initialize(LevelManager manager)
    {
        levelManager = manager;

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(ContinueToNextLevel);
        }

        Hide();
    }

    public void Show(int completedLevel, bool hasNextLevel)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (levelCompleteText != null)
        {
            levelCompleteText.text = hasNextLevel
                ? "Level " + completedLevel + " completed!"
                : "Congratulations!\nYou completed all levels!";
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(hasNextLevel);
        }

        uiSoundPlayer?.PlayLevelCompleteSound();
    }

    public void Hide()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    private void ContinueToNextLevel()
    {
        if (levelManager != null)
        {
            levelManager.ContinueToNextLevel();
        }
    }
}