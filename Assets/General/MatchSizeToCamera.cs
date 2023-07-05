using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSizeToCamera : MonoBehaviour
{

    public Camera cam;

    private void Update()
    {
        float height = cam.orthographicSize * 2f;
        float aspectRatio = Screen.width / (float)Screen.height;
        Vector3 localScale = new Vector3(height * aspectRatio, height, 1f);

        transform.localScale = localScale;
    }
}
