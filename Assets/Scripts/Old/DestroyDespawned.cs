using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestroyDespawned : NetworkBehaviour
{
    // This small Script destroys any network despawned objects locally when a new client connects
    // Not really necessary for the most recent build

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientID)
    {
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject networkObject in networkObjects)
        {
            if (!networkObject.IsSpawned)
                Destroy(networkObject.gameObject);
        }
    }
}
