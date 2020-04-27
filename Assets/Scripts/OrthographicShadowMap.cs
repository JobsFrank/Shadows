using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public enum PCFType
{
    HARD = 0,
    SOFT_2x2,
    SOFT_4Samples,
    SOFT_4x4,//慎用
}
public enum ShadowsType
{
    ProjectorShadow=0,
    ShadowMap,
}
[Serializable]
public struct ProjectorData
{
    [Tooltip("正交视口范围")]
    public float orthoSize;
    [Tooltip("投影纹理尺寸")]
    public int TextureSize;
};
[Serializable]
public struct ShadowMapData
{
    [Tooltip("近裁剪值")]
    public float Near;
    [Tooltip("远裁剪值")]
    public float Far;
    [Tooltip("正交视口范围")]
    public float orthoSize;
    [Tooltip("投影纹理尺寸")]
    public int TextureSize;
    [Range(0, 0.1f), Tooltip("阴影距模型的斜率值")]
    public float Bias;
    [Range(0, 1), Tooltip("阴影强度")]
    public float Strength;
    [Tooltip("PCF抗锯齿类型")]
    public PCFType softShadowType;
};
public class OrthographicShadowMap : MonoBehaviour
{
    public LayerMask casterLayer;
    public ShadowsType shadowType;
    public ProjectorData peojectorData = new ProjectorData
    {
        orthoSize = 10,
        TextureSize = 1024
    };
    public ShadowMapData shadowmapData = new ShadowMapData
    {
        Near = 0,
        Far = 200,
        orthoSize = 10,
        TextureSize = 1024,
        Bias = 0,
        Strength = 0.5f,
        softShadowType = PCFType.HARD
    };
    private Shader shader;
    private Transform actor;
    private Matrix4x4 biasMatrix;
    private Camera shadowCamera;
    private RenderTexture shadowTexture;
    private PCFType oldSoftShadowType;
    private Projector pro;
    private GameObject cameraParent;

