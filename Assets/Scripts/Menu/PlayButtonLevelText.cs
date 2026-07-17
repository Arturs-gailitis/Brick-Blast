using TMPro;
using UnityEngine;

public class PlayButtonLevelText : MonoBehaviour
{
    [SerializeField] private TMP_Text playButtonText;

    [Header("Button text")]
    [SerializeField] private string defaultText = "Play";
    [SerializeField] private string savedGameTextFormat = "Continue Level {0}";

    private void Awake()
    {
        if (playButtonText == null)
        {
            playButtonText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        RefreshText();
    }

    public void RefreshText()
    {
        if (playButtonText == null)
        {
            return;
        }

        if (GameSaveManager.TryLoadGame(out SavedGameData savedGame))
        {
            playButtonText.text = string.Format(savedGameTextFormat, savedGame.level);
        }
        else
        {
            playButtonText.text = defaultText;
        }
    }
}