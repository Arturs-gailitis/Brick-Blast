using System;
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

            if (values.Length < 6)
            {
                continue;
            }

            bool levelIsValid = int.TryParse(
                values[0].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int level
            );

            bool rowIsValid = int.TryParse(
                values[1].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int row
            );

            bool columnIsValid = int.TryParse(
                values[2].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int column
            );

            bool hitPointsAreValid = int.TryParse(
                values[3].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int hitPoints
            );

            bool scoreIsValid = int.TryParse(
                values[4].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int score
            );

            bool numbersAreValid = levelIsValid && rowIsValid && columnIsValid && hitPointsAreValid && scoreIsValid;

            if (!numbersAreValid)
            {
                continue;
            }

            allBricks.Add(new BrickConfig
            {
                level = level,
                row = row,
                column = column,
                hitPoints = Mathf.Max(1, hitPoints),
                score = Mathf.Max(0, score),
                blockType = values[5].Trim()
            });
        }
    }
}