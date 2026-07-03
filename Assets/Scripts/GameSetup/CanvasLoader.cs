using UnityEngine;

public class CanvasLoader : MonoBehaviour
{
    [Header("Canvas prefab")]
    [SerializeField] private GameObject CanvasPrefab;

    private void Awake()
    {
        if (CanvasPrefab == null)
        {
            return;
        }

        Instantiate(CanvasPrefab);
    }
}