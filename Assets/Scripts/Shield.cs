using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [HideInInspector] public Transform instantiatorTransform;
    [HideInInspector] public Vector3 positionOffset;

    public void SetDestruction(float duration)
    {
        Destroy(gameObject, duration);
    }

    void Update()
    {
        transform.position = instantiatorTransform.position + instantiatorTransform.TransformDirection(positionOffset);        
    }
}
