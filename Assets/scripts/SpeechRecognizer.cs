using UnityEngine;
using Vosk;
using System;
using System.Text;

public class SpeechRecognizer : MonoBehaviour
{
    private Model model;
    private VoskRecognizer recognizer;
    private AudioClip mic;
    private const int sampleRate = 16000;
    private int lastSample = 0;

    void Start()
    {
        Vosk.Vosk.SetLogLevel(0); // 靜音 log
        model = new Model(Application.streamingAssetsPath + "/vosk-model-small-ja-0.22");
        recognizer = new VoskRecognizer(model, sampleRate);

        mic = Microphone.Start(null, true, 10, sampleRate);
    }

    void Update()
    {
        int currentPos = Microphone.GetPosition(null);
        int diff = currentPos - lastSample;
        if (diff > 0)
        {
            float[] samples = new float[diff];
            mic.GetData(samples, lastSample);
            byte[] pcm = ConvertToPCM16(samples);

            if (recognizer.AcceptWaveform(pcm, pcm.Length))
            {
                Debug.Log("Final: " + recognizer.Result());
            }
            else
            {
                Debug.Log("Partial: " + recognizer.PartialResult());
            }
        }

        lastSample = currentPos;
    }

    byte[] ConvertToPCM16(float[] samples)
    {
        byte[] result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(samples[i] * short.MaxValue);
            byte[] b = BitConverter.GetBytes(s);
            result[i * 2] = b[0];
            result[i * 2 + 1] = b[1];
        }
        return result;
    }
}
