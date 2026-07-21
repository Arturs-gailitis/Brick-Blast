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
    [SerializeField] [Min(0f)] private float brickMoveDownDistance;
    [SerializeField] [Min(0.01f)] private float brickMoveDownDuration;

    [Header("Rows loading")]
    [SerializeField] [Min(1)] private int visibleRowsAtLevelStart = 3;
    [SerializeField] [Min(1)] private int movesBeforeNewRow;

    [Header("Game over settings")]
    [SerializeField] [Min(0f)] private float gameOverDistanceFromBottomWall;

    [Header("Fully cleared bonus")]
    [SerializeField] [Min(1)] private int fullyClearedRowsToMove = 3;
    [SerializeField] [Min(1)] private int fullyClearedBallMultiplier = 3;

    public int CurrentLevel { get; private set; }
    public bool IsGameOver => isGameOver;

    private int remainingBricks;
    private bool isChangingLevel;
    private bool waitingForNextLevel;
    private bool isGameOver;
    private bool isMovingObjectsDown;

    private bool fullyClearedBonusActive;
    private bool skipNextRegularMove;

    private int attackStrengthAtLevelStart = 1;
    private int downMoveCounter = 0;

    private LevelCompleteUI levelCompleteUI;
    private GameOverUI gameOverUI;
    private PlayerBallTrajectory playerBall;
    private Collider2D bottomWall;
    private FullyClearedUI fullyClearedUI;

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

        if (fullyClearedUI == null)
        {
            fullyClearedUI = FindFirstObjectByType<FullyClearedUI>(FindObjectsInactive.Include);
        }

        fullyClearedUI?.HideImmediate();

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

        List<SavedAbilityData> savedAbilities = abilitySpawner != null ? abilitySpawner.GetCurrentAbilities()
            : new List<SavedAbilityData>();

        SavedGameData savedGame = new SavedGameData
        {
            level = CurrentLevel,

            score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0,

            ball = ball.CreateSaveData(),
            bricks = savedBricks,

            abilitiesWereSaved = true,
            abilities = savedAbilities,

            nextBrickRowToSpawn = brickSpawner != null ? brickSpawner.GetNextRowToSpawn() : visibleRowsAtLevelStart,

            nextAbilityRowToSpawn = abilitySpawner != null ? abilitySpawner.GetNextRowToSpawn()
                : visibleRowsAtLevelStart,

            downMoveCounter = downMoveCounter
        };

        GameSaveManager.SaveGame(savedGame);
        SaveLevelProgress(CurrentLevel);
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
            TryStartFullyClearedBonus();
            return;
        }

        SaveCurrentAttackAsNextLevelStartAttack();

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

    public void RetryFromFirstLevel()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.ResetAttackStrength();
        }
        else
        {
            GameSaveManager.SaveBallAttackStrength(1);
        }

        GameSaveManager.ClearSavedGame();
        GameSaveManager.SaveBallAttackStrength(1);

        SaveLevelProgress(firstLevel);
        LoadLevel(firstLevel);

        ball = GetPlayerBall();

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

        downMoveCounter =
            Mathf.Max(0, savedGame.downMoveCounter);

        brickSpawner.PrepareSavedLevel(
            CurrentLevel,
            savedGame.nextBrickRowToSpawn
        );

        if (abilitySpawner != null)
        {
            abilitySpawner.PrepareSavedLevel(
                CurrentLevel,
                savedGame.nextAbilityRowToSpawn
            );
        }

        RememberLevelStartAttackStrength();

        if (abilitySpawner != null)
        {
            if (savedGame.abilitiesWereSaved)
            {
                abilitySpawner.SpawnSavedAbilities(
                    savedGame.abilities
                );
            }
            else
            {
                abilitySpawner.SpawnLevel(
                    CurrentLevel,
                    visibleRowsAtLevelStart
                );
            }
        }

        int spawnedSavedBricks = brickSpawner.SpawnSavedLevel(savedGame.bricks);

        int unspawnedBricks = brickSpawner.GetUnspawnedBrickCount();

        remainingBricks = spawnedSavedBricks + unspawnedBricks;

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
        isMovingObjectsDown = false;

        SetLevelCompletePanelVisible(false);

        if (gameOverUI != null)
        {
            gameOverUI.Hide();
        }

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball != null)
        {
            ball.RestoreSavedState(savedGame.ball);
            ApplyLevelStartAttackStrengthToBall();
        }

        ResetFullyClearedBonus();

        CheckForGameOver();
    }

    private void LoadLevel(int level)
    {
        if (brickSpawner == null)
        {
            return;
        }

        GameSaveManager.ClearSavedGame();

        RememberLevelStartAttackStrength();

        CurrentLevel = level;

        downMoveCounter = 0;

        abilitySpawner?.SpawnLevel(CurrentLevel, visibleRowsAtLevelStart);

        remainingBricks = brickSpawner.SpawnLevel(CurrentLevel, visibleRowsAtLevelStart);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateLevelText(CurrentLevel);
        }

        isChangingLevel = false;
        waitingForNextLevel = false;
        isGameOver = false;
        isMovingObjectsDown = false;

        SetLevelCompletePanelVisible(false);

        if (gameOverUI != null)
        {
            gameOverUI.Hide();
        }

        ApplyLevelStartAttackStrengthToBall();
        ResetFullyClearedBonus();

        CheckForGameOver();
    }

    public void MoveAllBricksDown()
    {
        if (skipNextRegularMove)
        {
            skipNextRegularMove = false;
            return;
        }

        if (isChangingLevel || isGameOver || isMovingObjectsDown || fullyClearedBonusActive)
        {
            return;
        }

        int safeMovesBeforeNewRow = Mathf.Max(1, movesBeforeNewRow);

        float moveDownDistance = brickMoveDownDistance;

        if (brickSpawner != null)
        {
            float rowStep = brickSpawner.GetRowStep();

            if (rowStep > 0f)
            {
                moveDownDistance = rowStep / safeMovesBeforeNewRow;
            }
        }

        StartCoroutine(MoveAllObjectsDownSmooth(moveDownDistance, true));
    }

    private IEnumerator MoveAllObjectsDownSmooth(float moveDownDistance, bool updateRegularMoveCounter)
    {
        isMovingObjectsDown = true;

        int safeMovesBeforeNewRow = Mathf.Max(1, movesBeforeNewRow);

        moveDownDistance = Mathf.Max(0f, moveDownDistance);

        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        Vector3[] startPositions = new Vector3[bricks.Length];

        Vector3[] targetPositions = new Vector3[bricks.Length];

        for (int i = 0; i < bricks.Length; i++)
        {
            startPositions[i] = bricks[i].transform.position;

            targetPositions[i] = startPositions[i] + Vector3.down * moveDownDistance;}

        Coroutine abilityMoveCoroutine = null;

        if (abilitySpawner != null)
        {
            abilityMoveCoroutine = StartCoroutine(abilitySpawner.MoveAllAbilitiesDownSmooth(moveDownDistance,
                brickMoveDownDuration));
        }

        float elapsedTime = 0f;

        while (elapsedTime < brickMoveDownDuration)
        {
            elapsedTime += Time.deltaTime;

            float movePercent = Mathf.Clamp01(elapsedTime / brickMoveDownDuration);

            for (int i = 0; i < bricks.Length; i++)
            {
                if (bricks[i] == null)
                {
                    continue;
                }

                bricks[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], movePercent);
            }

            brickSpawner?.RefreshRowDepths();

            yield return null;
        }

        for (int i = 0; i < bricks.Length; i++)
        {
            if (bricks[i] == null)
            {
                continue;
            }

            bricks[i].transform.position = targetPositions[i];
        }

        brickSpawner?.RefreshRowDepths();

        if (abilityMoveCoroutine != null)
        {
            yield return abilityMoveCoroutine;
        }

        if (updateRegularMoveCounter)
        {
            downMoveCounter++;

            if (downMoveCounter >= safeMovesBeforeNewRow)
            {
                downMoveCounter = 0;
            }
        }

        isMovingObjectsDown = false;

        CheckForGameOver();
    }

    private void TryStartFullyClearedBonus()
    {
        if (fullyClearedBonusActive || isChangingLevel || isGameOver)
        {
            return;
        }

        if (!OnlyHiddenBricksRemain())
        {
            return;
        }

        StartCoroutine(HandleFullyClearedBonus());
    }

    private bool OnlyHiddenBricksRemain()
    {
        BrickCollision[] bricks = FindObjectsByType<BrickCollision>(FindObjectsSortMode.None);

        bool hasHiddenBrick = false;

        foreach (BrickCollision brick in bricks)
        {
            if (brick == null || brick.IsDestroyed)
            {
                continue;
            }

            HiddenRowDepth hiddenRowDepth = brick.GetComponent<HiddenRowDepth>();

            bool isHidden = hiddenRowDepth != null && hiddenRowDepth.IsHidden;

            if (!isHidden)
            {
                return false;
            }

            hasHiddenBrick = true;
        }

        return hasHiddenBrick;
    }

    private IEnumerator HandleFullyClearedBonus()
    {
        fullyClearedBonusActive = true;

        PlayerBallTrajectory ball = GetPlayerBall();

        MultiBallShooter shooter = ball != null ? ball.GetComponent<MultiBallShooter>() : null;

        if (shooter != null)
        {
            shooter.MultiplyNextShotBallCount(fullyClearedBallMultiplier);
        }

        skipNextRegularMove = ball != null && ball.TurnIsActive;

        fullyClearedUI?.Show(fullyClearedBallMultiplier);

        while (ball != null && ball.TurnIsActive && !isChangingLevel && !isGameOver)
        {
            yield return null;
        }

        if (isChangingLevel || isGameOver)
        {
            skipNextRegularMove = false;
            fullyClearedBonusActive = false;
            yield break;
        }

        float moveDownDistance = GetFullRowStep() * Mathf.Max(1, fullyClearedRowsToMove);

        downMoveCounter = 0;

        yield return StartCoroutine(MoveAllObjectsDownSmooth(moveDownDistance, false));

        skipNextRegularMove = false;
        fullyClearedBonusActive = false;
    }

    private float GetFullRowStep()
    {
        if (brickSpawner != null)
        {
            float rowStep = brickSpawner.GetRowStep();

            if (rowStep > 0f)
            {
                return rowStep;
            }
        }

        return Mathf.Max(0f, brickMoveDownDistance) * Mathf.Max(1, movesBeforeNewRow);
    }

    private void ResetFullyClearedBonus()
    {
        fullyClearedBonusActive = false;
        skipNextRegularMove = false;

        fullyClearedUI?.HideImmediate();

        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball == null)
        {
            return;
        }

        MultiBallShooter shooter = ball.GetComponent<MultiBallShooter>();

        if (shooter != null)
        {
            shooter.ResetTemporaryBallCountBonus();
        }
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

        ResetFullyClearedBonus();

        if (gameOverUI != null)
        {
            gameOverUI.Show(CurrentLevel);
        }
    }

    private void RememberLevelStartAttackStrength()
    {
        attackStrengthAtLevelStart = GameSaveManager.LoadBallAttackStrength();
    }

    private void ApplyLevelStartAttackStrengthToBall()
    {
        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball == null)
        {
            return;
        }

        ball.SetAttackStrength(attackStrengthAtLevelStart);
    }

    private void SaveCurrentAttackAsNextLevelStartAttack()
    {
        PlayerBallTrajectory ball = GetPlayerBall();

        if (ball == null)
        {
            return;
        }

        attackStrengthAtLevelStart = Mathf.Max(1, ball.AttackStrength);
        GameSaveManager.SaveBallAttackStrength(attackStrengthAtLevelStart);
    }

    public void SaveLevelStartBallAttackStrength()
    {
        GameSaveManager.SaveBallAttackStrength(attackStrengthAtLevelStart);
    }

    private void SaveGameWhenProgramCloses()
    {
        if (isGameOver || isChangingLevel)
        {
            return;
        }

        SaveCurrentGame();

        GameSaveManager.SaveBallAttackStrength(attackStrengthAtLevelStart);
    }

    private void OnApplicationQuit()
    {
        SaveGameWhenProgramCloses();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGameWhenProgramCloses();
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