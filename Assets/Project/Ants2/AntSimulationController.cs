using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    // [System.Serializable]
    // public class ShaderFunctions
    // {
    //     [Header("Name of variable in shader")]
    //     public string name;
    //     
    //     
    //     [Header("(ignore) How is this function being grouped when called")]
    //     public Vector3Int threadGroupDivider;
    //
    //     public bool xThreadGroup = true;
    //     public bool yThreadGroup = true;
    //     public bool zThreadGroup = false;
    //
    //     public int xThreadGroupSize => xThreadGroup ? threadGroupDivider.x : 1;
    //     public int yThreadGroupSize => yThreadGroup ? threadGroupDivider.y : 1;
    //     public int zThreadGroupSize => zThreadGroup ? threadGroupDivider.z : 1;
    //     
    //     [Header("Only info:")]
    //     public string comment;
    // }
    
    
    
    [Header("Variables that will be sent to the shader")]
    public List<ShaderVariable> shaderVariables;
    
    // [Header("Shader functions that will be called every frame")]
    // public List<ShaderFunctions> shaderFunctions;
    
    
    public float targetFrameRate = 60f;
    
    
    [Header("Our ant behaviour shader")]
    public ComputeShader computeShader;

    [Header("The material we want to set our texture on")]
    public Material materialToApplyTextureOn;



    public int numberOfAnts = 1000;
    

    
    
    
    private ComputeBuffer antBuffer;
    private RenderTexture trailMap;
    
    private Ant [] ants;
    
    
    void Start() {
        // Initialize the trail map
        trailMap = new RenderTexture(2160, 2160, 0, RenderTextureFormat.ARGBFloat);
        trailMap.enableRandomWrite = true;
        trailMap.Create();
        materialToApplyTextureOn.mainTexture = trailMap;
        
        // Initialize the ant buffer
        ants = new Ant[numberOfAnts];
        for (int i = 0; i < numberOfAnts; i++)
        {
            Vector2 position = new Vector2(0.5f, 0.5f) + Random.insideUnitCircle * 0.05f;
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

    
    //Our ant update loop!
    //This is done as a "Coroutine" (Not important) to make sure we run a specific frame rate
    IEnumerator UpdateAnts() {
        while (true)
        {
            CallShaderFunctions();
            
            // antBehaviorShader.SetFloat("time", 1f / targetFrameRate);
            //
            // antBehaviorShader.SetFloat("sensorAngle", sensorAngle);
            // antBehaviorShader.SetFloat("sensorScale", sensorScale);
            // antBehaviorShader.SetFloat("rotationSpeed", rotationSpeed);
            // antBehaviorShader.SetFloat("moveSpeed", moveSpeed);
            //
            // antBehaviorShader.SetFloat("repulseScale", repulseScale);
            // antBehaviorShader.SetFloat("attractionScale", attractionScale);
            // antBehaviorShader.SetFloat("sensorDistance", sensorDistance);
            //
            // antBehaviorShader.SetInt("numAnts", currentNumberOfAnts);
            // antBehaviorShader.SetInts("textureSize", new int[2] { trailMap.width, trailMap.height });
            // antBehaviorShader.Dispatch(antBehaviorKernel, currentNumberOfAnts / 64, 1, 1);
            //
            // // Execute the draw ants shader
            // int drawAntsKernel = drawAntsShader.FindKernel("CSMain");
            // drawAntsShader.SetTexture(drawAntsKernel, "trailMap", trailMap);
            // drawAntsShader.SetBuffer(drawAntsKernel, "antBuffer", antBuffer);
            // drawAntsShader.SetInts("textureSize", new int[2] { trailMap.width, trailMap.height });
            // drawAntsShader.Dispatch(drawAntsKernel, currentNumberOfAnts / 64, 1, 1);

            // Execute the diffuse trail shader
            // int diffuseTrailKernel = diffuseTrailShader.FindKernel("CSMain");
            // diffuseTrailShader.Dispatch(diffuseTrailKernel, trailMap.width / 8, trailMap.height / 8, 1);

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
        
        
        int updateAntsKernel = computeShader.FindKernel("UpdateAnts");
        int drawAntsKernel = computeShader.FindKernel("DrawAnts");
        int textureDissipationKernel = computeShader.FindKernel("TextureDissipation");

        UpdateComputeShaderKernelVariables(updateAntsKernel);
        UpdateComputeShaderKernelVariables(drawAntsKernel);            
        UpdateComputeShaderKernelVariables(textureDissipationKernel);
            
        // computeShader.Dispatch(updateAntsKernel, numberOfAnts / 64, 1, 1);
        computeShader.Dispatch(drawAntsKernel, trailMap.width / 8, trailMap.height / 8, 1);
        // computeShader.Dispatch(textureDissipationKernel, trailMap.width / 8, trailMap.height / 8, 1);
    }

    void UpdateComputeShaderKernelVariables(int kernelIndex)
    {
        computeShader.SetTexture(kernelIndex, "trailMap", trailMap);
        computeShader.SetBuffer(kernelIndex, "antBuffer", antBuffer);
    }

    void OnDestroy() {
        antBuffer.Release();
        trailMap.Release();
    }

    private struct Ant {
        public Vector2 position;
        public float angle;
    }
}