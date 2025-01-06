using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGizmoTest : MonoBehaviour
{
    public Vector3[] posi;
    public void OnDrawGizmos()
    {
        Color originColor = Gizmos.color;
        Gizmos.color = Color.green;
        if (posi != null && posi.Length > 0)
        {
            for (int i = 0; i < posi.Length; i++)
            {
                Gizmos.DrawSphere(posi[i], 0.2f);
            }

            Vector3 lastPosi = Vector3.zero;

            for (int i = 0; i < posi.Length; i++)
            {
                Gizmos.color = Color.blue;
                if (i > 0)
                {
                    Gizmos.DrawLine(lastPosi, posi[i]);
                }
                lastPosi = posi[i];
            }
        }

        Gizmos.color = originColor;
    }
}
