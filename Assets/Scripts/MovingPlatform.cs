using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float timeUntilDirectionChange;
    [SerializeField] private float speed;

    private float direction;
    private float changeDirectionTimer;

    private void Start()
    {
        if (timeUntilDirectionChange != 0)
            changeDirectionTimer = timeUntilDirectionChange;
        direction = 1f;
        rb = this.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (timeUntilDirectionChange != 0)
        {
            rb.velocity = new Vector2(direction * speed, rb.velocity.y);

            if (changeDirectionTimer <= 0)
            {
                direction *= -1f;
                changeDirectionTimer = timeUntilDirectionChange;
            }
        }

        if (changeDirectionTimer > 0)
            changeDirectionTimer -= Time.deltaTime;
    }

    // Makes the player move with the platform
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 deltaVelocity = rb.velocity - collision.rigidbody.velocity;
            collision.gameObject.GetComponent<PlayerMovement>().AddMovement(deltaVelocity);
        }
    }
}
