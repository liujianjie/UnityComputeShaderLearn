using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassData : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    // Start is called before the first frame update
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }
    private void InitShader()
    {
        circlesHandle = shader.FindKernel("Circles");

        shader.SetInt("texResolution", texResolution);
        shader.SetTexture(circlesHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    private void DispatchKernel(int count)
    {
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        DispatchKernel(1);
    }
}
