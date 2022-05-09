using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VHSBleed : MonoBehaviour
{
    private Material material;

    //public float color_resX = 7f;
    public float bias = 30f;

    private void Awake()
    {
        material = new Material(Shader.Find("Hidden/VHS Bleed"));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //material.SetFloat("color_resX", color_resX);
        material.SetFloat("bias", bias);
        Graphics.Blit(source, destination, material);
    }
}
