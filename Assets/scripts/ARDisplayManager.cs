using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class ARDisplayManager : MonoBehaviour
{
    private void Start()
    {
        ImageTargetBehaviour[] imageTargets = FindObjectsOfType<ImageTargetBehaviour>();
        foreach (var target in imageTargets)
        {
            target.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        if (behaviour != null && behaviour.transform.childCount > 0)
        {
            GameObject arContent = behaviour.transform.GetChild(0).gameObject;
            arContent.SetActive(isTracked && AppStateManager.CurrentState == AppState.tutorial);
        }
    }
}
