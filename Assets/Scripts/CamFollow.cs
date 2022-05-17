using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform cameraTarget;
    public float sSpeed = 10.0f;
    public Vector3 dist; 
    public Transform lookTarget;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dPos = cameraTarget.position + dist;
        Vector3 sPos = Vector3.Lerp(transform.position, dPos, sSpeed * Time.deltaTime); 
        transform.position = sPos;
        transform.LookAt(lookTarget.position);
    }
}
