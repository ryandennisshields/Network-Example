using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileMovement : NetworkBehaviour
{
    public float projectileSpeed;

    private void FixedUpdate()
    {
        transform.position += new Vector3(projectileSpeed * Time.deltaTime, 0f, 0f);
    }
}
