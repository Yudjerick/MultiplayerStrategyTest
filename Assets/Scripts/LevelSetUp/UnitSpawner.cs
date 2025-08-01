using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] private List<UnitSpawnSetting> unitSpawnSettings;
    [SerializeField] private float intersectionCheckRadius;
    [Header("Spawning zone")]
    [SerializeField] private float rightPlayerMinSpawnX;
    [SerializeField] private float rightPlayerMaxSpawnX;
    [SerializeField] private float minSpawnZ;
    [SerializeField] private float maxSpawnZ;
    [SerializeField] private float spawnY;
    [Header("Players colors")]
    [SerializeField] private Color rightPlayerColor;
    [SerializeField] private Color leftPlayerColor;

    public void Spawn(ulong rightPlayerId, ulong leftPlayerId)
    {
        SpawnUnitsForPlayer(rightPlayerId, true);
        SpawnUnitsForPlayer(leftPlayerId, false);
    }

    private void SpawnUnitsForPlayer(ulong playerId, bool rightSide)
    {
        float maxX = rightSide ? rightPlayerMaxSpawnX : -rightPlayerMinSpawnX;
        float minX = rightSide ? rightPlayerMinSpawnX : -rightPlayerMaxSpawnX;
        Color spawnColor = rightSide ? rightPlayerColor : leftPlayerColor;
        foreach (UnitSpawnSetting setting in unitSpawnSettings)
        {
            for (int i = 0; i < setting.count; i++)
            {
                
                Vector3 spawnPosition = new Vector3(Random.Range(minX, maxX), spawnY, Random.Range(minSpawnZ, maxSpawnZ));
                while (Physics.OverlapSphere(spawnPosition, intersectionCheckRadius).Length > 0)
                {
                    spawnPosition = new Vector3(Random.Range(minX, maxX), spawnY, Random.Range(minSpawnZ, maxSpawnZ));
                }
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var instance = Instantiate(setting.unit, spawnPosition, spawnRotation);
                instance.OwnerId.Value = playerId;
                instance.Color.Value = spawnColor;
                instance.NetworkObject.Spawn();
            }
        }
    }

    [Serializable]
    class UnitSpawnSetting
    {
        public Unit unit;
        public int count;
    }
}
