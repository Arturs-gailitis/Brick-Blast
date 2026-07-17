using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private OptionsUI optionsUI;

    [Header("Buttons")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Button backButton;

    [Header("Text")]
    [SerializeField] private TMP_Text muteButtonText;

    public bool IsVisible => settingsPanel != null && settingsPanel.activeSelf;

    private void Awake()
    {
        if (muteButton != null)
        {
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(ToggleSoundEffects);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToOptions);
        }

        RefreshMuteButtonText();
        HideSettings();
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        RefreshMuteButtonText();
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void BackToOptions()
    {
        HideSettings();

        if (optionsUI != null)
        {
            optionsUI.ShowOptions();
        }
    }

    private void ToggleSoundEffects()
    {
        SoundSettingsManager.ToggleMute();
        RefreshMuteButtonText();
    }

    private void RefreshMuteButtonText()
    {
        if (muteButtonText == null)
        {
            return;
        }

        muteButtonText.text = SoundSettingsManager.IsMuted ? "Unmute Sound" : "Mute Sound";
    }
}