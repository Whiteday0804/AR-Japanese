using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    [Header("音檔")]
    public List<AudioClip> fiftySounds;
    public AudioSource audioSource;

    [Header("UI")]
    public Button playSoundButton;
    public Button nextQuestionButton;
    public Text resultText;
    public Text comboText;
    public Text bestComboText;

    private int index = 0;
    private string currentTargetName;
    private bool hasAnswered = false;

    private int comboCount = 0;
    private int bestCombo = 0;
    private bool isFirstPlay = true;

    void Start()
    {
        resultText.text = "";
        comboText.text = "目前連答紀錄: 0";
        bestCombo = PlayerPrefs.GetInt("BestCombo", 0);
        bestComboText.text = "最高連答紀錄: " + bestCombo;

        playSoundButton.onClick.AddListener(PlayCurrentQuestion);
        nextQuestionButton.onClick.AddListener(NextQuestion);

        playSoundButton.interactable = true;
        nextQuestionButton.interactable = false;
    }

    private void PlayCurrentQuestion()
    {
        if (isFirstPlay)
        {
            index = Random.Range(0, fiftySounds.Count);
            currentTargetName = fiftySounds[index].name;
            audioSource.clip = fiftySounds[index];
            audioSource.Play();

            hasAnswered = false;
            resultText.text = "";
            playSoundButton.interactable = true;
            nextQuestionButton.interactable = true;
            isFirstPlay = false;
        }
        else
        {

            audioSource.Play();

            hasAnswered = false;
            resultText.text = "";
            playSoundButton.interactable = true;
            nextQuestionButton.interactable = true;
        }
    }

    public void OnCardDetected(string detectedName)
    {
        if (hasAnswered) return;

        if (detectedName == currentTargetName)
        {
            resultText.text = "正確！";
            comboCount++;
            playSoundButton.interactable = false;
            if (comboCount > bestCombo)
            {
                bestCombo = comboCount;
                PlayerPrefs.SetInt("BestCombo", bestCombo);
            }
        }
        else
        {
            resultText.text = "錯誤！";
            comboCount = 0;
            playSoundButton.interactable = false;
        }

        comboText.text = "目前連答紀錄: " + comboCount;
        bestComboText.text = "最高連答紀錄: " + bestCombo;
        hasAnswered = true;
    }

    private void NextQuestion()
    {
        isFirstPlay = true;
        resultText.text = "";
        playSoundButton.interactable = true;
        nextQuestionButton.interactable = false;
    }
}
