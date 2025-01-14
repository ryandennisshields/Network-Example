using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioListener audioListener;

    public Camera camera1;
    public Camera camera2;

    [SerializeField] private float speed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float wallJumpSpeed;
    [SerializeField] private float wallJumpHeight;
    [SerializeField] private float wallSlideSpeed;
    [SerializeField] private float wallJumpMovementCooldown;
    [SerializeField] private float decelerationRate;
    private float jumpTimer;
    private float wallJumpTimer;

    private Collider2D ground;
    private Collider2D wall;
    [SerializeField] private BoxCollider2D groundCollider;
    [SerializeField] private BoxCollider2D wallCollider;
    private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isOnWall = new NetworkVariable<bool>(false);

    [SerializeField] private GameObject projectile;

    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isFalling = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip fireSound;

    public override void OnNetworkSpawn()
    {
        rb = this.GetComponent<Rigidbody2D>();
        animator = this.GetComponent<Animator>();
        spriteRenderer = this.GetComponent<SpriteRenderer>();
        audioListener = this.GetComponent<AudioListener>();
        // Stuff for player 1 and 2
        if (OwnerClientId == 0)
        {
            MultiPlayerManager.instance.players.Add(this.gameObject);
            transform.position = new Vector3(0f, 0f, 0f);
            camera1.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z - 15);
            Destroy(camera2.gameObject);
        }
        else if (OwnerClientId == 1)
        {
            MultiPlayerManager.instance.players.Add(this.gameObject);
            transform.position = new Vector3(0f, -250f, 0f);
            camera2.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z - 15);
            Destroy(camera1.gameObject);
        }
        // In case someone else tries to join
        else
        {
            GetComponent<NetworkObject>().Despawn(true);
            Destroy(gameObject);
        }

        audioListener.enabled = IsOwner;
    }

    // "AddMovement" stuff is for when another object should change the player's velocity (for example, moving platforms)
    public void AddMovement(Vector2 velocity)
    {
        HandleAddMovementServerRpc(velocity);
    }

    [ServerRpc]
    private void HandleAddMovementServerRpc(Vector2 velocity)
    {
        HandleAddMovementClientRpc(velocity);
    }

    [ClientRpc]
    private void HandleAddMovementClientRpc(Vector2 velocity)
    {
        rb.velocity = new Vector2(velocity.x + rb.velocity.x, rb.velocity.y);
    }

    [ServerRpc]
    private void HandleMovementServerRpc(string movementdirection)
    {
        HandleMovementClientRpc(movementdirection);
    }

    [ClientRpc]
    private void HandleMovementClientRpc(string movementdirection)
    {
        switch (movementdirection)
        {
            case "right":
                rb.velocity = new Vector2(speed, rb.velocity.y);
                spriteRenderer.flipX = false;
                break;
            case "left":
                rb.velocity = new Vector2(-speed, rb.velocity.y);
                spriteRenderer.flipX = true;
                break;
            case "decelerateRight":
                rb.velocity = new Vector2(rb.velocity.x - decelerationRate, rb.velocity.y);
                if (rb.velocity.x < 0)
                    rb.velocity = new Vector2(0, rb.velocity.y);
                break;
            case "decelerateLeft":
                rb.velocity = new Vector2(rb.velocity.x + decelerationRate, rb.velocity.y);
                if (rb.velocity.x > 0)
                    rb.velocity = new Vector2(0, rb.velocity.y);
                break;
            case "jump":
                rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
                AudioSource.PlayClipAtPoint(jumpSound, transform.position);
                break;
            case "walljump":
                if (wall.transform.position.x < transform.position.x)
                    rb.velocity = new Vector2(wallJumpSpeed, wallJumpHeight);
                else
                    rb.velocity = new Vector2(-wallJumpSpeed, wallJumpHeight);
                AudioSource.PlayClipAtPoint(jumpSound, transform.position);
                break;
            case "wallSlide":
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
                break;
        }
    }

    [ServerRpc]
    private void CreateProjectileServerRpc(float positionx, float positiony, float positionz, Quaternion vector3rotation)
    {
        // The spriteRenderer.flipX code allow easy checking for if the player is facing left or right, so we can spawn the projectile depending on what way the player faces
        int spawnOffset = 3;
        if (spriteRenderer.flipX)
            spawnOffset = -spawnOffset;

        GameObject spawnedObject = Instantiate(projectile, new Vector3(positionx + spawnOffset, positiony, positionz), vector3rotation);
        if (spriteRenderer.flipX)
            spawnedObject.GetComponent<ProjectileMovement>().projectileSpeed = -spawnedObject.GetComponent<ProjectileMovement>().projectileSpeed;

        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        networkObject.Spawn(true);
        CreateProjectileClientRpc(networkObject.NetworkObjectId, OwnerClientId);
    }

    // This sets the colour of the projectiles for clients and makes the firing sound
    [ClientRpc]
    private void CreateProjectileClientRpc(ulong objectID, ulong clientID)
    {
        NetworkObject spawnedObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectID];

        if (spawnedObject != null)
        {
            SpriteRenderer spriteRenderer = spawnedObject.GetComponent<SpriteRenderer>();
            switch (clientID)
            {
                case 0:
                    spriteRenderer.color = Color.red;
                    break;
                case 1:
                    spriteRenderer.color = Color.blue;
                    break;
                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
        }

        AudioSource.PlayClipAtPoint(fireSound, transform.position);
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            // Left and Right movement code
            if (Input.GetKey(KeyCode.D) && IsOwner && wallJumpTimer <= 0)
                HandleMovementServerRpc("right");
            if (Input.GetKey(KeyCode.A) && IsOwner && wallJumpTimer <= 0)
                HandleMovementServerRpc("left");

            // Deceleration code
            if (!Input.GetKey(KeyCode.D) && rb.velocity.x > 0 && wallJumpTimer <= 0 && !isGrounded.Value)
                HandleMovementServerRpc("decelerateRight");
            if (!Input.GetKey(KeyCode.A) && rb.velocity.x < 0 && wallJumpTimer <= 0 && !isGrounded.Value)
                HandleMovementServerRpc("decelerateLeft");
        }
    }

    private void Update()
    {
        // Jump code
        if (Input.GetKey(KeyCode.Space) && isGrounded.Value)
        {
            if (IsOwner && jumpTimer <= 0)
            {
                HandleMovementServerRpc("jump");
                jumpTimer = jumpCooldown;
            }
        }

        // Walljump code
        if (Input.GetKey(KeyCode.Space) && isOnWall.Value)
        {
            if (IsOwner && jumpTimer <= 0)
            {
                HandleMovementServerRpc("walljump");
                jumpTimer = jumpCooldown;
                wallJumpTimer = wallJumpMovementCooldown;
            }
        }

        if (IsOwner)
        {
            // Bunch of code for setting animation states
            bool isJumping = !isGrounded.Value && rb.velocity.y >= 0;
            bool isFalling = !isGrounded.Value && rb.velocity.y < 0;
            bool isMoving = isGrounded.Value && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));

            UpdatePlayerStateServerRpc(isJumping, isFalling, isMoving);

            // Wall Slide code
            if (isOnWall.Value && !isGrounded.Value && rb.velocity.y < 0)
                HandleMovementServerRpc("wallSlide");
        }

        animator.SetBool("isJumping", isJumping.Value);
        animator.SetBool("isFalling", isFalling.Value);
        animator.SetBool("isMoving", isMoving.Value);

        if (Input.GetKeyDown(KeyCode.R) && IsOwner)
            CreateProjectileServerRpc(transform.position.x, transform.position.y, transform.position.z, transform.rotation);

        if (jumpTimer > 0)
            jumpTimer -= Time.deltaTime;
        if (wallJumpTimer > 0)
            wallJumpTimer -= Time.deltaTime;

        if (NetworkManager.Singleton.IsServer && ground != null)
            isGrounded.Value = groundCollider.IsTouching(ground);
        if (NetworkManager.Singleton.IsServer && wall != null)
            isOnWall.Value = wallCollider.IsTouching(wall);
    }

    // Even with this and the above code, animations are still buggy :(
    [ServerRpc]
    private void UpdatePlayerStateServerRpc(bool isJumpingValue, bool isFallingValue, bool isMovingValue)
    {
        isJumping.Value = isJumpingValue;
        isFalling.Value = isFallingValue;
        isMoving.Value = isMovingValue;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // For some reason, this can be checking for stuff as the player is destroyed, causing a null reference exception
        if (gameObject != null)
        {
            if (collision.gameObject.CompareTag("Ground"))
                ground = collision;
            if (collision.gameObject.CompareTag("Wall"))
                wall = collision;
        }
    }
}
