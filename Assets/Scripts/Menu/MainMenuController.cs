using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string SceneName;

    [Header("Progress")]
    [SerializeField] private bool resetProgressAfterGameOver;

    [Header("Sound")]
    [SerializeField] private UIButtonClickSound buttonClickSound;
    [SerializeField] [Min(0f)] private float sceneLoadDelay;

    private Button button;
    private bool isLoading;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonClickSound == null)
        {
            buttonClickSound = GetComponent<UIButtonClickSound>();
        }
    }

    private void Start()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.enabled = false;
        }
    }

    public void SwitchGame()
    {
        if (isLoading || string.IsNullOrWhiteSpace(SceneName))
        {
            return;
        }

        StartCoroutine(LoadSceneAfterSound());
    }

    private IEnumerator LoadSceneAfterSound()
    {
        isLoading = true;

        if (buttonClickSound != null)
        {
            buttonClickSound.OnPointerClick(null);
        }

        if (button != null)
        {
            button.interactable = false;
        }

        yield return new WaitForSecondsRealtime(sceneLoadDelay);

        if (resetProgressAfterGameOver && LevelManager.Instance != null && LevelManager.Instance.IsGameOver)
        {
            LevelManager.ResetSavedProgress();
            ScoreManager.ResetSavedScore();
        }

        SceneManager.LoadScene(SceneName);
    }
}