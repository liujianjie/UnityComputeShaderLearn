using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//https://zhuanlan.zhihu.com/p/368307575
public class GettingStarted : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        RenderTexture mRenderTexture = new RenderTexture(256, 256, 16);
        mRenderTexture.enableRandomWrite = true;
        mRenderTexture.Create();

        material.mainTexture = mRenderTexture;
        int kernerIndex = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernerIndex, "Result", mRenderTexture);

        computeShader.Dispatch(kernerIndex, 256 / 8, 256 / 8, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
