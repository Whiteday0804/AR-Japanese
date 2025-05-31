using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buttonControll : MonoBehaviour
{
    public Transform canvasTransform;
    void Start()
    {
        canvasTransform.LookAt(Camera.main.transform);
    }

    public void PlaySound()
    {
        Debug.Log("Play Sound");
    }
}
