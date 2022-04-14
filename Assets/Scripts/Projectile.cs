using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public int instantiatorID;
    [SerializeField] GameObject explosionVFX;

    public void SetMovement(Vector3 direction, float speed)
    {
        direction = direction.normalized;
        GetComponent<Rigidbody>().velocity = speed * direction;
        Destroy(gameObject, 30);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetInstanceID() != instantiatorID)
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<SphereCollider>().enabled = false;
            GameObject instance = Instantiate(explosionVFX, collision.GetContact(0).point, Quaternion.identity);
            Destroy(instance, 1);
            Destroy(gameObject, 1);
        }
    }

}
