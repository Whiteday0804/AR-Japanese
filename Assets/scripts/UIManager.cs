using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel, tutorialPanel, questionPanel, voicePanel, objectPanel;
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
        objectPanel.SetActive(false);
        AppStateManager.CurrentState = AppState.Home;
    }

    public void ShowTutorialPanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(true);
        questionPanel.SetActive(false);
        voicePanel.SetActive(false);
        objectPanel.SetActive(false);
        AppStateManager.CurrentState = AppState.Tutorial;
    }

    public void ShowQuestionPanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(true);
        voicePanel.SetActive(false);
        objectPanel.SetActive(false);
        AppStateManager.CurrentState = AppState.Questions;
    }

    public void ShowVoicePanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(false);
        voicePanel.SetActive(true);
        objectPanel.SetActive(false);
        AppStateManager.CurrentState = AppState.Voice;
    }
    
    public void ShowObjectPanel()
    {
        homePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        questionPanel.SetActive(false);
        voicePanel.SetActive(false);
        objectPanel.SetActive(true);
        AppStateManager.CurrentState = AppState.Voice;
    }
}
