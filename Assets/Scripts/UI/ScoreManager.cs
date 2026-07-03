using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

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

        UpdateScoreText();
    }

    public void AddScore(int points)
    {
        CurrentScore += Mathf.Max(0, points);
        UpdateScoreText();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateScoreText();
    }

    public void UpdateLevelText(int level)
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text = "Level: " + level;
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