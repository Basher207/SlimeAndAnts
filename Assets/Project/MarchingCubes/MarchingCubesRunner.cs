using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

public class MarchingCubesRunner : MonoBehaviour
{
    public ComputeShader shader;
    public MeshFilter meshFilter;
    public int cubeSize = 32;
    public float noiseScale = 1;
    public float threshold = 0.5f;

    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer normalBuffer;
    
    private ComputeBuffer vertexCountBuffer;
    private ComputeBuffer vertexCountIndirectBuffer;

    private ComputeBuffer triTableIndexBuffer;
    private ComputeBuffer triTableBuffer;
    private ComputeBuffer triTableCountBuffer;

    private Mesh mesh;

    private int computeKernal;

    public int triangleBudget = 1000000;
    
    private void Start()
    {
        // Initialize the ComputeBuffers
        computeKernal = shader.FindKernel("MarchingCubes");

        vertexCountBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.Counter);
        // vertexCountIndirectBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

        triTableIndexBuffer = new ComputeBuffer(MarchingCubesTables.TriTableIndex.Length, sizeof(int));
        triTableBuffer = new ComputeBuffer(MarchingCubesTables.TriTable.Length, sizeof(int));
        triTableCountBuffer = new ComputeBuffer(MarchingCubesTables.TriTableCount.Length, sizeof(int));

        triTableIndexBuffer.SetData(MarchingCubesTables.TriTableIndex);
        triTableBuffer.SetData(MarchingCubesTables.TriTable);
        triTableCountBuffer.SetData(MarchingCubesTables.TriTableCount);

        Mesh mesh = new Mesh();

        // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        // Vertex position: float32 x 3
        var vp = new VertexAttributeDescriptor
            (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        // Vertex normal: float32 x 3
        var vn = new VertexAttributeDescriptor
            (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        // Vertex/index buffer formats
        mesh.SetVertexBufferParams(triangleBudget, vp, vn);
        mesh.SetIndexBufferParams(triangleBudget, IndexFormat.UInt32);

        // Submesh initialization
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleBudget),
            MeshUpdateFlags.DontRecalculateBounds);

        // GraphicsBuffer references
        vertexBuffer = mesh.GetVertexBuffer(0);
        normalBuffer = mesh.GetIndexBuffer();
        
        
        // Assign to shader
        shader.SetBuffer(computeKernal, "VertexBuffer", vertexBuffer);
        shader.SetBuffer(computeKernal, "NormalBuffer", normalBuffer);
        
        shader.SetBuffer(computeKernal, "triTableIndex", triTableIndexBuffer);
        shader.SetBuffer(computeKernal, "triTable", triTableBuffer);
        shader.SetBuffer(computeKernal, "triTableCount", triTableCountBuffer);
        
        shader.SetBuffer(computeKernal, "vertexCount", vertexCountBuffer);

        // Reset vertex count
        vertexCountBuffer.SetCounterValue(0);

        mesh = new Mesh();
        meshFilter.mesh = mesh;
        // Initialize Mesh
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    private void Update()
    {
        
        
        // Reset vertex count
        ComputeBuffer.CopyCount(vertexCountBuffer, vertexCountIndirectBuffer, 0);
        
        // int[] verticiesCount = new int[1];
        // vertexCountIndirectBuffer.GetData(verticiesCount);

        // int vertexCount = verticiesCount[0];



        shader.SetFloat("noiseScale", noiseScale);
        shader.SetFloat("threshold", threshold);


        // get current vertex count from buffer

        // Vector3[] vertices = new Vector3[vertexCount];
        // Vector3[] normals = new Vector3[vertexCount];

        // vertexBuffer.GetData(vertices, 0, 0, vertexCount);
        // normalBuffer.GetData(normals, 0, 0, vertexCount);
        // Debug.Log(vertexCount);
        // mesh.Clear();
        // mesh.vertices = vertices;
        // mesh.normals = normals;
        // mesh.SetIndices(Enumerable.Range(0, vertexCount).ToArray(), MeshTopology.Triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * cubeSize);
        
        
        vertexCountBuffer.SetCounterValue(0);
        shader.Dispatch(0, cubeSize / 8, cubeSize / 8, cubeSize / 8);
    }

    private void OnDestroy()
    {
        vertexBuffer.Release();
        normalBuffer.Release();
        vertexCountBuffer.Release();
        vertexCountIndirectBuffer.Release();
        triTableIndexBuffer.Release();
        triTableBuffer.Release();
        triTableCountBuffer.Release();
    }
}
