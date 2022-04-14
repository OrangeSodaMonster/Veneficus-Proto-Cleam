using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [HideInInspector] public Transform instantiatorTransform;
    [HideInInspector] public Vector3 positionOffset;
    [HideInInspector] public float duration;
    [HideInInspector] public float finalScale;

    float totalTime;
    private void Update()
    {
        transform.position = instantiatorTransform.position + instantiatorTransform.TransformDirection(positionOffset);

        transform.localScale = Vector3.Lerp(Vector3.zero, finalScale * Vector3.one, totalTime/duration);
        totalTime += Time.deltaTime;

        if(totalTime > duration + .1f) Destroy(gameObject);
    }
}
