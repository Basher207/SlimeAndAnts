using System;
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using MarchingCubes;
using UnityEngine.Rendering;

public class MarchingCubesRunner : MonoBehaviour
{
    public ComputeShader shader;
    public ComputeShader voxelShader;

    public MeshFilter meshFilter;
    public Vector3Int cubeSize = new Vector3Int(64, 64, 64);
    public float scale = 1f;
    public float noiseScale = 1;
    public float threshold = 0.5f;
    

    private Mesh mesh;

    private int computeKernal;

    public int triangleBudget = 1000000;
    
    private MeshBuilder meshBuilder;
    private ComputeBuffer voxelBuffer;

    private int shaderKernalIndex;
    
    private void Start()
    {
        meshBuilder = new MeshBuilder(cubeSize, triangleBudget, voxelShader);
        voxelBuffer = new ComputeBuffer(cubeSize.x * cubeSize.y * cubeSize.z, sizeof(float));
        
    }

    private void Update()
    {
        shaderKernalIndex = shader.FindKernel("NoiseFieldGenerator");

        Instantiate(meshBuilder);
        // Noise field update
        shader.SetInts("Dims", cubeSize);
        shader.SetFloat("Scale", noiseScale);
        shader.SetFloat("Time", Time.time);
        shader.SetBuffer(shaderKernalIndex, "Voxels", voxelBuffer);
        shader.DispatchThreads(shaderKernalIndex, cubeSize);

        // Isosurface reconstruction
        meshBuilder.BuildIsosurface(voxelBuffer, threshold, scale);
        meshFilter.sharedMesh = meshBuilder.Mesh;
    }

    void OnDestroy()
    {
        voxelBuffer.Dispose();
        meshBuilder.Dispose();
    }
}
