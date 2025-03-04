using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;
        public float speed;
        public float frame;         // 36           帧是浮点数， 整数作为帧索引，小数作为插值权重
        public Vector3 padding;     // 36 + 12 = 48
    
        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = dir.x;
            direction.y = dir.y;
            direction.z = dir.z;
            noise_offset = offset;
            speed = frame = 0;
            padding.x = padding.y = padding.z = 0;
        }
    }
    public ComputeShader shader;
    private SkinnedMeshRenderer boidSMR;
    public GameObject boidObject;
    private Animator animator;
    public AnimationClip animationClip;

    private int numOfFrames;        // 动画总帧数
    // 定义群体行为模拟的参数。
    public int boidsCount; // Boid的数量。
    public float spawnRadius; // Boid生成的半径。
    public float rotationSpeed = 1f; // 旋转速度。
    public float boidSpeed = 1f; // Boid速度。
    public float neighbourDistance = 1f; // 邻近距离。
    public float boidSpeedVariation = 1f; // 速度变化。
    public float boidFrameSpeed = 10f;  // 动画播放速度
    public bool frameInterpolation = true;  // 是否插值计算帧顶点位置
    public Transform target; // 群体的移动目标。

    private Mesh boidMesh;
    public Material boidMaterial;

    int kernelHandle;
    ComputeBuffer boidsBuffer;
    ComputeBuffer vertexAnimationBuffer;
    ComputeBuffer argsBuffer;
    MaterialPropertyBlock props;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    Boid[] boidsArray;
    int groupSizeX;
    int numOfBoids;
    Bounds bounds;

    // Start is called before the first frame update
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        InitBoids();
        GenerateSkinnedAnimationForGPUBuffer();
        InitShader();
    }
    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }
    private void InitShader()
    {
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        boidsBuffer = new ComputeBuffer(numOfBoids, 12 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetFloat("rotationSpeed", rotationSpeed); // 旋转速度。
        shader.SetFloat("boidSpeed", boidSpeed); // Boid速度。
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation); // 速度变化。
        shader.SetVector("flockPosition", target.transform.position); // 群体的移动目标。
        shader.SetFloat("neighbourDistance", neighbourDistance); // 邻近距离。
        shader.SetFloat("boidFrameSpeed", boidFrameSpeed); // 邻近距离。
        shader.SetInt("boidsCount", numOfBoids); // Boid的数量。
        shader.SetInt("numOfFrames", numOfFrames); // 邻近距离。
        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
        boidMaterial.SetInt("numOfFrames", numOfFrames);

        if (frameInterpolation && !boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.EnableKeyword("FRAME_INTERPOLATION");
        if (!frameInterpolation && boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.DisableKeyword("FRAME_INTERPOLATION");
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);
    }
    private void OnDestroy()
    {
        if (boidsBuffer != null) boidsBuffer.Dispose();
        if (argsBuffer != null) argsBuffer.Dispose();
        if (vertexAnimationBuffer != null) vertexAnimationBuffer.Release();
    }
    private void GenerateSkinnedAnimationForGPUBuffer()
    {
        boidSMR = boidObject.GetComponentInChildren<SkinnedMeshRenderer>();

        boidMesh = boidSMR.sharedMesh;

        animator = boidObject.GetComponentInChildren<Animator>();
        int iLayer = 0;
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);

        Mesh bakedMesh = new Mesh();
        float sampleTime = 0;
        float perFrameTime = 0;
        
        numOfFrames = Mathf.ClosestPowerOfTwo((int)(animationClip.frameRate * animationClip.length));    // 总帧数
        perFrameTime = animationClip.length / numOfFrames;

        var vertexCount = boidSMR.sharedMesh.vertexCount;
        vertexAnimationBuffer = new ComputeBuffer(vertexCount * numOfFrames, 16); // 因为Vector4[] 所以16
        Vector4[] vertexAnimationData = new Vector4[vertexCount * numOfFrames];
        for(int i = 0; i < numOfFrames; i++)
        {
            animator.Play(aniStateInfo.shortNameHash, iLayer, sampleTime);
            animator.Update(0);

            boidSMR.BakeMesh(bakedMesh);
            for(int j = 0; j < vertexCount; j++)
            {
                Vector4 vertex = bakedMesh.vertices[j];
                vertex.w = 1;
                vertexAnimationData[(j * numOfFrames) + i] = vertex;
            }
            sampleTime += perFrameTime;
        }
        vertexAnimationBuffer.SetData(vertexAnimationData);
        boidMaterial.SetBuffer("vertexAnimation", vertexAnimationBuffer);

        boidObject.SetActive(false);    // 被拷贝的物体隐藏
    }
}
