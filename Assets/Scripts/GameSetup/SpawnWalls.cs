using UnityEngine;

public class SpawnWalls : MonoBehaviour
{
    [SerializeField] private GameObject[] WallPrefab;

    private void Awake()
    {
        for (int i = 0; i < WallPrefab.Length; i++)
        {
            GameObject wall = Instantiate(WallPrefab[i]);

            wall.name = WallPrefab[i].name;
        }

        Physics2D.SyncTransforms();
    }
}