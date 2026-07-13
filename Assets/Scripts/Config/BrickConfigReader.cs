using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class BrickConfigReader : MonoBehaviour
{
    [Header("CSV file")]
    [SerializeField] private TextAsset brickConfigCsv;

    private readonly List<BrickConfig> allBricks = new List<BrickConfig>();

    private bool isLoaded;

    public List<BrickConfig> GetBricksForLevel(int selectedLevel)
    {
        LoadCsvIfNeeded();

        List<BrickConfig> levelBricks = new List<BrickConfig>();

        foreach (BrickConfig brick in allBricks)
        {
            if (brick.level == selectedLevel)
            {
                levelBricks.Add(brick);
            }
        }

        return levelBricks;
    }

    public void ReloadCsv()
    {
        isLoaded = false;
        allBricks.Clear();

        LoadCsvIfNeeded();
    }

    private void LoadCsvIfNeeded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        allBricks.Clear();

        if (brickConfigCsv == null)
        {
            return;
        }

        string[] lines = brickConfigCsv.text.Replace("\r", string.Empty).Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] values = line.Split(',');

            if (values.Length < 7)
            {
                continue;
            }

            bool levelIsValid = TryReadInt(values[0], out int level);

            bool rowIsValid = TryReadInt(values[1], out int row);

            bool columnIsValid = TryReadInt(values[2], out int column);

            bool hitPointsAreValid = TryReadInt(values[3], out int hitPoints);

            bool scoreIsValid = TryReadInt(values[4], out int score);

            bool rotationIsValid = TryReadInt(values[6], out int rotation);

            bool numbersAreValid = levelIsValid && rowIsValid && columnIsValid && hitPointsAreValid && scoreIsValid 
                && rotationIsValid;

            if (!numbersAreValid)
            {
                continue;
            }

            string blockType = values[5].Trim().ToLowerInvariant();

            if (blockType != "full" && blockType != "half")
            {

                blockType = "full";
            }

            allBricks.Add(new BrickConfig
                {
                    level = Mathf.Max(1, level),
                    row = Mathf.Max(0, row),
                    column = Mathf.Max(0, column),
                    hitPoints = Mathf.Max(1, hitPoints),
                    score = Mathf.Max(0, score),
                    blockType = blockType,
                    rotation = NormalizeRotation(rotation)
                }
            );
        }
    }

    private bool TryReadInt(string value, out int result)
    {
        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private int NormalizeRotation(int value)
    {
        int normalizedRotation = ((value % 360) + 360) % 360;

        if (normalizedRotation == 90 || normalizedRotation == 180 || normalizedRotation == 270)
        {
            return normalizedRotation;
        }

        return 0;
    }
}