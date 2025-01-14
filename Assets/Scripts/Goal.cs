using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Goal : MonoBehaviour
{
    // Bool is here to try and enforce only running this code once per player
    private bool once;

    private void Start()
    {
        once = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && once)
        {
            once = false;
            ulong clientId = collision.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId; // Grab ID of the player who reached the goal

            // Run code in MultiPlayerManager for a player winning
            MultiPlayerManager.instance.PlayerWon(clientId, collision.gameObject);
            MultiPlayerManager.instance.PlayerWonGlobal();

            gameObject.SetActive(false); // Disable the goal to not cause any re-colliding problems 
        }
    }
}
