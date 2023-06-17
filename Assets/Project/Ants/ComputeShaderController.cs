using UnityEngine;

public struct Ant {
    public Vector3 position;
    public Vector3 direction;
    public float speed;
}

public class ComputeShaderController : MonoBehaviour
{

    public int numberOfAnts = 2000;
    
    public ComputeShader antBehaviorComputeShader;
    public ComputeShader antMovementComputeShader;
    public ComputeShader clearTextureComputeShader;
    public ComputeShader drawAntsComputeShader;

    [SerializeField] public Vector3 textureOffset;
    
    private int antBehaviorKernel;
    private int antMovementKernel;
    private int clearTextureKernel;
    private int drawAntsKernel;

    private ComputeBuffer antBuffer;
    public RenderTexture antTexture;

    public Material material;

    private Ant[] ants;

    

    void Start() {
        antBehaviorKernel = antBehaviorComputeShader.FindKernel("CSMain");
        antMovementKernel = antMovementComputeShader.FindKernel("CSMain");
        clearTextureKernel = clearTextureComputeShader.FindKernel("CSMain");
        drawAntsKernel = drawAntsComputeShader.FindKernel("CSMain");

        ants = new Ant[1000]; // Let's say we have 1000 ants for now.
        for (int i = 0; i < ants.Length; i++) {
            ants[i] = new Ant {
                position = Random.insideUnitSphere, // previously multiplied by 10, remove that to keep within (-1,-1,-1) to (1,1,1) box
                direction = Random.onUnitSphere, // simplified from insideUnitSphere.normalized
                speed = Random.Range(0.5f, 1f),
            };
        }

        antBuffer = new ComputeBuffer(ants.Length, sizeof(float) * 7); // each Ant has 7 float values (3 for position, 3 for direction and 1 for speed)
        antBuffer.SetData(ants);

        antTexture = new RenderTexture(512, 512, 0) {
            enableRandomWrite = true,
        };
        antTexture.Create();
        material.mainTexture = antTexture;
    }

    void Update() {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        antBehaviorComputeShader.SetVector("randomDirection", randomDirection);

        // Run the ant behavior compute shader
        antBehaviorComputeShader.Dispatch(antBehaviorKernel, ants.Length / 64 + 1, 1, 1);
        antBehaviorComputeShader.SetBuffer(antBehaviorKernel, "antBuffer", antBuffer);
        antBehaviorComputeShader.SetInt("numAnts", ants.Length);
        antBehaviorComputeShader.SetFloat("time", Time.time);
        antBehaviorComputeShader.Dispatch(antBehaviorKernel, ants.Length / 64 + 1, 1, 1); // Assuming you have 64 threads in your thread group

        antMovementComputeShader.SetBuffer(antMovementKernel, "antBuffer", antBuffer);
        antMovementComputeShader.SetFloat("time", Time.time);
        antMovementComputeShader.SetFloat("deltaTime", Time.deltaTime);
        antMovementComputeShader.Dispatch(antMovementKernel, ants.Length / 64 + 1, 1, 1);

        clearTextureComputeShader.SetTexture(clearTextureKernel, "outputTexture", antTexture);
        clearTextureComputeShader.Dispatch(clearTextureKernel, antTexture.width / 8, antTexture.height / 8, 1);

        drawAntsComputeShader.SetVector("textureOffset", textureOffset);
        drawAntsComputeShader.SetBuffer(drawAntsKernel, "antBuffer", antBuffer);
        drawAntsComputeShader.SetTexture(drawAntsKernel, "outputTexture", antTexture);
        drawAntsComputeShader.SetInts("texSize", new int[] { antTexture.width, antTexture.height });  // Pass the texture size to the shader
        drawAntsComputeShader.Dispatch(drawAntsKernel, ants.Length / 64 + 1, 1, 1);
    }

    void OnDestroy() {
        antBuffer.Release();
        antTexture.Release();
    }
}
