using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AntSimulationController : MonoBehaviour
{
    
    
    
    [System.Serializable]
    public class ShaderVariable
    {
        [Header("Name of variable in shader")]
        public string name;
        
        public float value;
        
        [Header("Only info:")]
        public string comment;
    }
    
    
    [Header("Variables that will be sent to the shader")]
    public List<ShaderVariable> shaderVariables;
    
    // [Header("Shader functions that will be called every frame")]
    // public List<ShaderFunctions> shaderFunctions;
    
    
    public float targetFrameRate = 60f;
    public int textureHeight = 2160;
    
    [Header("Our ant behaviour shader")]
    public ComputeShader computeShader;

    [Header("The material we want to set our texture on")]
    public Material materialToApplyTextureOn;


    [Header("Dont change at runtime")]
    public int numberOfAnts = 10000;

    public Texture biasTexture;
    
    private ComputeBuffer antBuffer;
    private RenderTexture trailMap;
    
    private Ant [] ants;
    private float currentAspectRatio;
    
    
    void Start() {
        // Initialize the trail map

        // NEW SHIT
        SocketServer.OnDataReceived += UpdateShaderVariables;

        ReBuildTextureIfNeeded();

        currentAspectRatio = Screen.width / (float)Screen.height;
        
        // Initialize the ant buffer
        ants = new Ant[numberOfAnts];
        for (int i = 0; i < numberOfAnts; i++)
        {
            Vector2 position = new Vector2(currentAspectRatio * 0.5f, 0.5f) + Random.insideUnitCircle * 0.05f;
            float angle = Random.Range(-Mathf.PI, Mathf.PI);
            
            ants[i] = new Ant {
                position = position,
                angle = angle
            };
        }
        antBuffer = new ComputeBuffer(numberOfAnts, sizeof(float) * 3);
        antBuffer.SetData(ants);
        
        StartCoroutine(UpdateAnts());
    }

    private void ReBuildTextureIfNeeded()
    {
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        if (aspectRatio.Equals(currentAspectRatio))
            return;

        if (trailMap)
        {
            Destroy(trailMap);
        }
        currentAspectRatio = aspectRatio;
        
        int resolutionWidth = Mathf.RoundToInt(textureHeight * aspectRatio);
        
        trailMap = new RenderTexture(resolutionWidth, 2160, 0, RenderTextureFormat.ARGBFloat);
        trailMap.enableRandomWrite = true;
        trailMap.wrapMode = TextureWrapMode.Repeat;
        trailMap.Create();
        materialToApplyTextureOn.mainTexture = trailMap;
    }

    private string textureToLoad;
    void UpdateShaderVariables(List<SocketServer.Item> data)
    {
        Debug.Log(data);
        // Loop through the received data and update shaderVariables
        foreach (var item in data)
        {
            if (!string.IsNullOrEmpty(item.stringValue)) {
                //if biasTexture
                if (item.name == "biasTexture")
                {
                    textureToLoad = item.stringValue;
                }
            } else {
                Debug.Log(item);
                foreach (var shaderVar in shaderVariables)
                {
                    Debug.Log(item.numberValue);
                    if (shaderVar.name == item.name)
                    {
                        Debug.Log(shaderVar.name);
                        shaderVar.value = item.numberValue;
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(textureToLoad))
        {
            // Load the image from file/url path
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes((string)textureToLoad));
            biasTexture = texture;

            textureToLoad = null;
        }
        ReBuildTextureIfNeeded();
    }

    //Our ant update loop!
    //This is done as a "Coroutine" (Not important) to make sure we run a specific frame rate
    IEnumerator UpdateAnts() {
        while (true)
        {
            CallShaderFunctions();
            yield return new WaitForSeconds(1f / targetFrameRate);
        }
    }

    void CallShaderFunctions()
    {
        //Assign the shader variables
        for (int i = 0; i < shaderVariables.Count; i++)
        {
            ShaderVariable shaderVariable = shaderVariables[i];
            computeShader.SetFloat(shaderVariable.name, shaderVariable.value);
        }
        
        //General variables
        computeShader.SetFloat("time", 1f / targetFrameRate);
        computeShader.SetInt("numberOfAnts", numberOfAnts);
        computeShader.SetInts("textureSize", new int[2] { trailMap.width, trailMap.height });
        computeShader.SetVector("positionRange",  new Vector2((float)trailMap.width / (float)trailMap.height, 1f));
        computeShader.SetFloat("aspectRatio", (float)trailMap.width / (float)trailMap.height);
        
        
        if (biasTexture)
        {
            computeShader.SetInts("biasTextureSize", new int[2] { biasTexture.width, biasTexture.height});
        }

        // Vector2 mousePos = Input.mousePosition / (float)Screen.height;
        // if (mousePos.x < 0f || mousePos.x > currentAspectRatio || mousePos.y < 0f || mousePos.y > 1f)
        // {
        //     mousePos = new Vector2((float)trailMap.width / (float)trailMap.height, 1f) / 2f;
        // }
            
        //computeShader.SetVector("mousePos", mousePos);
        // computeShader.SetVector("mousePos", new Vector4(currentAspectRatio * 0.5f, 0.5f));

        
        int updateAntsKernel = computeShader.FindKernel("UpdateAnts");
        int drawAntsKernel = computeShader.FindKernel("DrawAnts");
        int textureDissipationKernel = computeShader.FindKernel("TextureDissipation");

        UpdateComputeShaderKernelVariables(updateAntsKernel);
        UpdateComputeShaderKernelVariables(drawAntsKernel);
        UpdateComputeShaderKernelVariables(textureDissipationKernel);
            
        computeShader.Dispatch(updateAntsKernel, numberOfAnts / 64, 1, 1);
        computeShader.Dispatch(drawAntsKernel, numberOfAnts / 64, 1, 1);
        computeShader.Dispatch(textureDissipationKernel, trailMap.width / 8, trailMap.height / 8, 1);
    }

    void UpdateComputeShaderKernelVariables(int kernelIndex)
    {
        if (biasTexture)
        {
            computeShader.SetTexture(kernelIndex, "biasTexture", biasTexture);
        }
        
        computeShader.SetTexture(kernelIndex, "trailMap", trailMap);
        computeShader.SetBuffer(kernelIndex, "antBuffer", antBuffer);
    }

    void OnDestroy() {
        antBuffer.Release();
        trailMap.Release();
        SocketServer.OnDataReceived -= UpdateShaderVariables;
    }

    private struct Ant {
        public Vector2 position;
        public float angle;
    }
}