using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowsBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        GameObject prefabObj = Resources.Load("ActorShadowMap") as GameObject;
        GameObject go = GameObject.Instantiate(prefabObj) as GameObject;
        OrthographicShadowMap shadowProjector = go.GetComponent<OrthographicShadowMap>();
        if (shadowProjector != null)
        {
            Vector3 lightDir = new Vector3(-24.0f, 24.56f, -27.76f);
            //Vector3 lightDir = new Vector3(31.479f,42.816f,25.819f);
            Vector3 center = new Vector3(2, 2, 2);
            Vector3 size = new Vector3(10, 10, 10);
            Bounds shadowBound = new Bounds(center, size);
            if (shadowProjector.shadowType == ShadowsType.ProjectorShadow)
                SetMainCityShadowSetting(lightDir, shadowProjector.GetComponent<Camera>(), shadowBound, GetComponent<Camera>());
            else if (shadowProjector.shadowType == ShadowsType.ShadowMap)
                SetMainCityShadowSetting(lightDir, shadowProjector.GetComponentInChildren<Camera>(), shadowBound, GetComponent<Camera>(), shadowProjector);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
    private void SetMainCityShadowSetting(Vector3 rotation, Camera projCamera, Bounds bound, Camera mainCamera, OrthographicShadowMap obj = null)
    {
        Vector3 preMainPos = mainCamera.transform.position;
        mainCamera.transform.position = new Vector3(bound.center.x, preMainPos.y, preMainPos.z);
        projCamera.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
        projCamera.transform.position = bound.center;
        Matrix4x4 worldToMainCamera = mainCamera.transform.worldToLocalMatrix;
        Matrix4x4 mainCameraToWorld = worldToMainCamera.inverse;
        List<Vector3> boundsFourUpPoints = new List<Vector3>();
        //左上
        boundsFourUpPoints.Add(bound.center + new Vector3(bound.size.x / 2, 0, -bound.size.z / 2));
        //右上
        boundsFourUpPoints.Add(bound.center + new Vector3(-bound.size.x / 2, 0, -bound.size.z / 2));
        //左下
        boundsFourUpPoints.Add(bound.center + new Vector3(bound.size.x / 2, 0, bound.size.z / 2));
        //右下
        boundsFourUpPoints.Add(bound.center + new Vector3(-bound.size.x / 2, 0, bound.size.z / 2));

        float r = (mainCamera.fieldOfView / 180f) * Mathf.PI;
        Vector3 tempVec = Vector3.zero;
        float z = 0;
        float xMax = 0;
        float yMax = 0;
        float x = 0;
        float y = 0;
        for (int i = 0; i < boundsFourUpPoints.Count; i++)
        {
            tempVec = boundsFourUpPoints[i];
            //将包围盒四个顶点转换到主摄像机空间
            boundsFourUpPoints[i] = worldToMainCamera.MultiplyPoint(tempVec);
            //计算该点z对用主摄像机xy范围
            tempVec = boundsFourUpPoints[i];
            z = tempVec.z;
            x = tempVec.x;
            y = tempVec.y;
            yMax = Mathf.Abs(Mathf.Tan(r / 2) * z);
            xMax = mainCamera.aspect * yMax;
            if (Mathf.Abs(x) > xMax)
            {
                x = x / Mathf.Abs(x) * xMax;
            }
            if (Mathf.Abs(y) > yMax)
            {
                y = y / Mathf.Abs(y) * yMax;
            }
            tempVec.x = x;
            tempVec.y = y;
            //在转换为世界坐标
            boundsFourUpPoints[i] = mainCameraToWorld.MultiplyPoint(tempVec);
        }
        Matrix4x4 worldToProjCamera = projCamera.transform.worldToLocalMatrix;
        //转换到投影空间设置投影相机size
        for (int i = 0; i < boundsFourUpPoints.Count; i++)
        {
            boundsFourUpPoints[i] = worldToProjCamera.MultiplyPoint(boundsFourUpPoints[i]);
        }
        float xMin = boundsFourUpPoints[0].x;
        xMax = boundsFourUpPoints[0].x;
        float yMin = boundsFourUpPoints[0].y;
        yMax = boundsFourUpPoints[0].y;
        for (int i = 0; i < boundsFourUpPoints.Count; i++)
        {
            if (boundsFourUpPoints[i].x < xMin)
            {
                xMin = boundsFourUpPoints[i].x;
            }
            else if (boundsFourUpPoints[i].x > xMax)
            {
                xMax = boundsFourUpPoints[i].x;
            }
            if (boundsFourUpPoints[i].y < yMin)
            {
                yMin = boundsFourUpPoints[i].y;
            }
            else if (boundsFourUpPoints[i].y > yMax)
            {
                yMax = boundsFourUpPoints[i].y;
            }
        }
        projCamera.orthographicSize = (yMax - yMin) > (xMax - xMin) ? (yMax - yMin) / 2 : (xMax - xMin) / 2;
        projCamera.transform.position -= projCamera.transform.forward.normalized * 10;
        if (obj != null)
            projCamera.transform.parent = obj.transform;
        mainCamera.transform.position = preMainPos;
    }
}
