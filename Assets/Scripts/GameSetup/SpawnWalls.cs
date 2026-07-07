using UnityEngine;

public class SpawnWalls : MonoBehaviour
{
    [SerializeField] private GameObject[] WallPrefab;

    private void Awake()
    {
        for (int i = 0; i < WallPrefab.Length; i++)
        {
            Instantiate(WallPrefab[i]);
        }
    }
}