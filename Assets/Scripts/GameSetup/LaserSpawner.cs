using System.Collections.Generic;
using UnityEngine;
using System;

public class LaserSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject cellReferencePrefab;
    [SerializeField] private AbilityConfigReader abilityConfigReader;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Grid settings")]
    [SerializeField] [Min(1)] private int gridColumns = 7;
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.5f;
    [SerializeField] private float horizontalOffset = 0.25f;

    [Header("Laser size")]
    [SerializeField] private bool resizeLaserToCellSize = true;
    [SerializeField] [Range(0.1f, 1f)] private float laserSizeMultiplier = 0.9f;

    private Transform lasersParent;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Lasers");
        lasersParent = parentObject.transform;
    }

    public int SpawnLevel(int level)
    {
        ClearLasers();

        if (laserPrefab == null || abilityConfigReader == null)
        {
            return 0;
        }

        if (!TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall))
        {
            return 0;
        }

        if (!TryGetCellSize(out float cellWidth, out float cellHeight))
        {
            return 0;
        }

        List<AbilityConfig> levelLasers = abilityConfigReader.GetLasersForLevel(level);

        if (levelLasers.Count == 0)
        {
            return 0;
        }

        float gridWidth = gridColumns * cellWidth + (gridColumns - 1) * horizontalSpacing;
        float availableWidth = rightWall.bounds.min.x - leftWall.bounds.max.x;

        if (gridWidth > availableWidth)
        {
            return 0;
        }

        float firstCellX =
            (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f -
            gridWidth / 2f +
            cellWidth / 2f +
            horizontalOffset;

        float firstCellY = topWall.bounds.min.y - distanceFromTopWall - cellHeight / 2f;

        int spawnedLasers = 0;

        foreach (AbilityConfig laserConfig in levelLasers)
        {
            if (laserConfig.column >= gridColumns)
            {
                continue;
            }

            float x = firstCellX + laserConfig.column * (cellWidth + horizontalSpacing);
            float y = firstCellY - laserConfig.row * (cellHeight + verticalSpacing);

            GameObject newLaser = Instantiate(
                laserPrefab,
                new Vector3(x, y, 0f),
                Quaternion.identity,
                lasersParent
            );

            if (resizeLaserToCellSize)
            {
                ResizeLaserToCellSize(newLaser, cellWidth, cellHeight);
            }

            newLaser.name = "Laser_" + level + "_" + laserConfig.row + "_" + laserConfig.column;

            LaserAbility laserAbility = newLaser.GetComponent<LaserAbility>();

            if (laserAbility != null)
            {
                laserAbility.Configure(laserConfig);
            }

            spawnedLasers++;
        }

        return spawnedLasers;
    }

    public void MoveAllLasersDown(float distance)
    {
        if (lasersParent == null)
        {
            return;
        }

        for (int i = 0; i < lasersParent.childCount; i++)
        {
            lasersParent.GetChild(i).position += Vector3.down * distance;
        }
    }

    public void ClearLasers()
    {
        if (lasersParent == null)
        {
            return;
        }

        for (int i = lasersParent.childCount - 1; i >= 0; i--)
        {
            Destroy(lasersParent.GetChild(i).gameObject);
        }
    }

    private bool TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall)
    {
        leftWall = null;
        rightWall = null;
        topWall = null;

        if (walls == null || walls.Length < 2)
        {
            return false;
        }

        Collider2D[] sideWallColliders = walls[0].GetComponentsInChildren<Collider2D>();

        if (sideWallColliders.Length < 2)
        {
            return false;
        }

        leftWall = sideWallColliders[0];
        rightWall = sideWallColliders[1];

        if (leftWall.bounds.center.x > rightWall.bounds.center.x)
        {
            Collider2D temporaryWall = leftWall;
            leftWall = rightWall;
            rightWall = temporaryWall;
        }

        topWall = walls[1].GetComponent<Collider2D>();

        return topWall != null;
    }

    private void ResizeLaserToCellSize(GameObject laserObject, float cellWidth, float cellHeight)
    {
        if (laserObject == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = laserObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float targetSize = Mathf.Min(cellWidth, cellHeight) * laserSizeMultiplier;
        float spriteLargestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float scale = targetSize / spriteLargestSide;

        laserObject.transform.localScale = new Vector3(scale, scale, 1f);

        CircleCollider2D circleCollider = laserObject.GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.offset = Vector2.zero;
        }
    }

    public List<SavedAbilityData> GetCurrentAbilities()
    {
        List<SavedAbilityData> savedAbilities = new List<SavedAbilityData>();

        if (lasersParent == null)
        {
            return savedAbilities;
        }

        for (int i = 0; i < lasersParent.childCount; i++)
        {
            LaserAbility laserAbility =
                lasersParent.GetChild(i).GetComponent<LaserAbility>();

            if (laserAbility == null)
            {
                continue;
            }

            SavedAbilityData savedAbility = laserAbility.CreateSaveData();

            if (savedAbility != null)
            {
                savedAbilities.Add(savedAbility);
            }
        }

        return savedAbilities;
    }

    public int SpawnSavedAbilities(List<SavedAbilityData> savedAbilities)
    {
        ClearLasers();

        if (laserPrefab == null || savedAbilities == null)
        {
            return 0;
        }

        TryGetCellSize(out float cellWidth, out float cellHeight);

        int spawnedAbilities = 0;

        foreach (SavedAbilityData savedAbility in savedAbilities)
        {
            if (savedAbility == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(savedAbility.abilityType) &&
                !string.Equals(savedAbility.abilityType, "laser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            GameObject newLaser = Instantiate(
                laserPrefab,
                new Vector3(savedAbility.x, savedAbility.y, 0f),
                Quaternion.identity,
                lasersParent
            );

            if (resizeLaserToCellSize && cellWidth > 0f && cellHeight > 0f)
            {
                ResizeLaserToCellSize(newLaser, cellWidth, cellHeight);
            }

            newLaser.name = "SavedLaser_" + spawnedAbilities;

            LaserAbility laserAbility = newLaser.GetComponent<LaserAbility>();

            if (laserAbility != null)
            {
                laserAbility.Configure(new AbilityConfig
                {
                    level = 1,
                    row = 0,
                    column = 0,
                    abilityType = "laser",
                    value = savedAbility.value,
                    durationSeconds = savedAbility.durationSeconds,
                    aim = savedAbility.aim
                });
            }

            spawnedAbilities++;
        }

        return spawnedAbilities;
    }

    private bool TryGetCellSize(out float cellWidth, out float cellHeight)
    {
        cellWidth = 0f;
        cellHeight = 0f;

        GameObject referencePrefab = cellReferencePrefab != null
            ? cellReferencePrefab
            : laserPrefab;

        if (referencePrefab == null)
        {
            return false;
        }

        BoxCollider2D cellCollider = referencePrefab.GetComponent<BoxCollider2D>();

        if (cellCollider == null)
        {
            return false;
        }

        cellWidth =
            cellCollider.size.x *
            Mathf.Abs(referencePrefab.transform.localScale.x);

        cellHeight =
            cellCollider.size.y *
            Mathf.Abs(referencePrefab.transform.localScale.y);

        return cellWidth > 0f && cellHeight > 0f;
    }
}