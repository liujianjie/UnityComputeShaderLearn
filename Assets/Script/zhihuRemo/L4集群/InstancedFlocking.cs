using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = dir.x;
            direction.y = dir.y;
            direction.z = dir.z;
            noise_offset = offset;
        }
    }
    public ComputeShader shader;
    // 定义群体行为模拟的参数。
    public float rotationSpeed = 1f; // 旋转速度。
    public float boidSpeed = 1f; // Boid速度。
    public float neighbourDistance = 1f; // 邻近距离。
    public float boidSpeedVariation = 1f; // 速度变化。
    public Mesh boidMesh;
    public Material boidMaterial;
    public int boidsCount; // Boid的数量。
    public float spawnRadius; // Boid生成的半径。
    public Transform target; // 群体的移动目标。

    int kernelHandle;
    ComputeBuffer boidsBuffer;
    ComputeBuffer argsBuffer;
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

        InitBoids();
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
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if(boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat("rotationSpeed", rotationSpeed); // 旋转速度。
        shader.SetFloat("boidSpeed", boidSpeed); // Boid速度。
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation); // 速度变化。
        shader.SetVector("flockPosition", target.transform.position); // 群体的移动目标。
        shader.SetFloat("neighbourDistance", neighbourDistance); // 邻近距离。
        shader.SetInt("boidsCount", boidsCount); // Boid的数量。

        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(boidMesh,0 , boidMaterial, bounds, argsBuffer);  
    }
    private void OnDestroy()
    {
        if (boidsBuffer != null)
        {
            boidsBuffer.Dispose();
        }
        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }
}
