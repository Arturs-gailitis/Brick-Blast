using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject powerPrefab;
    [SerializeField] private GameObject directionPrefab;
    [SerializeField] private GameObject cellReferencePrefab;
    [SerializeField] private AbilityConfigReader abilityConfigReader;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Grid settings")]
    [SerializeField] [Min(1)] private int gridColumns = 7;
    [SerializeField] private float horizontalSpacing = 0.08f;
    [SerializeField] private float verticalSpacing = 0.08f;
    [SerializeField] private float distanceFromTopWall = 0.5f;
    [SerializeField] private float horizontalOffset;

    [Header("Ability size")]
    [SerializeField] private bool resizeAbilityToCellSize = true;
    [SerializeField] [Range(0.1f, 1f)] private float abilitySizeMultiplier = 0.9f;

    private Transform abilitiesParent;

    private List<AbilityConfig> currentLevelAbilities = new List<AbilityConfig>();
    private int nextRowToSpawn;
    private int maxRowInLevel = -1;

    private float cachedFirstCellX;
    private float cachedFirstCellY;
    private float cachedCellWidth;
    private float cachedCellHeight;
    private bool hasCachedGrid;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Abilities");
        abilitiesParent = parentObject.transform;
    }

    public int SpawnLevel(int level)
    {
        return SpawnLevel(level, 3);
    }

    public int SpawnLevel(int level, int visibleRowsAtStart)
    {
        ClearAbilities();

        nextRowToSpawn = 0;
        maxRowInLevel = -1;
        hasCachedGrid = false;

        if (abilityConfigReader == null)
        {
            return 0;
        }

        currentLevelAbilities = abilityConfigReader.GetAbilitiesForLevel(level);

        if (currentLevelAbilities.Count == 0)
        {
            return 0;
        }

        if (!PrepareGrid())
        {
            return 0;
        }

        maxRowInLevel = GetMaxRow(currentLevelAbilities);

        int spawnedAbilities = 0;

        for (int i = 0; i < visibleRowsAtStart; i++)
        {
            spawnedAbilities += SpawnCurrentNextRow(nextRowToSpawn, level);
        }

        return spawnedAbilities;
    }

    private GameObject GetPrefabForAbility(string abilityType)
    {
        if (string.Equals(abilityType, "laser", StringComparison.OrdinalIgnoreCase))
        {
            return laserPrefab;
        }

        if (string.Equals(abilityType, "power", StringComparison.OrdinalIgnoreCase))
        {
            return powerPrefab;
        }

        if (string.Equals(abilityType, "direction", StringComparison.OrdinalIgnoreCase))
        {
            return directionPrefab;
        }

        return null;
    }

    private void ConfigureSpawnedAbility(GameObject abilityObject, AbilityConfig abilityConfig)
    {
        if (abilityObject == null || abilityConfig == null)
        {
            return;
        }

        if (string.Equals(abilityConfig.abilityType, "laser", StringComparison.OrdinalIgnoreCase))
        {
            LaserAbility laserAbility = abilityObject.GetComponent<LaserAbility>();

            if (laserAbility != null)
            {
                laserAbility.Configure(abilityConfig);
            }

            return;
        }

        if (string.Equals(abilityConfig.abilityType, "power", StringComparison.OrdinalIgnoreCase))
        {
            PowerAbility powerAbility = abilityObject.GetComponent<PowerAbility>();

            if (powerAbility != null)
            {
                powerAbility.Configure(abilityConfig);
            }
        }

        if (string.Equals(abilityConfig.abilityType, "direction", StringComparison.OrdinalIgnoreCase))
        {
            DirectionAbility directionAbility = abilityObject.GetComponent<DirectionAbility>();

            if (directionAbility != null)
            {
                directionAbility.Configure(abilityConfig);
            }

            return;
        }
    }

    public IEnumerator MoveAllAbilitiesDownSmooth(float distance, float duration)
    {
        if (abilitiesParent == null)
        {
            yield break;
        }

        int abilityCount = abilitiesParent.childCount;

        Transform[] abilityTransforms = new Transform[abilityCount];
        Vector3[] startPositions = new Vector3[abilityCount];
        Vector3[] targetPositions = new Vector3[abilityCount];

        for (int i = 0; i < abilityCount; i++)
        {
            Transform abilityTransform = abilitiesParent.GetChild(i);

            abilityTransforms[i] = abilityTransform;
            startPositions[i] = abilityTransform.position;
            targetPositions[i] = startPositions[i] + Vector3.down * distance;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float movePercent = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < abilityTransforms.Length; i++)
            {
                if (abilityTransforms[i] == null)
                {
                    continue;
                }

                abilityTransforms[i].position = Vector3.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    movePercent
                );
            }

            yield return null;
        }

        for (int i = 0; i < abilityTransforms.Length; i++)
        {
            if (abilityTransforms[i] == null)
            {
                continue;
            }

            abilityTransforms[i].position = targetPositions[i];
        }
    }

    public void ClearLasers()
    {
        ClearAbilities();
    }

    public void ClearAbilities()
    {
        if (abilitiesParent == null)
        {
            return;
        }

        for (int i = abilitiesParent.childCount - 1; i >= 0; i--)
        {
            Destroy(abilitiesParent.GetChild(i).gameObject);
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

    private void ResizeAbilityToCellSize(GameObject abilityObject, float cellWidth, float cellHeight)
    {
        if (abilityObject == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = abilityObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float targetSize = Mathf.Min(cellWidth, cellHeight) * abilitySizeMultiplier;
        float spriteLargestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float scale = targetSize / spriteLargestSide;

        abilityObject.transform.localScale = new Vector3(scale, scale, 1f);

        CircleCollider2D circleCollider = abilityObject.GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.offset = Vector2.zero;
        }
    }

    public List<SavedAbilityData> GetCurrentAbilities()
    {
        List<SavedAbilityData> savedAbilities = new List<SavedAbilityData>();

        if (abilitiesParent == null)
        {
            return savedAbilities;
        }

        for (int i = 0; i < abilitiesParent.childCount; i++)
        {
            Transform abilityTransform = abilitiesParent.GetChild(i);

            LaserAbility laserAbility = abilityTransform.GetComponent<LaserAbility>();

            if (laserAbility != null)
            {
                SavedAbilityData savedLaser = laserAbility.CreateSaveData();

                if (savedLaser != null)
                {
                    savedAbilities.Add(savedLaser);
                }

                continue;
            }

            PowerAbility powerAbility = abilityTransform.GetComponent<PowerAbility>();

            if (powerAbility != null)
            {
                SavedAbilityData savedPower = powerAbility.CreateSaveData();

                if (savedPower != null)
                {
                    savedAbilities.Add(savedPower);
                }

                continue;
                
            }

            DirectionAbility directionAbility = abilityTransform.GetComponent<DirectionAbility>();

            if (directionAbility != null)
            {
                SavedAbilityData savedDirection = directionAbility.CreateSaveData();

                if (savedDirection != null)
                {
                    savedAbilities.Add(savedDirection);
                }

                continue;
            }
        }

        return savedAbilities;
    }

    public int SpawnSavedAbilities(List<SavedAbilityData> savedAbilities)
    {
        ClearLasers();

        if (savedAbilities == null)
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

            GameObject selectedPrefab = GetPrefabForAbility(savedAbility.abilityType);

            if (selectedPrefab == null)
            {
                continue;
            }

            GameObject newAbility = Instantiate(
                selectedPrefab,
                new Vector3(savedAbility.x, savedAbility.y, 0f),
                Quaternion.identity,
                abilitiesParent
            );

            if (resizeAbilityToCellSize && cellWidth > 0f && cellHeight > 0f)
            {
                ResizeAbilityToCellSize(newAbility, cellWidth, cellHeight);
            }

            newAbility.name = "Saved_" + savedAbility.abilityType + "_" + spawnedAbilities;

            AbilityConfig abilityConfig = new AbilityConfig
            {
                level = 1,
                row = 0,
                column = 0,
                abilityType = savedAbility.abilityType,
                value = savedAbility.value,
                durationSeconds = savedAbility.durationSeconds,
                aim = savedAbility.aim
            };

            ConfigureSpawnedAbility(newAbility, abilityConfig);

            spawnedAbilities++;
        }

        return spawnedAbilities;
    }

    private bool TryGetCellSize(out float cellWidth, out float cellHeight)
    {
        cellWidth = 0f;
        cellHeight = 0f;

        GameObject referencePrefab = cellReferencePrefab;

        if (referencePrefab == null)
        {
            referencePrefab = laserPrefab;
        }

        if (referencePrefab == null)
        {
            referencePrefab = powerPrefab;
        }

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

    public int SpawnNextRowAtTop()
    {
        return SpawnCurrentNextRow(0, 0);
    }

    private int SpawnCurrentNextRow(int visualRow, int levelForName)
    {
        if (!hasCachedGrid || currentLevelAbilities == null)
        {
            return 0;
        }

        if (nextRowToSpawn > maxRowInLevel)
        {
            return 0;
        }

        int spawnedAbilities = SpawnRow(nextRowToSpawn, visualRow, levelForName);

        nextRowToSpawn++;

        return spawnedAbilities;
    }

    private int SpawnRow(int csvRow, int visualRow, int levelForName)
    {
        int spawnedAbilities = 0;

        foreach (AbilityConfig abilityConfig in currentLevelAbilities)
        {
            if (abilityConfig.row != csvRow)
            {
                continue;
            }

            if (abilityConfig.column >= gridColumns)
            {
                continue;
            }

            GameObject selectedPrefab = GetPrefabForAbility(abilityConfig.abilityType);

            if (selectedPrefab == null)
            {
                continue;
            }

            float x = cachedFirstCellX + abilityConfig.column * (cachedCellWidth + horizontalSpacing);
            float y = cachedFirstCellY - visualRow * (cachedCellHeight + verticalSpacing);

            GameObject newAbility = Instantiate(
                selectedPrefab,
                new Vector3(x, y, 0f),
                Quaternion.identity,
                abilitiesParent
            );

            if (resizeAbilityToCellSize)
            {
                ResizeAbilityToCellSize(newAbility, cachedCellWidth, cachedCellHeight);
            }

            newAbility.name =
                abilityConfig.abilityType + "_" +
                levelForName + "_" +
                abilityConfig.row + "_" +
                abilityConfig.column;

            ConfigureSpawnedAbility(newAbility, abilityConfig);

            spawnedAbilities++;
        }

        return spawnedAbilities;
    }

    private bool PrepareGrid()
    {
        if (!TryGetWalls(out Collider2D leftWall, out Collider2D rightWall, out Collider2D topWall))
        {
            return false;
        }

        if (!TryGetCellSize(out cachedCellWidth, out cachedCellHeight))
        {
            return false;
        }

        float gridWidth =
            gridColumns * cachedCellWidth +
            (gridColumns - 1) * horizontalSpacing;

        float availableWidth =
            rightWall.bounds.min.x - leftWall.bounds.max.x;

        if (gridWidth > availableWidth)
        {
            return false;
        }

        cachedFirstCellX =
            (leftWall.bounds.max.x + rightWall.bounds.min.x) / 2f -
            gridWidth / 2f +
            cachedCellWidth / 2f +
            horizontalOffset;

        cachedFirstCellY =
            topWall.bounds.min.y -
            distanceFromTopWall -
            cachedCellHeight / 2f;

        hasCachedGrid = true;

        return true;
    }

    private int GetMaxRow(List<AbilityConfig> abilities)
    {
        int maxRow = -1;

        foreach (AbilityConfig abilityConfig in abilities)
        {
            maxRow = Mathf.Max(maxRow, abilityConfig.row);
        }

        return maxRow;
    }
}