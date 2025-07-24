using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private List<ObstacleSpawnSetting> obstacleSettings;
    [Header("Spawning zone")]
    [SerializeField] private float minSpawnX;
    [SerializeField] private float maxSpawnX;
    [SerializeField] private float minSpawnZ;
    [SerializeField] private float maxSpawnZ;
    [SerializeField] private float spawnY;

    private GameObject _generatedEnviroment;

    public void Generate(int seed)
    {
        Random.InitState(seed);
        _generatedEnviroment = new GameObject("GeneratedEnviroment");
        foreach (var obstacleSetting in obstacleSettings)
        {
            var count = Random.Range(obstacleSetting.minimumCount, obstacleSetting.maximumCount + 1);
            for(int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = new Vector3(Random.Range(minSpawnX, maxSpawnX), spawnY, Random.Range(minSpawnZ, maxSpawnZ));
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Instantiate(obstacleSetting.prefab, spawnPosition, spawnRotation, _generatedEnviroment.transform);
            }
        }
        navMeshSurface.BuildNavMesh();
    }

    public void DestroyGenerated()
    {
        Destroy(_generatedEnviroment);
    }

    [Serializable]
    class ObstacleSpawnSetting
    {
        public GameObject prefab;
        public int minimumCount;
        public int maximumCount;
    }

}
