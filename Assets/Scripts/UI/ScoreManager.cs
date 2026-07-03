using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI reference")]
    [SerializeField] private TMP_Text scoreText;

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

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = "Score: " + CurrentScore;
    }
}