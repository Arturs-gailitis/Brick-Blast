using UnityEngine;

public class SpawnWalls : MonoBehaviour
{
    [SerializeField] private GameObject [] WallPrefab;


    private void Start()
    {
        for (var i = 0; i < WallPrefab.Length; i++)
        {
            Instantiate(WallPrefab[i]);
        }
    }
}