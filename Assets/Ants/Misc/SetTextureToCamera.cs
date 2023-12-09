using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTextureToCamera : MonoBehaviour
{
    public AntSimulationController antSimulationController; // Reference to the AntSimulationController

    private void Start()
    {
        // Check if AntSimulationController is assigned
        if (antSimulationController == null)
        {
            Debug.LogError("AntSimulationController is not assigned!");
            return;
        }

        // Start capturing the webcam feed
        StartWebcamFeed();
    }


    private void StartWebcamFeed()
    {
        // Create a WebCamTexture
        WebCamTexture webcamTexture = new WebCamTexture();

        // Start the webcam
        webcamTexture.Play();

        // Check if the webcam is available and playing
        if (webcamTexture.isPlaying)
        {
            // Set the webcam texture as the biasTexture in AntSimulationController
            antSimulationController.biasTexture = webcamTexture;
        }
        else
        {
            Debug.LogError("Webcam is not available or not playing.");
        }
    }
}