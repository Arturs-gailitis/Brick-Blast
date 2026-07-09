using System;
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
    [SerializeField] private float horizontalOffset = 0.25f;

    [Header("Ability size")]
    [SerializeField] private bool resizeAbilityToCellSize = true;
    [SerializeField] [Range(0.1f, 1f)] private float abilitySizeMultiplier = 0.9f;

    private Transform abilitiesParent;

    private void Awake()
    {
        GameObject parentObject = new GameObject("Abilities");
        abilitiesParent = parentObject.transform;
    }

    public int SpawnLevel(int level)
    {
        ClearLasers();

        if (abilityConfigReader == null)
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

        List<AbilityConfig> levelAbilities = abilityConfigReader.GetAbilitiesForLevel(level);

        if (levelAbilities.Count == 0)
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

        int spawnedAbilities = 0;

        foreach (AbilityConfig abilityConfig in levelAbilities)
        {
            if (abilityConfig.column >= gridColumns)
            {
                continue;
            }

            GameObject selectedPrefab = GetPrefabForAbility(abilityConfig.abilityType);

            if (selectedPrefab == null)
            {
                continue;
            }

            float x = firstCellX + abilityConfig.column * (cellWidth + horizontalSpacing);
            float y = firstCellY - abilityConfig.row * (cellHeight + verticalSpacing);

            GameObject newAbility = Instantiate(
                selectedPrefab,
                new Vector3(x, y, 0f),
                Quaternion.identity,
                abilitiesParent
            );

            if (resizeAbilityToCellSize)
            {
                ResizeAbilityToCellSize(newAbility, cellWidth, cellHeight);
            }

            newAbility.name =
                abilityConfig.abilityType + "_" +
                level + "_" +
                abilityConfig.row + "_" +
                abilityConfig.column;

            ConfigureSpawnedAbility(newAbility, abilityConfig);

            spawnedAbilities++;
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

    public void MoveAllLasersDown(float distance)
    {
        MoveAllAbilitiesDown(distance);
    }

    public void MoveAllAbilitiesDown(float distance)
    {
        if (abilitiesParent == null)
        {
            return;
        }

        for (int i = 0; i < abilitiesParent.childCount; i++)
        {
            abilitiesParent.GetChild(i).position += Vector3.down * distance;
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
}