using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class ARDisplayManager : MonoBehaviour
{
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public QuestionManager questionManager;
    public static string currentCard;
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

        if (behaviour == null) return;

        Transform canvas = null;
        foreach (Transform child in behaviour.transform)
        {
            if (child.name.StartsWith("canva_"))
            {
                canvas = child;
                break;
            }
        }
        if (canvas == null)
        {
            Debug.LogWarning($"找不到 canvas_* 物件 in {behaviour.name}");
            return;
        }
        // 全部子元件先關掉
        foreach (Transform child in canvas)
        {
            child.gameObject.SetActive(false);
        }
        if (isTracked)
        {
            
            switch (AppStateManager.CurrentState)
            {
                case AppState.Home:
                    bgmSource.clip = bgmClip;
                    bgmSource.volume = 0.5f;
                    bgmSource.loop = true;
                    bgmSource.Play();
                    break;
                case AppState.Tutorial:
                    bgmSource.Pause();
                    ShowChildIfExists(canvas, "sound");
                    ShowChildIfExists(canvas, "word");
                    ShowChildIfExists(canvas, "panel_word");
                    questionManager.HideAllEffects();
                    break;
                case AppState.Questions:
                    bgmSource.Pause();
                    string detectedName = behaviour.TargetName;
                    Debug.Log($"偵測到圖卡：{detectedName}");
                    questionManager.OnCardDetected(detectedName);
                    break;
                case AppState.Voice:
                    bgmSource.Pause();
                    string targetName = behaviour.TargetName;
                    currentCard = targetName;
                    Debug.Log($"完整 target name: {currentCard}");
                    ShowChildIfExists(canvas, "voice");
                    ShowChildIfExists(canvas, "panel_word");
                    questionManager.HideAllEffects();
                    break;

                default:
                    // 其他狀態不顯示
                    break;
            }
        }

        // ✅ 新增：當圖卡開始追蹤到時，通知 GameManager
        // if (isTracked && AppStateManager.CurrentState == AppState.Questions)
        // {
        //     string detectedName = behaviour.TargetName;
        //     Debug.Log($"偵測到圖卡：{detectedName}");
        //     questionManager.OnCardDetected(detectedName);
        // }
        


    }
     void ShowChildIfExists(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"找不到子物件 {childName} in {parent.name}");
        }
    }
}
