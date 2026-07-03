using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BrickSpawner brickSpawner;

    [Header("Level settings")]
    [SerializeField] [Min(1)] private int firstLevel = 1;
    [SerializeField] [Min(0f)] private float nextLevelDelay = 0.7f;

    public int CurrentLevel { get; private set; }

    private int remainingBricks;
    private bool isChangingLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return null;

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
        StartCoroutine(LoadNextLevel());
    }

    private IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(nextLevelDelay);

        int nextLevel = CurrentLevel + 1;

        if (!brickSpawner.LevelExists(nextLevel))
        {
            yield break;
        }

        LoadLevel(nextLevel);
    }

    private void LoadLevel(int level)
    {
        if (brickSpawner == null)
        {
            return;
        }

        CurrentLevel = level;
        remainingBricks = brickSpawner.SpawnLevel(CurrentLevel);
        isChangingLevel = false;
    }
}