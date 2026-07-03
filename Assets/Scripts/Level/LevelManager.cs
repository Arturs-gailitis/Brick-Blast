using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BrickSpawner brickSpawner;

    [Header("Level settings")]
    [SerializeField] [Min(1)] private int firstLevel = 1;

    public int CurrentLevel { get; private set; }

    private int remainingBricks;
    private bool isChangingLevel;
    private bool waitingForNextLevel;
    private LevelCompleteUI levelCompleteUI;
    private PlayerBallTrajectory playerBall;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetLevelCompletePanelVisible(false);
    }

    private IEnumerator Start()
    {
        yield return null;

        levelCompleteUI = FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include);

        if (levelCompleteUI != null)
        {
            levelCompleteUI.Initialize(this);
        }

        LoadLevel(firstLevel);
    }

    public void BrickDestroyed()
    {
        if (isChangingLevel)
        {
            return;
        }

        remainingBricks--;

        if (remainingBricks > 0)
        {
            return;
        }

        isChangingLevel = true;
        ShowLevelCompleteMenu();
    }

    public void ContinueToNextLevel()
    {
        if (!waitingForNextLevel)
        {
            return;
        }

        int nextLevel = CurrentLevel + 1;

        if (brickSpawner == null || !brickSpawner.LevelExists(nextLevel))
        {
            return;
        }

        SetLevelCompletePanelVisible(false);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.SetGameplayInputEnabled(true);
        }

        LoadLevel(nextLevel);
    }

    private void ShowLevelCompleteMenu()
    {
        int nextLevel = CurrentLevel + 1;

        bool hasNextLevel = brickSpawner != null && brickSpawner.LevelExists(nextLevel);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.PrepareForNextLevel();
        }

        waitingForNextLevel = hasNextLevel;

        if (levelCompleteUI != null)
        {
            levelCompleteUI.Show(CurrentLevel, hasNextLevel);
        }
    }

    private void LoadLevel(int level)
    {
        if (brickSpawner == null)
        {
            return;
        }

        CurrentLevel = level;
        remainingBricks = brickSpawner.SpawnLevel(CurrentLevel);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateLevelText(CurrentLevel);
        }

        isChangingLevel = false;
        waitingForNextLevel = false;

        SetLevelCompletePanelVisible(false);
    }

    private PlayerBallTrajectory GetPlayerBall()
    {
        if (playerBall == null)
        {
            playerBall = FindFirstObjectByType<PlayerBallTrajectory>();;
        }

        return playerBall;
    }

    private void SetLevelCompletePanelVisible(bool isVisible)
    {
        if (levelCompleteUI == null)
        {
            return;
        }

        if (isVisible)
        {
            levelCompleteUI.Show(CurrentLevel, true);
        }
        else
        {
            levelCompleteUI.Hide();
        }
    }
}