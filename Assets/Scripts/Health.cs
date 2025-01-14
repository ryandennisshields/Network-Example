using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public NetworkVariable<int> health = new NetworkVariable<int>(0);
    [SerializeField] private int baseHealth;

    [SerializeField] private TextMesh currentHealth;
    [SerializeField] private AudioClip hurtSound;

    private float invincibleTimer;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            health.Value = baseHealth;
        currentHealth.text = health.Value.ToString();
    }

    // Function for modifying health accessible by other code
    public void HealthModify(int change)
    {
        UpdateHealthServerRpc(change);
    }

    [ServerRpc(RequireOwnership = false)] // Allow any client to request this ServerRpc
    public void UpdateHealthServerRpc(int change)
    {
        if (invincibleTimer <= 0)
        {
            health.Value += change;
            invincibleTimer = 2;

            UpdateHealthClientRpc(health.Value);
        }

        // Logic specifically for a player
        if (gameObject.CompareTag("Player") && health.Value <= 0)
        {
            ulong clientID = gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;
            if (MultiPlayerManager.instance.arenaActive)
                MultiPlayerManager.instance.PlayerWonArenaClientRpc(clientID);
            else
                MultiPlayerManager.instance.PlayerDiedClientRpc(clientID);
        }
        else if (health.Value <= 0)
        {
            GetComponent<NetworkObject>().Despawn(true);
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void UpdateHealthClientRpc(int newHealth)
    {
        currentHealth.text = newHealth.ToString();
        AudioSource.PlayClipAtPoint(hurtSound, transform.position);
    }

    private void Update()
    {
        if (invincibleTimer > 0)
            invincibleTimer -= Time.deltaTime;
    }
}
