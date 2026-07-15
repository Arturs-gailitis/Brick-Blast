using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AbilityConfigReader : MonoBehaviour
{
    [Header("CSV file")]
    [SerializeField] private TextAsset abilityConfigCsv;

    private readonly List<AbilityConfig> allAbilities = new List<AbilityConfig>();
    private bool isLoaded;

    public List<AbilityConfig> GetAbilitiesForLevel(int selectedLevel)
    {
        LoadCsvIfNeeded();

        List<AbilityConfig> levelAbilities = new List<AbilityConfig>();

        foreach (AbilityConfig ability in allAbilities)
        {
            if (ability.level == selectedLevel)
            {
                levelAbilities.Add(ability);
            }
        }

        return levelAbilities;
    }

    private void LoadCsvIfNeeded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;

        if (abilityConfigCsv == null)
        {
            return;
        }

        string[] lines = abilityConfigCsv.text.Replace("\r", string.Empty).Split('\n');

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

            bool levelIsValid = int.TryParse(values[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int level);

            bool rowIsValid = int.TryParse(values[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int row);

            bool columnIsValid = int.TryParse(values[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int column);

            string abilityType = values[3].Trim();

            bool valueIsValid = int.TryParse(values[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int value);

            bool aimIsValid = int.TryParse(values[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int aim);

            bool rowDataIsValid = levelIsValid && rowIsValid && columnIsValid && valueIsValid && aimIsValid &&
                !string.IsNullOrWhiteSpace(abilityType);

            if (!rowDataIsValid)
            {
                continue;
            }

            allAbilities.Add(new AbilityConfig
            {
                level = Mathf.Max(1, level), row = Mathf.Max(0, row), column = Mathf.Max(0, column),
                abilityType = abilityType, value = value, aim = aim
            });
        }
    }
}