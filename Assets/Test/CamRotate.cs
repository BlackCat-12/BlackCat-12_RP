using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRotate : MonoBehaviour
{
    [Header("摄像机设置")]
    public Camera myCamera;           // 指定要旋转的摄像机
    public Transform targetTransform; // 指定目标对象（可选）
    public Vector3 targetPos;         // 目标点的位置
    public float radius = 10f;        // 摄像机与目标点的距离
    public float speed = 20f;         // 摄像机旋转速度（度/秒）

    [Header("旋转轴设置")]
    public Vector3 rotationAxis = Vector3.up; // 旋转轴，默认绕Y轴旋转

    private Transform _transform;     // 当前摄像机的Transform

    void Start()
    {
        // 如果未在Inspector中指定摄像机，默认使用挂载脚本的摄像机
        if (myCamera == null)
        {
            myCamera = GetComponent<Camera>();
            if (myCamera == null)
            {
                Debug.LogError("未指定myCamera，并且该对象上没有Camera组件。");
                enabled = false;
                return;
            }
        }

        _transform = myCamera.transform;

        // 设置初始目标位置
        if (targetTransform != null)
        {
            targetPos = targetTransform.position;
        }

        // 将摄像机的位置设置在目标点的某个位置，确保与目标的距离为radius
        Vector3 initialOffset = new Vector3(0, 10, -radius);
        _transform.position = targetPos + initialOffset;

        // 确保摄像机初始时看向目标点
        _transform.LookAt(targetPos);
    }

    void Update()
    {
        // 如果指定了目标对象，更新目标位置
        if (targetTransform != null)
        {
            targetPos = targetTransform.position;
        }

        // 计算每帧应该旋转的角度
        float angle = speed * Time.deltaTime;

        // 绕目标点旋转
        _transform.RotateAround(targetPos, rotationAxis, angle);

        // 始终看向目标点
        _transform.LookAt(targetPos);
    }
}