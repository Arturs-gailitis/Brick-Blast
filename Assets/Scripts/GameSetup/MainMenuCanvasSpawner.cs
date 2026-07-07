using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuCanvasSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject mainMenuCanvasPrefab;

    [Header("Scene settings")]
    [SerializeField] private string mainMenuSceneName;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            return;
        }

        if (mainMenuCanvasPrefab == null)
        {
            return;
        }

        GameObject existingCanvas = GameObject.Find("MainMenuCanvas");

        if (existingCanvas != null)
        {
            existingCanvas.SetActive(true);
            return;
        }

        Instantiate(mainMenuCanvasPrefab);
    }
}