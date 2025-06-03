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
            playSoundButton.interactable = true;
            nextQuestionButton.interactable = true;
            isFirstPlay = false;
        }
        else
        {
            audioSource.Play();
            hasAnswered = false;
            playSoundButton.interactable = true;
            nextQuestionButton.interactable = true;
        }

        HideAllEffects(); // 新增：清除所有特效
    }

    public void OnCardDetected(string detectedName)
    {
        if (hasAnswered) return;

        bool isCorrect = (detectedName == currentTargetName);
        string cardObjectName = "card_" + detectedName;

        if (isCorrect)
        {
            comboCount++;
            playSoundButton.interactable = false;

            if (comboCount > bestCombo)
            {
                bestCombo = comboCount;
                PlayerPrefs.SetInt("BestCombo", bestCombo);
            }

            ShowCorrectEffect(cardObjectName); // 新增
        }
        else
        {
            comboCount = 0;
            playSoundButton.interactable = false;

            ShowWrongEffect(cardObjectName);
        }

        comboText.text = "目前連答紀錄: " + comboCount;
        bestComboText.text = "最高連答紀錄: " + bestCombo;
        hasAnswered = true;
    }

    private void NextQuestion()
    {
        isFirstPlay = true;
        playSoundButton.interactable = true;
        nextQuestionButton.interactable = false;
    }

    private void ShowWrongEffect(string cardName)
    {
        GameObject card = GameObject.Find(cardName);
        if (card != null)
        {
            Transform wrongEffect = card.transform.Find("WrongEffectCanvas");
            if (wrongEffect != null)
            {
                HideAllEffects();
                wrongEffect.gameObject.SetActive(true);
            }
        }
    }

    private void ShowCorrectEffect(string cardName)
    {
        GameObject card = GameObject.Find(cardName);
        if (card != null)
        {
            Transform correctCanvas = card.transform.Find("CorrectEffectCanvas");
            if (correctCanvas != null)
                HideAllEffects();
                correctCanvas.gameObject.SetActive(true);

            Transform goldParticle = card.transform.Find("GoldParticle");
            if (goldParticle != null)
            {
                var particle = goldParticle.GetComponent<ParticleSystem>();
                if (particle != null)
                    particle.Play();
            }
        }
    }

    public void HideAllEffects()
    {
        foreach (var card in GameObject.FindGameObjectsWithTag("ImageTarget"))
        {
            var wrong = card.transform.Find("WrongEffectCanvas");
            if (wrong != null) wrong.gameObject.SetActive(false);

            var correct = card.transform.Find("CorrectEffectCanvas");
            if (correct != null) correct.gameObject.SetActive(false);

            var gold = card.transform.Find("GoldParticle");
            if (gold != null)
            {
                var particle = gold.GetComponent<ParticleSystem>();
                if (particle != null) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
