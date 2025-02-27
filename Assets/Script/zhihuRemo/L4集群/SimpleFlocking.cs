using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;

        public Boid(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = 0;
            direction.y = 0;
            direction.z = 0;
        }
    }
    public ComputeShader shader;
    // 定义群体行为模拟的参数。
    public float rotationSpeed = 1f; // 旋转速度。
    public float boidSpeed = 1f; // Boid速度。
    public float neighbourDistance = 1f; // 邻近距离。
    public float boidSpeedVariation = 1f; // 速度变化。
    public GameObject boidPrefab; // Boid对象的预制体。
    public int boidsCount; // Boid的数量。
    public float spawnRadius; // Boid生成的半径。
    public Transform target; // 群体的移动目标。

    int kernelHandle;
    ComputeBuffer boidsBuffer;
    Boid[] boidsArray;
    GameObject[] boids;
    int groupSizeX;
    int numOfBoids;

    // Start is called before the first frame update
    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        InitBoids();
        InitShader();
    }
    private void InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++) {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidsArray[i] = new Boid(pos);
            boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity) as GameObject;
            boidsArray[i].direction = boids[i].transform.forward;
        }
    }
    private void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 6 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat("rotationSpeed", rotationSpeed); // 旋转速度。
        shader.SetFloat("boidSpeed", boidSpeed); // Boid速度。
        shader.SetFloat("neighbourDistance", neighbourDistance); // 邻近距离。
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation); // 速度变化。
        shader.SetInt("boidsCount", boidsCount); // Boid的数量。
        shader.SetVector("flockPosition", target.transform.position); // 群体的移动目标。

        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        for(int i = 0; i < boidsArray.Length; i++)
        {
            boids[i].transform.localPosition = boidsArray[i].position;

            if (!boidsArray[i].direction.Equals(Vector3.zero))
            {
                boids[i].transform.rotation = Quaternion.LookRotation(boidsArray[i].direction);
            }
        }
    }
    private void OnDestroy()
    {
        if(boidsBuffer != null)
        {
            boidsBuffer.Dispose();
        }
    }
}
