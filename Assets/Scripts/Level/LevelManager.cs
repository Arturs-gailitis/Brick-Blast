using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string SavedLevelKey = "SavedLevel";

    [Header("References")]
    [SerializeField] private BrickSpawner brickSpawner;
    [SerializeField] private AbilitySpawner abilitySpawner;
    [SerializeField] private LayerMask bottomWallLayer;

    [Header("Level settings")]
    [SerializeField] [Min(1)] private int firstLevel = 1;

    [Header("Brick movement after shot")]
    [SerializeField] [Min(0f)] private float brickMoveDownDistance = 1f;

    [Header("Game over settings")]
    [SerializeField] [Min(0f)]
    private float gameOverDistanceFromBottomWall = 0.4f;

    public int CurrentLevel { get; private set; }
    public bool IsGameOver => isGameOver;

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

        if (GameSaveManager.TryLoadGame(out SavedGameData savedGame) &&
            brickSpawner != null &&
            brickSpawner.LevelExists(savedGame.level))
        {
            LoadSavedGame(savedGame);
            yield break;
        }

        int savedLevel = PlayerPrefs.GetInt(SavedLevelKey, firstLevel);

        if (brickSpawner == null || !brickSpawner.LevelExists(savedLevel))
        {
            savedLevel = firstLevel;
            SaveLevelProgress(firstLevel);
        }

        LoadLevel(savedLevel);
    }

    public void SaveCurrentGame()
    {
        if (brickSpawner == null || isChangingLevel || isGameOver)
        {
            return;
        }

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball == null)
        {
            return;
        }

        List<SavedBrickData> savedBricks = brickSpawner.GetCurrentBricks();

        if (savedBricks.Count == 0)
        {
            return;
        }

        List<SavedAbilityData> savedAbilities = abilitySpawner != null
            ? abilitySpawner.GetCurrentAbilities()
            : new List<SavedAbilityData>();

        SavedGameData savedGame = new SavedGameData
        {
            level = CurrentLevel,
            score = ScoreManager.Instance != null
                ? ScoreManager.Instance.CurrentScore
                : 0,
            ball = ball.CreateSaveData(),
            bricks = savedBricks,
            abilitiesWereSaved = true,
            abilities = savedAbilities
        };

        GameSaveManager.SaveGame(savedGame);
        SaveLevelProgress(CurrentLevel);
    }

    public void SaveCurrentBallAttackStrength()
    {
        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball == null)
        {
            return;
        }

        GameSaveManager.SaveBallAttackStrength(ball.AttackStrength);
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

        GameSaveManager.ClearSavedGame();

        int nextLevel = CurrentLevel + 1;

        if (brickSpawner != null && brickSpawner.LevelExists(nextLevel))
        {
            SaveLevelProgress(nextLevel);
        }
        else
        {
            SaveLevelProgress(firstLevel);
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

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.ResetAttackStrength();
        }

        GameSaveManager.ClearSavedGame();
        SaveLevelProgress(firstLevel);
        LoadLevel(firstLevel);

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

    private void LoadSavedGame(SavedGameData savedGame)
    {
        if (brickSpawner == null)
        {
            return;
        }

        CurrentLevel = savedGame.level;

        if (abilitySpawner != null)
        {
            if (savedGame.abilitiesWereSaved)
            {
                abilitySpawner.SpawnSavedAbilities(savedGame.abilities);
            }
            else
            {
                abilitySpawner.SpawnLevel(CurrentLevel);
            }
        }

        remainingBricks = brickSpawner.SpawnSavedLevel(savedGame.bricks);

        if (remainingBricks == 0)
        {
            GameSaveManager.ClearSavedGame();
            LoadLevel(CurrentLevel);
            return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(savedGame.score);
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

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.RestoreSavedState(savedGame.ball);
        }

        CheckForGameOver();
    }

    private void LoadLevel(int level)
    {
        if (brickSpawner == null)
        {
            return;
        }

        GameSaveManager.ClearSavedGame();

        CurrentLevel = level;

        abilitySpawner?.SpawnLevel(CurrentLevel);

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

        abilitySpawner?.MoveAllAbilitiesDown(brickMoveDownDistance);

        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (isGameOver || bottomWall == null)
        {
            return;
        }

        float dangerY =
            bottomWall.bounds.max.y +
            gameOverDistanceFromBottomWall;

        BrickCollision[] bricks =
            FindObjectsByType<BrickCollision>(
                FindObjectsSortMode.None
            );

        foreach (BrickCollision brick in bricks)
        {
            Collider2D brickCollider =
                brick.GetComponent<Collider2D>();

            if (brickCollider == null)
            {
                continue;
            }

            if (brickCollider.bounds.min.y <= dangerY)
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

        GameSaveManager.ClearSavedGame();

        SetLevelCompletePanelVisible(false);

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.ResetAttackStrength();
            ball.PrepareForNextLevel();
        }
        else
        {
            GameSaveManager.SaveBallAttackStrength(1);
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
            playerBall =
                FindFirstObjectByType<PlayerBallTrajectory>();
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
        Collider2D[] colliders =
            FindObjectsByType<Collider2D>(
                FindObjectsSortMode.None
            );

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

    private void SaveLevelProgress(int level)
    {
        PlayerPrefs.SetInt(SavedLevelKey, level);
        PlayerPrefs.Save();
    }

    public static void ResetSavedProgress()
    {
        PlayerPrefs.DeleteKey(SavedLevelKey);
        GameSaveManager.ClearSavedGame();
        GameSaveManager.SaveBallAttackStrength(1);
        PlayerPrefs.Save();
    }
}