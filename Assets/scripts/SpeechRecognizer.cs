using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
public class SpeechRecognizer : MonoBehaviour
{

    private string apiKey = APIKeyLoader.GetAPIKey();
    private AudioClip clip;
    private const int sampleRate = 16000;
    private bool isRecording = false;
    public GameObject correctIcon;  
    public GameObject wrongIcon; 

    public Dictionary<string, string> imageToKana = new Dictionary<string, string>()
    {
        { "a", "あ 足" },{ "i", "い 犬" },{ "u", "う 海" },{ "e", "え 駅" },{ "o", "お お茶" },
        { "ka", "か 傘" },{ "ki", "き " },{ "ku", "く 車" },{ "ke", "け 煙" },{ "ko", "こ 子 供" },
        { "sa", "さ 桜" },{ "si", "し 白" },{ "su", "す 寿 司" },{ "se", "せ 世 界" },{ "so", "そ 空" },
        { "ta", "た 卵" },{ "ti", "ち 地 図" },{ "tu", "つ 月" },{ "te", "て 手" },{ "to", "と 時 計" },
        { "na", "な 夏" },{ "ni", "に 日 本" },{ "nu", "ぬ 布" },{ "ne", "ね 猫" },{ "no", "の 飲 み 物" },
        { "ha", "は 花" },{ "hi", "ひ 火" },{ "hu", "ふ 船" },{ "he", "へ 部 屋" },{ "ho", "ほ 星" },
        { "ma", "ま 窓" },{ "mi", "み 水" },{ "mu", "む 虫" },{ "me", "め 目" },{ "mo", "も 森" },
        { "ra", "ら 蘭" },{ "ri", "り 林 檎" },{ "ru", "る 留 守 番" },{ "re", "れ 冷 蔵 庫" },{ "ro", "ろ 廊 下" },
        { "ya", "や 山" },{ "yu", "ゆ 雪" },{ "yo", "よ 夜" },
        { "wa", "わ 私" },{ "wo", "を" },{ "n", "ん 本 ほ ん" }

    };

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("找不到麥克風");

            return;
        }


    }

    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.Log("錄音中，請等待結束...");
            return;
        }
        Debug.Log("開始錄音（5秒）...");
        string mic = Microphone.devices[0];
        clip = Microphone.Start(mic, false, 5, sampleRate);
        isRecording = true;
        StartCoroutine(WaitAndStop());
    }

    IEnumerator WaitAndStop()
    {
        yield return new WaitForSeconds(5f);
        Microphone.End(null);
        isRecording = false;
        Debug.Log("錄音結束，開始辨識");
        StartCoroutine(ProcessAudio());
    }

    IEnumerator ProcessAudio()
    {
        yield return new WaitForSeconds(0.1f); // 確保錄音停止後資料完整

        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] pcm = ConvertToPCM16(samples);

        yield return StartCoroutine(SendToGoogle(pcm));
    }

    byte[] ConvertToPCM16(float[] samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            byte[] b = BitConverter.GetBytes(s);
            result[i * 2] = b[0];
            result[i * 2 + 1] = b[1];
        }
        return result;
    }

    IEnumerator SendToGoogle(byte[] audioData)
    {
        string base64Audio = Convert.ToBase64String(audioData);

        string json = $@"
        {{
            ""config"": {{
                ""encoding"": ""LINEAR16"",
                ""sampleRateHertz"": {sampleRate},
                ""languageCode"": ""ja-JP""
            }},
            ""audio"": {{
                ""content"": ""{base64Audio}""
            }}
        }}";

        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={apiKey}";
        byte[] postData = Encoding.UTF8.GetBytes(json);

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, "POST"))
        {
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(postData);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("辨識成功：" + request.downloadHandler.text);
                ParseAndShowResult(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("辨識失敗：" + request.error);

            }
        }
    }

    void ParseAndShowResult(string json)
    {
        try
        {
            GoogleSpeechResponse response = JsonUtility.FromJson<GoogleSpeechResponse>(json);
            if (response.results != null && response.results.Length > 0)
            {
                string recognizedText = response.results[0].alternatives[0].transcript;
                Debug.Log("辨識文字：" + recognizedText);


                string currentCard = ARDisplayManager.currentCard;
                Debug.Log("當前卡片：" + currentCard);

                if (!string.IsNullOrEmpty(recognizedText))
                {
                    string firstChar = recognizedText.Substring(0, 1); // 取第一個字

                    if (imageToKana.ContainsKey(currentCard))
                    {
                        string[] possibleAnswers = imageToKana[currentCard].Split(' '); // 用空格分開成陣列

                        bool match = Array.Exists(possibleAnswers, answer => answer == firstChar);

                        if (match)
                        {
                            ShowResult(true);
                            Debug.Log("正確答案！");
                        }
                        else
                        {
                            ShowResult(false);
                            Debug.Log("錯誤答案！");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"找不到卡片 {currentCard} 的資料！");
                    }
                }
            }
            else
            {
                Debug.LogWarning("沒有辨識結果");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("解析結果時發生錯誤：" + e.Message);
        }
    }


    [Serializable]
    public class Alternative
    {
        public string transcript;
    }

    [Serializable]
    public class Result
    {
        public Alternative[] alternatives;
    }

    [Serializable]
    public class GoogleSpeechResponse
    {
        public Result[] results;
        public int resultIndex;
    }
    
       

    void ShowResult(bool isCorrect)
    {
        correctIcon.SetActive(isCorrect);
        wrongIcon.SetActive(!isCorrect);

        // 幾秒後自動隱藏
        StartCoroutine(HideResultAfterDelay(2f));
    }

    IEnumerator HideResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        correctIcon.SetActive(false);
        wrongIcon.SetActive(false);
    }

}
