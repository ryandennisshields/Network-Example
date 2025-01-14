using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0);
    public NetworkVariable<float> time = new NetworkVariable<float>(0);

    public TextMesh currentScore;

    [SerializeField] AudioClip pickUpSound;

    // Timer to enforce only getting one point per pickup
    private float pickupTimer;

    public override void OnNetworkSpawn()
    {
        currentScore.text = "P" + (OwnerClientId + 1).ToString() + ": " + score.Value.ToString();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Gem") == true || collision.gameObject.CompareTag("Cherries") == true)
        {
            if (collision.gameObject.CompareTag("Gem"))
                UpdateScoreServerRpc(1);
            if (collision.gameObject.CompareTag("Cherries"))
                UpdateScoreServerRpc(2);
            if (IsServer)
                collision.gameObject.GetComponent<NetworkObject>().Despawn(true);
            Destroy(collision.gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)] // Allow any client to request this ServerRpc
    public void UpdateScoreServerRpc(int scoreGained)
    {
        if (pickupTimer <= 0)
        {
            score.Value += scoreGained;
            pickupTimer = 0.5f;

            UpdateScoreClientRpc(score.Value);
        }
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(int newScore)
    {
        currentScore.text = "P" + (OwnerClientId + 1).ToString() + ": " + newScore.ToString();

        AudioSource.PlayClipAtPoint(pickUpSound, transform.position);
    }

    private void Update()
    {
        if (IsOwner)
        UpdateTimeServerRpc();

        if (pickupTimer > 0)
            pickupTimer -= Time.deltaTime;
    }

    [ServerRpc]
    public void UpdateTimeServerRpc()
    {
        time.Value += Time.deltaTime;
    }
}