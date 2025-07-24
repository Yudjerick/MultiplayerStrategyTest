using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random; 

public class ConnectionEventsHandler : NetworkBehaviour
{
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] UnitSpawner unitSpawner;
    [SerializeField] private GameObject uiCanvas;

    private void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvent;
    }

    private void HandleConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if(data.EventType == ConnectionEvent.ClientConnected)
        {
            HandleClientConnected();
        }
        else if(data.EventType == ConnectionEvent.ClientDisconnected)
        {
            HandleClientDisconnected();
        }
    }

    private void HandleClientConnected()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
            if (clientIds.Count == 2)
            {
                StartGameClientRpc(Random.Range(int.MinValue, int.MaxValue));
                unitSpawner.Spawn(clientIds[0], clientIds[1]);
                PlayerActionController controller = PlayerActionController.Singleton;
                controller.CurrentPlayerId.Value = clientIds[0];
                controller.NextPlayerId.Value = clientIds[1];
                controller.StartTimerServerRpc();
            }
        }
    }

    private void HandleClientDisconnected()
    {
        levelGenerator.DestroyGenerated();
        uiCanvas.SetActive(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartGameClientRpc(int seed)
    {
        levelGenerator.Generate(seed);
        uiCanvas.SetActive(true);
    }
}
