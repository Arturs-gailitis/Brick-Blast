using System;
using System.Collections.Generic;

[Serializable]
public class SavedGameData
{
    public int level;
    public int score;

    public SavedBallData ball;

    public List<SavedBrickData> bricks = new List<SavedBrickData>();

    public bool abilitiesWereSaved;

    public List<SavedAbilityData> abilities = new List<SavedAbilityData>();

    public int nextBrickRowToSpawn;
    public int nextAbilityRowToSpawn;
    public int downMoveCounter;
}