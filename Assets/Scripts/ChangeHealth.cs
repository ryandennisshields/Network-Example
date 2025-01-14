using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChangeHealth : MonoBehaviour
{
    [SerializeField] private int healthChangeAmount;
    [SerializeField] private bool destroyOnCollision;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (NetworkManager.Singleton.IsServer) // Host handles the code for consistency between players
        { 
            Health health = collision.gameObject.GetComponent<Health>();
            if (health)
                health.HealthModify(healthChangeAmount);
            if (destroyOnCollision && !collision.gameObject.CompareTag("Projectile")) // Ignoring Projectile tag is there so projectiles don't destroy each other
            {
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (!networkObject)
                networkObject.Despawn(true);
                Destroy(gameObject);
            }
        }
    }
}
