using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovementOld : NetworkBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    float direction;

    [SerializeField] float speed;
    [SerializeField] float jumpHeight;
    bool isGrounded;

    public int theScore;

    public override void OnNetworkSpawn()
    {

        rb = this.GetComponent<Rigidbody2D>();
        animator = this.GetComponent<Animator>();
        direction = this.transform.localScale.x;
        theScore = 0;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            rb.velocity = new Vector2(Input.GetAxis("Horizontal") * speed, rb.velocity.y);
            if (Input.GetAxis("Horizontal") == 0 && isGrounded)
                animator.SetBool("isMoving", false);
            if (Input.GetAxis("Horizontal") > 0)
            {
                transform.localScale = new Vector3(direction, transform.localScale.y, transform.localScale.z);
                if (isGrounded)
                    animator.SetBool("isMoving", true);
                else
                    animator.SetBool("isMoving", false);
            }
            if (Input.GetAxis("Horizontal") < 0)
            {
                transform.localScale = new Vector3(-direction, transform.localScale.y, transform.localScale.z);
                if (isGrounded)
                    animator.SetBool("isMoving", true);
                else
                    animator.SetBool("isMoving", false);
            }
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
                isGrounded = false;
                animator.SetBool("isJumping", true);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
            if (collision.gameObject.tag == "Ground")
            {
                isGrounded = true;
                animator.SetBool("isJumping", false);
            }
    }
}
