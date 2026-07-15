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
    [SerializeField] private AbilityConfigReader abilityConfigReader;
    [SerializeField] private GameObject brickPrefab;

    [Header("Walls")]
    [SerializeField] private Transform[] walls;

    [Header("Grid settings")]
    [SerializeField] [Min(1)] private int gridColumns = 7;
    [SerializeField] private float horizontalSpacing;
    [SerializeField] private float verticalSpacing;
    [SerializeField] [Min(0f)] private float distanceFromSideWalls = 0.08f;
    [SerializeField] private bool fitGridBetweenWalls = true;

    [Header("Individual ability position")]
    [SerializeField] [Min(0f)] private float laserDistanceFromTopWall;
    [SerializeField] private float laserHorizontalOffset;
    [SerializeField] [Min(0f)] private float powerDistanceFromTopWall;
    [SerializeField] private float powerHorizontalOffset;
    [SerializeField] [Min(0f)] private float directionDistanceFromTopWall;
    [SerializeField] private float directionHorizontalOffset;

    [Header("Ability size")]
    [SerializeField] private bool resizeAbilityToCellSize = true;
    [SerializeField] [Range(0.1f, 1f)] private float laserSizeMultiplier = 0.9f;
    [SerializeField] [Range(0.1f, 1f)] private float powerSizeMultiplier = 0.7f;
    [SerializeField] [Range(0.1f, 1f)] private float directionSizeMultiplier = 0.7f;

    [Header("Rows behind top wall")]
    [SerializeField] [Min(0.001f)] private float hiddenRowZOffset = 0.05f;
    [SerializeField] [Min(0f)] private float revealPositionTolerance = 0.001f;

    private Transform abilitiesParent;

    private List<AbilityConfig> currentLevelAbilities = new List<AbilityConfig>();
    private int nextRowToSpawn;
    private int maxRowInLevel = -1;
    private int selectedLevel = 1;

    private float cachedFirstCellX;
    private float cachedTopWallBottomY;
    private float cachedCellWidth;
    private float cachedCellHeight;
    private float cachedTopWallZ;
    private float cachedTopWallTopY;
    private int cachedTopWallSortingLayerId;
    private int cachedTopWallSortingOrder;
    private bool hasCachedGrid;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Abilities");
        abilitiesParent = parentObject.transform;
    }

    public int SpawnLevel(int level, int visibleRowsAtStart)
    {
        ClearAbilities();

        selectedLevel = level;
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

        int visibleRowCount = Mathf.Min(Mathf.Max(1, visibleRowsAtStart), maxRowInLevel + 1);

        int spawnedAbilities = 0;

        for (int csvRow = 0; csvRow <= maxRowInLevel; csvRow++)
        {
            int visualRow = visibleRowCount - 1 - csvRow;

            bool startsHidden = csvRow >= visibleRowCount;

            spawnedAbilities += SpawnRow(csvRow, visualRow, selectedLevel, startsHidden);
        }

        nextRowToSpawn = maxRowInLevel + 1;

        RefreshRowDepths();

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

                abilityTransforms[i].position = Vector3.Lerp(startPositions[i], targetPositions[i], movePercent);
            }

            RefreshRowDepths();

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

        RefreshRowDepths();
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

    private void ResizeAbilityToCellSize(GameObject abilityObject, string abilityType, float cellWidth, float cellHeight)
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

        float sizeMultiplier = laserSizeMultiplier;

        if (string.Equals(abilityType, "power", StringComparison.OrdinalIgnoreCase))
        {
            sizeMultiplier = powerSizeMultiplier;
        }
        else if (string.Equals(abilityType, "direction", StringComparison.OrdinalIgnoreCase))
        {
            sizeMultiplier = directionSizeMultiplier;
        }

        float targetSize = Mathf.Min(cellWidth, cellHeight) * sizeMultiplier;

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

            SavedAbilityData savedAbility = null;

            LaserAbility laserAbility = abilityTransform.GetComponent<LaserAbility>();

            if (laserAbility != null)
            {
                savedAbility = laserAbility.CreateSaveData();
            }
            else
            {
                PowerAbility powerAbility = abilityTransform.GetComponent<PowerAbility>();

                if (powerAbility != null)
                {
                    savedAbility = powerAbility.CreateSaveData();
                }
                else
                {
                    DirectionAbility directionAbility = abilityTransform.GetComponent<DirectionAbility>();

                    if (directionAbility != null)
                    {
                        savedAbility = directionAbility.CreateSaveData();
                    }
                }
            }

            if (savedAbility == null)
            {
                continue;
            }

            HiddenRowDepth hiddenRowDepth = abilityTransform .GetComponent<HiddenRowDepth>();

            savedAbility.isHidden = hiddenRowDepth != null && hiddenRowDepth.IsHidden;

            savedAbilities.Add(savedAbility);
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

        if (!hasCachedGrid && !PrepareGrid())
        {
            return 0;
        }

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

            float originalZ = selectedPrefab.transform.position.z;

            GameObject newAbility = Instantiate(selectedPrefab, new Vector3(savedAbility.x, savedAbility.y, originalZ),
                Quaternion.identity, abilitiesParent);

            if (resizeAbilityToCellSize)
            {
                ResizeAbilityToCellSize(newAbility, savedAbility.abilityType, cachedCellWidth, cachedCellHeight);
            }

            newAbility.name = "Saved_" + savedAbility.abilityType + "_" + spawnedAbilities;

            AbilityConfig abilityConfig = new AbilityConfig{level = 1, row = 0, column = 0,
                abilityType = savedAbility.abilityType, value = savedAbility.value, aim = savedAbility.aim};

            ConfigureSpawnedAbility(newAbility, abilityConfig);

            SpriteRenderer spriteRenderer = newAbility.GetComponent<SpriteRenderer>();

            float pivotOffsetY = 0f;

            if (spriteRenderer != null)
            {
                pivotOffsetY = newAbility.transform.position.y - spriteRenderer.bounds.center.y;
            }

            float firstCellY = GetFirstCellYForAbility(savedAbility.abilityType);

            float revealY = firstCellY + pivotOffsetY;

            AddHiddenRowDepth(newAbility, originalZ, revealY, savedAbility.isHidden);

            spawnedAbilities++;
        }

        RefreshRowDepths();

        return spawnedAbilities;
    }

    private bool TryGetCellSize(out float cellWidth, out float cellHeight)
    {
        cellWidth = 0f;
        cellHeight = 0f;

        if (brickPrefab == null)
        {
            return false;
        }

        SpriteRenderer brickRenderer = brickPrefab.GetComponent<SpriteRenderer>();

        if (brickRenderer == null || brickRenderer.sprite == null)
        {
            return false;
        }

        Vector2 spriteSize = brickRenderer.sprite.bounds.size;

        cellWidth = spriteSize.x * Mathf.Abs(brickPrefab.transform.localScale.x);

        cellHeight = spriteSize.y * Mathf.Abs(brickPrefab.transform.localScale.y);

        return cellWidth > 0f && cellHeight > 0f;
    }

    private int SpawnRow(int csvRow, int visualRow, int levelForName, bool startsHidden)
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

            float x = cachedFirstCellX + abilityConfig.column * (cachedCellWidth + horizontalSpacing) +
                GetHorizontalOffsetForAbility(abilityConfig.abilityType);

            float firstCellY = GetFirstCellYForAbility(abilityConfig.abilityType);

            float y = firstCellY - visualRow * (cachedCellHeight + verticalSpacing);

            float originalZ = selectedPrefab.transform.position.z;

            Vector3 gridPosition = new Vector3(x, y, originalZ);

            GameObject newAbility = Instantiate(selectedPrefab, gridPosition, Quaternion.identity, abilitiesParent);

            if (resizeAbilityToCellSize)
            {
                ResizeAbilityToCellSize(newAbility, abilityConfig.abilityType, cachedCellWidth, cachedCellHeight);
            }

            CenterAbilityOnGridPosition(newAbility, gridPosition);

            float rowObjectOffsetY = newAbility.transform.position.y - gridPosition.y;

            float revealY = firstCellY + rowObjectOffsetY;

            newAbility.name = abilityConfig.abilityType + "_" + levelForName + "_" + abilityConfig.row + "_" +
                abilityConfig.column;

            ConfigureSpawnedAbility(newAbility, abilityConfig);

            AddHiddenRowDepth(newAbility, originalZ, revealY, startsHidden);

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

        float gridWidth = gridColumns * cachedCellWidth + (gridColumns - 1) * horizontalSpacing;

        float availableWidth = rightWall.bounds.min.x - leftWall.bounds.max.x - distanceFromSideWalls * 2f;

        if (availableWidth <= 0f)
        {
            return false;
        }

        if (fitGridBetweenWalls)
        {
            cachedCellWidth = (availableWidth - (gridColumns - 1) * horizontalSpacing) / gridColumns;
        }

        if (gridWidth > availableWidth)
        {
            return false;
        }

        cachedFirstCellX = leftWall.bounds.max.x + distanceFromSideWalls + cachedCellWidth / 2f;

        cachedTopWallBottomY = topWall.bounds.min.y;
        
        cachedTopWallZ = topWall.transform.position.z;

        cachedTopWallTopY = topWall.bounds.max.y;

        Renderer topWallRenderer = topWall.GetComponent<Renderer>();

        if (topWallRenderer == null)
        {
            topWallRenderer = topWall.GetComponentInChildren<Renderer>();
        }

        if (topWallRenderer != null)
        {
            cachedTopWallSortingLayerId = topWallRenderer.sortingLayerID;

            cachedTopWallSortingOrder = topWallRenderer.sortingOrder;
        }
        else
        {
            cachedTopWallSortingLayerId = 0;
            cachedTopWallSortingOrder = 0;
        }

        hasCachedGrid = true;

        return true;
    }

    private void CenterAbilityOnGridPosition(GameObject abilityObject, Vector3 gridPosition)
    {
        if (abilityObject == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = abilityObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        Vector3 spriteCenter = spriteRenderer.bounds.center;
        Vector3 centerOffset = gridPosition - spriteCenter;

        abilityObject.transform.position += centerOffset;
    }

    private float GetDistanceFromTopWallForAbility(string abilityType)
    {
        if (string.Equals(abilityType, "power", StringComparison.OrdinalIgnoreCase))
        {
            return powerDistanceFromTopWall;
        }

        if (string.Equals(abilityType, "direction", StringComparison.OrdinalIgnoreCase))
        {
            return directionDistanceFromTopWall;
        }

        return laserDistanceFromTopWall;
    }

    private float GetHorizontalOffsetForAbility(string abilityType)
    {
        if (string.Equals(abilityType, "power", StringComparison.OrdinalIgnoreCase))
        {
            return powerHorizontalOffset;
        }

        if (string.Equals(abilityType, "direction", StringComparison.OrdinalIgnoreCase))
        {
            return directionHorizontalOffset;
        }

        return laserHorizontalOffset;
    }

    private float GetFirstCellYForAbility(string abilityType)
    {
        return cachedTopWallBottomY - GetDistanceFromTopWallForAbility(abilityType) - cachedCellHeight / 2f;
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

    public bool PrepareSavedLevel(int level, int nextRow)
    {
        selectedLevel = level;
        nextRowToSpawn = Mathf.Max(0, nextRow);
        maxRowInLevel = -1;
        hasCachedGrid = false;

        if (abilityConfigReader == null)
        {
            return false;
        }

        currentLevelAbilities = abilityConfigReader.GetAbilitiesForLevel(level);

        if (currentLevelAbilities == null || currentLevelAbilities.Count == 0)
        {
            return false;
        }

        if (!PrepareGrid())
        {
            return false;
        }

        maxRowInLevel = GetMaxRow(currentLevelAbilities);

        nextRowToSpawn = maxRowInLevel + 1;

        return true;
    }

    public void RefreshRowDepths()
    {
        if (abilitiesParent == null)
        {
            return;
        }

        HiddenRowDepth[] hiddenRows = abilitiesParent.GetComponentsInChildren<HiddenRowDepth>(true);

        foreach (HiddenRowDepth hiddenRow in hiddenRows)
        {
            if (hiddenRow != null)
            {
                hiddenRow.RefreshVisibility();
            }
        }
    }

    private void AddHiddenRowDepth(GameObject abilityObject, float originalZ, float revealY, bool startsHidden)
    {
        if (abilityObject == null)
        {
            return;
        }

        HiddenRowDepth hiddenRowDepth = abilityObject.GetComponent<HiddenRowDepth>();

        if (hiddenRowDepth == null)
        {
            hiddenRowDepth = abilityObject.AddComponent<HiddenRowDepth>();
        }

        float hiddenZ = Mathf.Max(cachedTopWallZ + hiddenRowZOffset, originalZ + hiddenRowZOffset);

        hiddenRowDepth.Initialize(originalZ, hiddenZ, revealY, cachedTopWallTopY, startsHidden,
            revealPositionTolerance, cachedTopWallSortingLayerId, cachedTopWallSortingOrder - 1);
}

    public int GetNextRowToSpawn()
    {
        return nextRowToSpawn;
    }
}