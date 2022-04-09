using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckGrounding : MonoBehaviour
{
    public bool isGrounded;
    public bool wasGroundedLastFrame;
    public bool castInLocalSpace;
    [SerializeField] LayerMask collisionLayers;

    [Header("RayCast")]
    [SerializeField] bool useRayCasting;
    [SerializeField] List<Vector3> castsInitPos = new List<Vector3>();
    [SerializeField] float raySize;
    [Header("Box")]
    [SerializeField] bool useBoxCast;
    [SerializeField] List <Vector3> boxCenters = new List<Vector3>();
    [SerializeField] List <Vector3> boxScales = new List<Vector3>();
    [Header("Debug")]
    [SerializeField] bool showGizmos = true;
    [SerializeField] bool showDebugRay = true;
    [SerializeField] bool alwaysGrounded;

    bool groundingCheck;
    bool sceneGroundingCheck;

    private void Start()
    {
        if (boxScales.Count == 1)
        {
            for (int i = 1; i < boxCenters.Count; i++)
            {
                boxScales.Add(boxScales[0]);
            }
        }
    }

    void FixedUpdate()
    {

        groundingCheck = false;

        if (useRayCasting)
            RayCastingGrounding();

        if (useBoxCast)
        {
            BoxCheckGrounding();
        }

        wasGroundedLastFrame = isGrounded;
        isGrounded = groundingCheck;
        if (alwaysGrounded) isGrounded = true;
    }

    private void BoxCheckGrounding()
    {
        for (int i = 0; i < boxCenters.Count; i++)
        {
            if (Physics.CheckBox(transform.TransformDirection(boxCenters[i]) + transform.position, boxScales[i]/2, transform.rotation, ~collisionLayers))
            {
                groundingCheck = true;
                sceneGroundingCheck = true;
                if (showDebugRay) Debug.DrawRay(transform.TransformDirection(boxCenters[i]) + transform.position, transform.TransformDirection(Vector3.down) * boxScales[i].y/2, Color.cyan);
            }
            else
                if (showDebugRay) Debug.DrawRay(transform.TransformDirection(boxCenters[i]) + transform.position, transform.TransformDirection(Vector3.down) * boxScales[i].y/2, Color.grey);

        }
    }
    Vector3 rayDirection;
    Vector3 rayOrigin;
    private void RayCastingGrounding()
    {
        rayDirection = Vector3.down;
        if (castInLocalSpace) rayDirection = transform.TransformDirection(Vector3.down);

        for (int i = 0; i < castsInitPos.Count; i++)
        {
            rayOrigin = transform.TransformDirection(castsInitPos[i]) + transform.position;
            if (Physics.Raycast(rayOrigin, rayDirection, raySize, ~collisionLayers))
            {
                groundingCheck = true;
                if (showDebugRay) Debug.DrawRay(rayOrigin, rayDirection * raySize, Color.blue);
            }
            else
                if (showDebugRay) Debug.DrawRay(rayOrigin, rayDirection * raySize, Color.yellow);
        }
    }

    void OnDrawGizmosSelected()
    {
        sceneGroundingCheck = false;
        if (showDebugRay || showGizmos) BoxCheckGrounding();
        if (!showGizmos) return;

        Gizmos.color = Color.grey;

        if (useRayCasting)
        {
            Vector3 direction = Vector3.down;
            if (castInLocalSpace) direction = transform.TransformDirection(Vector3.down);
            Gizmos.color = Color.grey;
            for (int i = 0; i < castsInitPos.Count; i++)
            {
                Vector3 origin = transform.TransformDirection(castsInitPos[i]) + transform.position;
                Gizmos.DrawRay(origin, direction * raySize);
            }     
        }

        if (useBoxCast)
        {            
            Gizmos.matrix = transform.localToWorldMatrix;

            for (int i = 0; i < boxCenters.Count; i++)
            {
                Vector3 gizmoBoxCenter = boxCenters[i];
                
                if (sceneGroundingCheck) Gizmos.color = Color.blue;

                Gizmos.DrawWireCube(gizmoBoxCenter, boxScales[i]);
            }
        }
    }
}
