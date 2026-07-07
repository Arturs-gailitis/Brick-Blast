using System;
using System.Collections.Generic;

[Serializable]
public class SavedGameData
{
    public int level;
    public int score;
    public SavedBallData ball;
    public List<SavedBrickData> bricks = new List<SavedBrickData>();
}