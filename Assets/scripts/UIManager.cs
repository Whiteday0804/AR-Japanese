using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel, tutorialPanel, questionPanel, voicePanel;
    // Start is called before the first frame update
    void Start()
    {
        ShowHomePanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ShowHomePanel()
    {
        homePanel.SetActive(true);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(false);
        voicePanel.SetActive(false);
    }
    
    public void ShowTutorialPanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(true);
        questionPanel.SetActive(false);
        voicePanel.SetActive(false);
    }

    public void ShowQuestionPanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(true);
        voicePanel.SetActive(false);
    }

    public void ShowVoicePanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(false);
        voicePanel.SetActive(true);
    }
}
