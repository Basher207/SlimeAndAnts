using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float shiftMoveSpeed = 30f;
    public MarchingCubesRunner marchingCubesRunner;
    public float scaleOffset = 0f;
    
    private void Start()
    {
    }
    
    float GetScale(Vector3 atPosition)
    {
        float distanceFromCenter = atPosition.magnitude;
        float normalisedDistance = distanceFromCenter / marchingCubesRunner.radius;
        
        return normalisedDistance - 1f + scaleOffset;
    }
    
    void Update()
    {
        float upDownMovement = (Input.GetKey(KeyCode.Q) ? -1 : 0) + (Input.GetKey(KeyCode.E) ? 1 : 0);
        
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), upDownMovement , Input.GetAxis("Vertical"));
        movement = transform.TransformDirection(movement);
        float speedMulti = Input.GetKey(KeyCode.LeftShift) ? shiftMoveSpeed : moveSpeed;
        

        transform.position += movement * speedMulti * Time.deltaTime;
        // transform.localScale = Vector3.one * GetScale(transform.position);
    }
}
