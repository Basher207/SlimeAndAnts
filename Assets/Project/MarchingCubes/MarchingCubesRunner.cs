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

    public Texture heightMapTexture;
    
    public MeshFilter meshFilter;
    public Vector3Int cubeSize = new Vector3Int(64, 64, 64);
    public Camera camera;
    
    public float noiseFrequency = 1f;
    public float noiseScale = 1;
    
    
    public float threshold = 0.5f;
    public float radius = 100f;    
    public float heightDelta = 100f;
    public float waterThreshold;

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

    private void LateUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
            return;
        // transform.localPosition = (Vector3)cubeSize / 2;
        
        shaderKernalIndex = shader.FindKernel("NoiseFieldGenerator");

        // Instantiate(meshBuilder);
        // Noise field update
        shader.SetInts("Dims", cubeSize);
        shader.SetFloat("NoiseScale", noiseScale);
        shader.SetFloat("NoiseFrequency", noiseFrequency);
        shader.SetFloat("Time", Time.time);
        shader.SetFloat("Radius", radius);
        shader.SetFloat("HeightDelta", heightDelta);
        shader.SetFloat("WaterThreshold", waterThreshold);

        shader.SetMatrix("Offset", GetSheerMatrix);// transform.parent.localToWorldMatrix);
        shader.SetMatrix("OffsetIT", GetSheerMatrix.inverse.transpose);
        shader.SetTexture(shaderKernalIndex, "HeightMapTexture", heightMapTexture);
        shader.SetBuffer(shaderKernalIndex, "Voxels", voxelBuffer);
        shader.DispatchThreads(shaderKernalIndex, cubeSize);

        voxelShader.SetMatrix("Offset", GetSheerMatrix);
        voxelShader.SetMatrix("OffsetIT", GetSheerMatrix.inverse.transpose);
        
        // Isosurface reconstruction
        meshBuilder.BuildIsosurface(voxelBuffer, threshold, 2f / cubeSize.x);
        meshFilter.sharedMesh = meshBuilder.Mesh;
    }

    Matrix4x4 GetSheerMatrix
    {
        get
        {
            if (camera == null)
                return Matrix4x4.identity;
            // 
            Matrix4x4 shiftMatrix = Matrix4x4.TRS(camera.transform.position, camera.transform.rotation * Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            return shiftMatrix * camera.projectionMatrix.inverse;
            Matrix4x4 temp = Gizmos.matrix;
    
            // Create the transformation matrix
            float fov = Mathf.Tan((camera.fieldOfView * 0.5f) * Mathf.Deg2Rad);
            Vector3 scale = new Vector3(fov * camera.aspect, fov, 1.0f);
            scale *= camera.farClipPlane;

            Matrix4x4 scaleMatrix = Matrix4x4.Scale(scale);
            Matrix4x4 translationMatrix = Matrix4x4.Translate(new Vector3(0, 0, 0.5f));

            Matrix4x4 trs = camera.transform.localToWorldMatrix * translationMatrix * scaleMatrix;


            return trs;
            
            // Calculate the projection matrix that will turn a unit cube into a camera frustum
            Matrix4x4 projectionMatrix = camera.projectionMatrix.inverse;
            Matrix4x4 shearMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1));

            return camera.transform.localToWorldMatrix * projectionMatrix;
        }
    }

    public GameObject trackingGo;
    
    public int subDivisions = 5;
    public int distance = 5;
    private void OnDrawGizmos()
    {
        
        // Matrix4x4 matrix = Gizmos.matrix;
        Gizmos.color = Color.white;

        // Matrix4x4 camMatrix = GetSheerMatrix;
        // Gizmos.matrix = GetSheerMatrix;//camera.projectionMatrix;//.transform,transform.parent.localToWorldMatrix;
        
        // Gizmos.DrawWireCube(new Vector3(0f, 0f, 0f), Vector3.one*2f);
        // Gizmos.DrawWireSphere(new Vector3(0f, 0f, 1f), 1f);

        // float maxWidth = 1f;
        // for (int x = -subDivisions; x <= subDivisions; x++)
        // {
        //     for (int y = -subDivisions; y <= subDivisions; y++)
        //     {
        //         for (int z = 0; z <= distance; z++)
        //         {
        //             Vector3 point1 = camMatrix.MultiplyPoint(new Vector3(x * (maxWidth / subDivisions), y * (maxWidth / subDivisions), z * (maxWidth / distance)));
        //             Vector3 point2 = camMatrix.MultiplyPoint(new Vector3(x * (maxWidth / subDivisions), (y+1) * (maxWidth / subDivisions), z * (maxWidth / distance)));
        //             Vector3 point3 = camMatrix.MultiplyPoint(new Vector3((x+1) * (maxWidth / subDivisions), (y+1) * (maxWidth / subDivisions), z * (maxWidth / distance)));
        //             Vector3 point4 = camMatrix.MultiplyPoint(new Vector3((x+1) * (maxWidth / subDivisions), y * (maxWidth / subDivisions), z * (maxWidth / distance)));
        //             
        //             Gizmos.DrawLine(point1, point2);
        //             Gizmos.DrawLine(point2, point3);
        //             Gizmos.DrawLine(point3, point4);
        //             Gizmos.DrawLine(point4, point1);
        //         }
        //     }
        // }
        if (!Application.isPlaying)
            return;
        // Gizmos.matrix = matrix;
        // GetComponent<MeshRenderer>().sharedMaterial.SetPass(0);

        // Graphics.DrawMeshNow(meshFilter.sharedMesh, Matrix4x4.identity);
        
        
        // trackingGo.transform.position = GetSheerMatrix.MultiplyPoint(camera.transform.position);
        
        Gizmos.color = Color.green;
        
        
        // Gizmos.DrawWireSphere(trackingGo.transform.position, 0.2f);
        // Gizmos.DrawWireMesh(meshBuilder.Mesh, Vector3.zero, Quaternion.identity, Vector3.one);
    }

    void OnDestroy()
    {
        voxelBuffer.Dispose();
        meshBuilder.Dispose();
    }
}
