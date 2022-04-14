using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ManaPowers : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] float projectileSpeed = 80f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform instantiatePosition;
    [SerializeField] float projectileCD = 2f;
    [Header("Explosion")]
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] float explosionDuration;
    [SerializeField] float explosionFinalScale;
    [SerializeField] Vector3 explosionInitialPosition;
    [SerializeField] float explosionCD;
    [Header("Shield")]
    [SerializeField] GameObject shieldPrefab;
    [SerializeField] Vector3 shieldInitialPosition;
    [SerializeField] float shieldDuration;
    [SerializeField] float shieldCD;

    bool projectileUsable = true;
    bool explosionUsable = true;
    bool shieldUsable = true;

    public void Projectile(InputAction.CallbackContext context)
    {
        if(!context.performed || !projectileUsable) return;

        GameObject projectile = Instantiate(projectilePrefab, instantiatePosition.position, Quaternion.identity);
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        projectileScript.instantiatorID = gameObject.GetInstanceID();
        projectileScript.SetMovement(transform.forward, projectileSpeed);
        projectileUsable = false;
        StartCoroutine(ReEnableProjectile());
    }

    IEnumerator ReEnableProjectile()
    {
        yield return new WaitForSeconds(projectileCD);
        projectileUsable = true;
    }

    public void Explosion(InputAction.CallbackContext context)
    {
        if (!context.performed || !explosionUsable) return;

        GameObject projectile = Instantiate(explosionPrefab, explosionInitialPosition, Quaternion.identity);
        Explosion explosionScript = projectile.GetComponent<Explosion>();
        explosionScript.instantiatorTransform = transform;
        explosionScript.positionOffset = explosionInitialPosition;
        explosionScript.duration = explosionDuration;
        explosionScript.finalScale = explosionFinalScale;
        explosionUsable = false;
        StartCoroutine(ReEnableExplosion());
    }

    IEnumerator ReEnableExplosion()
    {
        yield return new WaitForSeconds(explosionCD);
        explosionUsable = true;
    }

    public void Shield(InputAction.CallbackContext context)
    {
        if (!context.performed || !shieldUsable) return;

        GameObject shield = Instantiate(shieldPrefab, transform.TransformDirection(shieldInitialPosition), Quaternion.identity);
        Shield shieldScript = shield.GetComponent<Shield>();
        shieldScript.instantiatorTransform = transform;
        shieldScript.positionOffset = shieldInitialPosition;
        shieldScript.SetDestruction(shieldDuration);
        shieldUsable = false;
        StartCoroutine(ReEnableShield());
    }

    IEnumerator ReEnableShield()
    {
        yield return new WaitForSeconds(shieldCD);
        shieldUsable = true;
    }
}
