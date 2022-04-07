using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpSlerpTest : MonoBehaviour
{
    [SerializeField] bool rotate;
    [SerializeField] float rotatingTime;
    [SerializeField] float degresSecond;
    [Range(1,3)] [SerializeField] int RotTow_Slerp_Lerp;
    [SerializeField] bool calculateInFixed;
    [SerializeField] bool resetPosition;
    [SerializeField] Transform initRot;
    [SerializeField] Transform endRot;
    [Header("Gizmos")]
    [SerializeField] bool showGizmos;
    [SerializeField] bool slerp;
    [SerializeField] Vector3 initialDirection;
    [SerializeField] Vector3 EndingDirection;
    [SerializeField] float mainRaySize;
    [SerializeField] float subRaysSize;

    float timePassed;
    Vector3 currentDirection;
    Quaternion targetRotation;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

            if (resetPosition)
        {
            currentDirection = initialDirection;
            timePassed = 0f;
        }

        if(rotate && !slerp) LerpDirection();
        else if(rotate && slerp) SlerpDirection();

        
        Gizmos.DrawRay(transform.position, initialDirection * subRaysSize);
        Gizmos.DrawRay(transform.position, EndingDirection * subRaysSize);
        Gizmos.DrawRay(transform.position, currentDirection * mainRaySize);        
    }

    private void Update()
    {
        if (!rotate) return;
        if (resetPosition)
        {
            targetRotation = initRot.rotation;
            timePassed = 0;
        }


        if (calculateInFixed) return;

        if(RotTow_Slerp_Lerp == 1)
            targetRotation = Quaternion.RotateTowards(transform.rotation, endRot.rotation, degresSecond * Time.deltaTime);
        if (RotTow_Slerp_Lerp == 2)
            targetRotation = Quaternion.Slerp(initRot.rotation, endRot.rotation, timePassed/rotatingTime);
        if (RotTow_Slerp_Lerp == 3)
            targetRotation = Quaternion.Lerp(initRot.rotation, endRot.rotation, timePassed/rotatingTime);

        timePassed += Time.deltaTime;

        rb.MoveRotation(targetRotation.normalized);

        //Debug.Log("");
    }

    private void FixedUpdate()
    {
        if (!rotate) return;

        if (calculateInFixed)
        {

            if (RotTow_Slerp_Lerp == 1)
                targetRotation = Quaternion.RotateTowards(transform.rotation, endRot.rotation, degresSecond * Time.fixedDeltaTime);
            if (RotTow_Slerp_Lerp == 2)
                targetRotation = Quaternion.Slerp(initRot.rotation, endRot.rotation, timePassed/rotatingTime);
            if (RotTow_Slerp_Lerp == 3)
                targetRotation = Quaternion.Lerp(initRot.rotation, endRot.rotation, timePassed/rotatingTime);

            timePassed += Time.fixedDeltaTime;

            rb.MoveRotation(targetRotation.normalized);
        }       
    }

    void SlerpDirection()
    {
        currentDirection = Vector3.Slerp(initialDirection, EndingDirection, timePassed/rotatingTime);
        timePassed += Time.deltaTime;
    }

    void LerpDirection()
    {
        currentDirection = Vector3.Lerp(initialDirection, EndingDirection, timePassed/rotatingTime);
        timePassed += Time.deltaTime;
    }
}
