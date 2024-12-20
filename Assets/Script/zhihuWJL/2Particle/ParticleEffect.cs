using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;

    ComputeBuffer mParticleDataBuffer;

    const int mParticleCount = 20000;

    int kernelId;

    struct ParticleData
    {
        public Vector3 pos;
        public Color color;
    }

    // Start is called before the first frame update
    void Start()
    {
        mParticleDataBuffer = new ComputeBuffer(mParticleCount, 28);
        ParticleData[] particleDatas = new ParticleData[mParticleCount];
        mParticleDataBuffer.SetData(particleDatas);
        kernelId = computeShader.FindKernel("UpdateParticle");
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetBuffer(kernelId, "ParticleBuffer", mParticleDataBuffer);
        computeShader.SetFloat("Time", Time.time);
        computeShader.Dispatch(kernelId, mParticleCount / 1000, 1, 1);
        material.SetBuffer("_particleDataBuffer", mParticleDataBuffer);
    }
    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, mParticleCount);
    }
    private void OnDestroy()
    {
        mParticleDataBuffer.Release();
        mParticleDataBuffer = null;
    }
}
