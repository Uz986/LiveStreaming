using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TestScript : MonoBehaviour
{
    RenderTexture renderTexture;
    public VideoPlayer vP;
    [SerializeField] private Material sbsMaterial;
    // Start is called before the first frame update
    void Start()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(3980, 1080, 0, RenderTextureFormat.ARGB32);
        }
        sbsMaterial.SetTexture("_MainTex", renderTexture);
        vP.renderMode = VideoRenderMode.RenderTexture;
        vP.targetTexture = renderTexture;
        vP.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
