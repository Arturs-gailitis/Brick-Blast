using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private const string SavedScoreKey = "SavedScore";

    [Header("UI references")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text levelText;

    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentScore = PlayerPrefs.GetInt(SavedScoreKey, 0);

        UpdateScoreText();
    }

    public void AddScore(int points)
    {
        CurrentScore += Mathf.Max(0, points);

        SaveScore();
        UpdateScoreText();
    }

    public void SetScore(int score)
    {
        CurrentScore = Mathf.Max(0, score);

        SaveScore();
        UpdateScoreText();
    }

    public void ResetScore()
    {
        SetScore(0);
    }

    public static void ResetSavedScore()
    {
        PlayerPrefs.DeleteKey(SavedScoreKey);
        PlayerPrefs.Save();
    }

    public void UpdateLevelText(int level)
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text = "Level: " + level;
    }

    private void SaveScore()
    {
        PlayerPrefs.SetInt(SavedScoreKey, CurrentScore);
        PlayerPrefs.Save();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = "Score: " + CurrentScore;
    }
}