using UnityEngine;

public class ToggleInfoPanel : MonoBehaviour
{
    public GameObject infoPanel;
    private bool isVisible = false;

    public void TogglePanel()
    {
        isVisible = !isVisible;
        infoPanel.SetActive(isVisible);
    }
}
