using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetFloorNormal : MonoBehaviour
{
    public Vector3 Normal;
    public Vector3 FrameNormal;
    public Vector3 FrameNormalSum;
    public Vector3 NormalTimeAveraged;
    public bool AllRaysHit;
    public bool CastInLocalSpace;

    [Header("Casting Rays")]
    [SerializeField] List<Vector2> castsInitPos = new List<Vector2>();
    [SerializeField] float yOffset;
    [SerializeField] float size;
    [SerializeField] LayerMask layersIgnored;
    [Header("Averaging Over Time")]
    [SerializeField] bool getAverageOverTime = true;
    [Range(1,100)][SerializeField] int sampleSize = 5;
    [SerializeField] float timeBetweenSamples = 0.01f;
    [SerializeField] Vector3 initializingVector = Vector3.up;
    [SerializeField] bool useDefaultIfNotAllHit = true;
    [SerializeField] Vector3 directionWhenNotAllHit = Vector3.up;
    [Header("Debugging")]
    [SerializeField] Vector3 originDebugNormal;
    [SerializeField] bool showDebugRays;

    List<Vector3> frameNormals = new List<Vector3>();
    Vector3[] allNormals;
    private int normalIndex;

    private void Start()
    {
        foreach(Vector2 pos in castsInitPos)
        {
            frameNormals.Add(Vector3.zero);
        }

        allNormals = new Vector3[sampleSize];
        for (int i = 0; i < allNormals.Length; i++)
        {
            allNormals[i] = initializingVector;
        }

        StartCoroutine(GetAverageOverTime());
    }

    void Update()
    {
        GetFrameNormal();

        Normal = getAverageOverTime ? NormalTimeAveraged : FrameNormal;
    }

    private void GetFrameNormal()
    {
        for (int i = 0; i < frameNormals.Count; i++)
        {
            frameNormals[i] = Vector3.zero;
        }

        AllRaysHit = true;
        Vector3 direction = Vector3.down;
        if (CastInLocalSpace) direction = transform.TransformDirection(Vector3.down);
        for (int i = 0; i < castsInitPos.Count; ++i)
        {
            RaycastHit hit;
            Vector3 Origin = transform.TransformDirection(new Vector3(castsInitPos[i].x, yOffset, castsInitPos[i].y)) + transform.position;
            if (Physics.Raycast(Origin, direction, out hit, size, ~layersIgnored))
                frameNormals[i] = hit.normal;
            else
                AllRaysHit = false;

            if (showDebugRays) Debug.DrawRay(Origin, direction * size, Color.red);
        }

        FrameNormalSum = Vector3.zero;
        foreach (Vector3 normal in frameNormals)
        {
            FrameNormalSum += normal;
        }

        FrameNormal = FrameNormalSum.normalized;

        if (showDebugRays) Debug.DrawRay(originDebugNormal + transform.position, Normal, Color.green);
    }

    IEnumerator GetAverageOverTime()
    {
        while(getAverageOverTime)
        {
            if (normalIndex >= allNormals.Length) normalIndex = 0;

            Vector3 normalsSum = directionWhenNotAllHit;

            if (!AllRaysHit && useDefaultIfNotAllHit)
                allNormals[normalIndex] = directionWhenNotAllHit;
            else
            {
                allNormals[normalIndex] = FrameNormal;
            }
            normalIndex++;

            foreach (Vector3 normal in allNormals)
            {
                normalsSum += normal;
            }
            NormalTimeAveraged = normalsSum.normalized;

            yield return new WaitForSeconds(timeBetweenSamples);
        }        
    }
   

    void OnDrawGizmosSelected()
    {
        Vector3 direction = Vector3.down;
        if (CastInLocalSpace) direction = transform.TransformDirection(Vector3.down);
        Gizmos.color = Color.red;
        for (int i = 0; i < castsInitPos.Count; ++i)
        {
            Vector3 Origin = transform.TransformDirection(new Vector3(castsInitPos[i].x, yOffset, castsInitPos[i].y)) + transform.position;
            Gizmos.DrawRay(Origin, direction * size);
        }
    }
}
