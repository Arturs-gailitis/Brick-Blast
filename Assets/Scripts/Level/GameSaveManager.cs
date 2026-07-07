using UnityEngine;

public static class GameSaveManager
{
    private const string SavedGameKey = "SavedGameData";
    private const string HasSavedGameKey = "HasSavedGameData";

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
}