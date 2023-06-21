using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntSimulationController : MonoBehaviour
{
    public Texture textureMap;
    public Material textureMaterial;
    
    public ComputeShader antBehaviorShader;
    public ComputeShader drawAntsShader;
    public ComputeShader diffuseTrailShader;

    public int numAnts = 1000;
    public float sensorAngle = 0.1f;
    public float sensorScale = 1.0f;
    public float rotationSpeed = 1.0f;
    public float moveSpeed = 0.1f;
    
    public float repulseScale = 1.0f;
    public float attractionScale = 0.1f;
    public float sensorDistance = 0.1f;

    private ComputeBuffer antBuffer;
    private RenderTexture trailMap;

    public float targetFrameRate = 120f;
    public int antSpawnPerFrame = 5;
    public int currentNumberOfAnts = 10000;

    private Ant [] ants;
    void Start() {
        // Initialize the trail map
        trailMap = new RenderTexture(2160, 2160, 0, RenderTextureFormat.ARGBFloat);
        trailMap.enableRandomWrite = true;
        trailMap.Create();
        textureMaterial.mainTexture = trailMap;
        
        // Initialize the ant buffer
        ants = new Ant[numAnts];
        for (int i = 0; i < numAnts; i++)
        {
            Vector2 position = Random.insideUnitCircle * 0.1f;//Random.insideUnitCircle.normalized;
            float angle = Random.Range(-Mathf.PI, Mathf.PI);//-position;
            
            ants[i] = new Ant {
                position = position,//Random.insideUnitCircle.normalized,
                // position = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)),//Random.insideUnitCircle * 0.1f,
                angle = angle//.SignedAngle(Vector2.right, velocity) * Mathf.Deg2Rad//,
            };
        }
        antBuffer = new ComputeBuffer(numAnts, sizeof(float) * 3);
        antBuffer.SetData(ants);
        StartCoroutine(UpdateAnts());
    }

    IEnumerator UpdateAnts() {
        while (true)
        {
            // Execute the ant behavior shader
            int antBehaviorKernel = antBehaviorShader.FindKernel("CSMain");
            antBehaviorShader.SetTexture(antBehaviorKernel, "trailMap", trailMap);
            antBehaviorShader.SetBuffer(antBehaviorKernel, "antBuffer", antBuffer);
            antBehaviorShader.SetFloat("time", 1f / targetFrameRate);
            antBehaviorShader.SetFloat("sensorAngle", sensorAngle);
            antBehaviorShader.SetFloat("sensorScale", sensorScale);
            antBehaviorShader.SetFloat("rotationSpeed", rotationSpeed);
            antBehaviorShader.SetFloat("moveSpeed", moveSpeed);
            
            antBehaviorShader.SetFloat("repulseScale", repulseScale);
            antBehaviorShader.SetFloat("attractionScale", attractionScale);
            antBehaviorShader.SetFloat("sensorDistance", sensorDistance);
            
            antBehaviorShader.SetInt("numAnts", currentNumberOfAnts);
            antBehaviorShader.SetInts("textureSize", new int[2] { trailMap.width, trailMap.height });
            antBehaviorShader.Dispatch(antBehaviorKernel, currentNumberOfAnts / 64, 1, 1);

            // Execute the draw ants shader
            int drawAntsKernel = drawAntsShader.FindKernel("CSMain");
            drawAntsShader.SetTexture(drawAntsKernel, "trailMap", trailMap);
            drawAntsShader.SetBuffer(drawAntsKernel, "antBuffer", antBuffer);
            drawAntsShader.SetInt("numAnts", currentNumberOfAnts);
            drawAntsShader.SetInts("textureSize", new int[2] { trailMap.width, trailMap.height });
            drawAntsShader.Dispatch(drawAntsKernel, currentNumberOfAnts / 64, 1, 1);

            // Execute the diffuse trail shader
            int diffuseTrailKernel = diffuseTrailShader.FindKernel("CSMain");
            diffuseTrailShader.SetTexture(diffuseTrailKernel, "trailMap", trailMap);
            diffuseTrailShader.Dispatch(diffuseTrailKernel, trailMap.width / 8, trailMap.height / 8, 1);

            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 mousePos = Camera.main.ScreenToViewportPoint(Input.mousePosition) * 2f - Vector3.one;
                
                int targetNumberOfAnts = Mathf.Min(currentNumberOfAnts + antSpawnPerFrame, numAnts);
                
                for (; currentNumberOfAnts < targetNumberOfAnts; currentNumberOfAnts++)
                {
                    ants[currentNumberOfAnts] = new Ant {position = mousePos, angle = Random.Range(-Mathf.PI, Mathf.PI)};
                }
                
                antBuffer.SetData(ants, currentNumberOfAnts - antSpawnPerFrame, currentNumberOfAnts - antSpawnPerFrame, antSpawnPerFrame);
                
                // diffuseTrailShader.SetVector("mousePos",  new Vector2(trailMap.width, trailMap.height) * mousePos);
                // int repulseTrailKernel = diffuseTrailShader.FindKernel("RepulseTrail");
                // diffuseTrailShader.SetTexture(repulseTrailKernel, "trailMap", trailMap);
                // diffuseTrailShader.Dispatch(repulseTrailKernel, trailMap.width / 8, trailMap.height / 8, 1);
            }
            yield return new WaitForSeconds(1f / targetFrameRate);
        }
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