    void InitRenderTexture()
    {
        if (shadowType == ShadowsType.ProjectorShadow)
        {
            shadowTexture = new RenderTexture(peojectorData.TextureSize, peojectorData.TextureSize, 0, RenderTextureFormat.R8);//纹理格式用单通道保存即可
            InitProjector();
        }
        else if (shadowType == ShadowsType.ShadowMap)
        {
            shadowTexture = new RenderTexture(shadowmapData.TextureSize, shadowmapData.TextureSize, 16, RenderTextureFormat.ARGB32);
            InitMatrix();
        }
        shadowTexture.name = "_ShadowRT";
        shadowTexture.antiAliasing = 1;
        shadowTexture.filterMode = FilterMode.Bilinear;
        shadowTexture.wrapMode = TextureWrapMode.Clamp;
    }
    void InitCamera()
    {
        if (shadowType == ShadowsType.ProjectorShadow)
        {
            shadowCamera = gameObject.AddComponent<Camera>();
            shadowCamera.clearFlags = CameraClearFlags.Color;
            shadowCamera.backgroundColor = Color.black;
            shadowCamera.orthographicSize = peojectorData.orthoSize;
            shadowCamera.depth = 0.0f;
            shadowCamera.nearClipPlane = pro.nearClipPlane;
            shadowCamera.farClipPlane = pro.farClipPlane;
        }
        else if (shadowType == ShadowsType.ShadowMap)
        {
            cameraParent = new GameObject("shadowCamera");
            shadowCamera = cameraParent.AddComponent<Camera>();
            shadowCamera.clearFlags = CameraClearFlags.SolidColor;
            shadowCamera.transform.position = Vector3.zero;
            shadowCamera.transform.rotation = transform.rotation;
            shadowCamera.transform.localPosition += transform.forward * shadowmapData.Near;
            shadowCamera.transform.parent = transform;
            shadowCamera.orthographicSize = shadowmapData.orthoSize;
            shadowCamera.backgroundColor = Color.white;
            shadowCamera.enabled = false;
            oldSoftShadowType = PCFType.HARD;
        }
        shadowCamera.allowHDR = false;
        shadowCamera.allowMSAA = false;
        shadowCamera.SetReplacementShader(shader, null);
        shadowCamera.targetTexture = shadowTexture;
        shadowCamera.orthographic = true;
    }
    void InitProjector()
    {
        pro = GetComponent<Projector>();
        pro.orthographic = true;
        pro.orthographicSize = peojectorData.orthoSize;
        pro.ignoreLayers = casterLayer;
        pro.material.SetTexture("_ShadowTex", shadowTexture);
    }
    void InitMatrix()
    {
        biasMatrix = Matrix4x4.identity;
        biasMatrix[0, 0] = 0.5f;
        biasMatrix[1, 1] = 0.5f;
        biasMatrix[2, 2] = 0.5f;
        biasMatrix[0, 3] = 0.5f;
        biasMatrix[1, 3] = 0.5f;
        biasMatrix[2, 3] = 0.5f;
    }
    void InitShader()
    {
        if (shadowType == ShadowsType.ProjectorShadow)
        {
            shader = Shader.Find("ShadowCaster");
        }
        else if (shadowType == ShadowsType.ShadowMap)
        {
            shader = Shader.Find("RenderDepthTexture");
        }
    }
    void InitData()
    {
        InitShader();
        InitRenderTexture();
        InitCamera();
    }
    void RenderShadows()
    {
        if (shadowType == ShadowsType.ProjectorShadow)
        {
            shadowCamera.cullingMask = casterLayer;
            if (actor!=null)
            {
                shadowCamera.transform.position = actor.position;
                shadowCamera.transform.position += -shadowCamera.transform.forward.normalized * 10;
            }
            shadowCamera.orthographicSize = peojectorData.orthoSize;
            pro.aspectRatio = shadowCamera.aspect;
            pro.orthographicSize = shadowCamera.orthographicSize;
            pro.nearClipPlane = shadowCamera.nearClipPlane;
            pro.farClipPlane = shadowCamera.farClipPlane;
        }
        else if (shadowType == ShadowsType.ShadowMap)
        {
            ChangePCFType();
            shadowCamera.cullingMask = casterLayer;
            shadowCamera.orthographicSize = shadowmapData.orthoSize;
            shadowCamera.farClipPlane = shadowmapData.Far;
            shadowCamera.nearClipPlane = shadowmapData.Near;
            shadowCamera.Render();
            Matrix4x4 depthProjectionMatrix = shadowCamera.projectionMatrix;
            Matrix4x4 depthViewMatrix = shadowCamera.worldToCameraMatrix;
            Matrix4x4 depthVP = depthProjectionMatrix * depthViewMatrix;
            Matrix4x4 depthVPBias = biasMatrix * depthVP;
            Shader.SetGlobalMatrix("_depthVPBias", depthVPBias);
            Shader.SetGlobalMatrix("_depthV", depthViewMatrix);
            Shader.SetGlobalTexture("_actShadowMap", shadowCamera.targetTexture);
            Shader.SetGlobalFloat("_bias", shadowmapData.Bias);
            Shader.SetGlobalFloat("_strength", 1 - shadowmapData.Strength);
            Shader.SetGlobalFloat("_texmapScale", 1f / shadowmapData.TextureSize);
            Shader.SetGlobalFloat("_farplaneScale", 1 / shadowmapData.Far);
            if (actor == null)
                return;
            shadowCamera.transform.position = actor.position;
            shadowCamera.transform.position += -shadowCamera.transform.forward.normalized * 10;
        }
    }
    void ChangePCFType()
    {
        if (oldSoftShadowType != shadowmapData.softShadowType)
        {
            if (shadowmapData.softShadowType == PCFType.HARD)
            {
                Shader.EnableKeyword("HARD_SHADOW");
                Shader.DisableKeyword("SOFT_SHADOW_2x2");
                Shader.DisableKeyword("SOFT_SHADOW_4Samples");
                Shader.DisableKeyword("SOFT_SHADOW_4x4");
            }
            if (shadowmapData.softShadowType == PCFType.SOFT_2x2)
            {
                Shader.DisableKeyword("HARD_SHADOW");
                Shader.EnableKeyword("SOFT_SHADOW_2x2");
                Shader.DisableKeyword("SOFT_SHADOW_4Samples");
                Shader.DisableKeyword("SOFT_SHADOW_4x4");
            }
            if (shadowmapData.softShadowType == PCFType.SOFT_4Samples)
            {
                Shader.DisableKeyword("HARD_SHADOW");
                Shader.DisableKeyword("SOFT_SHADOW_2x2");
                Shader.EnableKeyword("SOFT_SHADOW_4Samples");
                Shader.DisableKeyword("SOFT_SHADOW_4x4");
            }
            if (shadowmapData.softShadowType == PCFType.SOFT_4x4)
            {
                Shader.DisableKeyword("HARD_SHADOW");
                Shader.DisableKeyword("SOFT_SHADOW_2x2");
                Shader.DisableKeyword("SOFT_SHADOW_4Samples");
                Shader.EnableKeyword("SOFT_SHADOW_4x4");
            }
            oldSoftShadowType = shadowmapData.softShadowType;
        }
    }
    /// <summary>
    /// 切换场景要重置阴影的摄像机
    /// </summary>
    public void ResetCamera()
    {
        if (cameraParent != null)
            DestroyImmediate(cameraParent);
        if (gameObject.GetComponent<Camera>()!=null)
            DestroyImmediate(gameObject.GetComponent<Camera>());
    }

    public void SetTarget(Transform trans)
    {
        actor = trans;
    }
    private void Awake()
    {
        InitData();
    }
    private void LateUpdate()
    {
        RenderShadows();
    }
}
