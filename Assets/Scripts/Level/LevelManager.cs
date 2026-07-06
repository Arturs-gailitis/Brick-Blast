using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BrickSpawner brickSpawner;
    [SerializeField] private LayerMask bottomWallLayer;

    [Header("Level settings")]
    [SerializeField] [Min(1)] private int firstLevel = 1;

    [Header("Brick movement after shot")]
    [SerializeField] [Min(0f)] private float brickMoveDownDistance = 1f;

    [Header("Game over settings")]
    [SerializeField] [Min(0f)] private float gameOverDistanceFromBottomWall = 0.4f;

    public int CurrentLevel { get; private set; }

    private int remainingBricks;
    private bool isChangingLevel;
    private bool waitingForNextLevel;
    private bool isGameOver;

    private LevelCompleteUI levelCompleteUI;
    private GameOverUI gameOverUI;
    private PlayerBallTrajectory playerBall;
    private Collider2D bottomWall;

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

        FindRuntimeBottomWall();

        levelCompleteUI = FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include);

        gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (levelCompleteUI != null)
        {
            levelCompleteUI.Initialize(this);
        }

        if (gameOverUI != null)
        {
            gameOverUI.Initialize(this);
        }

        LoadLevel(firstLevel);
    }

    public void BrickDestroyed()
    {
        if (isChangingLevel || isGameOver)
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
        if (!waitingForNextLevel || isGameOver)
        {
            return;
        }

        int nextLevel = CurrentLevel + 1;

        if (brickSpawner == null || !brickSpawner.LevelExists(nextLevel))
        {
            return;
        }

        LoadLevel(nextLevel);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.SetGameplayInputEnabled(true);
        }
    }

    public void RetryCurrentLevel()
    {
        if (!isGameOver)
        {
            return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        LoadLevel(firstLevel);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.SetGameplayInputEnabled(true);
        }
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
        isGameOver = false;

        SetLevelCompletePanelVisible(false);

        if (gameOverUI != null)
        {
            gameOverUI.Hide();
        }

        CheckForGameOver();
    }

    public void MoveAllBricksDown()
    {
        if (isChangingLevel || isGameOver)
        {
            return;
        }

        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        foreach (BrickCollision brick in bricks)
        {
            brick.transform.position += Vector3.down * brickMoveDownDistance;
        }

        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (isGameOver || bottomWall == null)
        {
            return;
        }

        float dangerY = bottomWall.bounds.max.y + gameOverDistanceFromBottomWall;

        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        foreach (BrickCollision brick in bricks)
        {
            Collider2D brickCollider = brick.GetComponent<Collider2D>();

            if (brickCollider == null)
            {
                continue;
            }

            float brickBottomY = brickCollider.bounds.min.y;

            if (brickBottomY <= dangerY)
            {
                ShowGameOver();
                return;
            }
        }
    }

    private void ShowGameOver()
    {
        isGameOver = true;
        waitingForNextLevel = false;

        SetLevelCompletePanelVisible(false);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.PrepareForNextLevel();
        }

        if (gameOverUI != null)
        {
            gameOverUI.Show(CurrentLevel);
        }
    }

    private PlayerBallTrajectory GetPlayerBall()
    {
        if (playerBall == null)
        {
            playerBall = FindFirstObjectByType<PlayerBallTrajectory>();
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

    private void FindRuntimeBottomWall()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        foreach (Collider2D collider in colliders)
        {
            int colliderLayerMask = 1 << collider.gameObject.layer;

            if ((bottomWallLayer.value & colliderLayerMask) != 0)
            {
                bottomWall = collider;
                return;
            }
        }
    }
}