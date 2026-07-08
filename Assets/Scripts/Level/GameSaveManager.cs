using UnityEngine;

public static class GameSaveManager
{
    private const string SavedGameKey = "SavedGameData";
    private const string HasSavedGameKey = "HasSavedGameData";

    private const string SavedBallAttackKey = "SavedBallAttackStrength";

    public static void SaveGame(SavedGameData savedGame)
    {
        if (savedGame == null)
        {
            return;
        }

        PlayerPrefs.SetString(SavedGameKey, JsonUtility.ToJson(savedGame));
        PlayerPrefs.SetInt(HasSavedGameKey, 1);
        PlayerPrefs.Save();
    }

    public static bool TryLoadGame(out SavedGameData savedGame)
    {
        savedGame = null;

        if (PlayerPrefs.GetInt(HasSavedGameKey, 0) != 1 || !PlayerPrefs.HasKey(SavedGameKey))
        {
            return false;
        }

        savedGame = JsonUtility.FromJson<SavedGameData>(PlayerPrefs.GetString(SavedGameKey));

        if (savedGame == null ||
            savedGame.level < 1 ||
            savedGame.ball == null ||
            savedGame.bricks == null ||
            savedGame.bricks.Count == 0)
        {
            ClearSavedGame();
            savedGame = null;
            return false;
        }

        return true;
    }

    public static void ClearSavedGame()
    {
        PlayerPrefs.DeleteKey(SavedGameKey);
        PlayerPrefs.DeleteKey(HasSavedGameKey);
        PlayerPrefs.Save();
    }

    public static void SaveBallAttackStrength(int attackStrength)
    {
        PlayerPrefs.SetInt(SavedBallAttackKey, Mathf.Max(1, attackStrength));
        PlayerPrefs.Save();
    }

    public static int LoadBallAttackStrength()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(SavedBallAttackKey, 1));
    }

    public static void ClearBallAttackStrength()
    {
        PlayerPrefs.DeleteKey(SavedBallAttackKey);
        PlayerPrefs.Save();
    }
}