using UnityEngine;
using System.Collections;
using System.IO;

public static class APIKeyLoader
{
    private static string apiKey = null;

    // 取得 API Key（同步版本）
    public static string GetAPIKey()
    {
        if (apiKey != null)
            return apiKey;

        string path = Path.Combine(Application.streamingAssetsPath, "apikey.txt");

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 的 StreamingAssets 必須用 UnityWebRequest 讀
        // 但同步讀不到，這裡只能先回空字串
        Debug.LogWarning("Android 平台請改用 GetAPIKeyAsync()");
        return "";
#else
        if (File.Exists(path))
        {
            apiKey = File.ReadAllText(path).Trim();
            return apiKey;
        }
        else
        {
            Debug.LogError("API Key 檔案不存在：" + path);
            return "";
        }
#endif
    }

    // 取得 API Key（Android 可用的非同步版本）
    public static IEnumerator GetAPIKeyAsync(System.Action<string> callback)
    {
        if (apiKey != null)
        {
            callback?.Invoke(apiKey);
            yield break;
        }

        string path = Path.Combine(Application.streamingAssetsPath, "apikey.txt");

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                apiKey = www.downloadHandler.text.Trim();
                callback?.Invoke(apiKey);
            }
            else
            {
                Debug.LogError("讀取 API Key 失敗：" + www.error);
                callback?.Invoke("");
            }
        }
#else
        if (File.Exists(path))
        {
            apiKey = File.ReadAllText(path).Trim();
            callback?.Invoke(apiKey);
        }
        else
        {
            Debug.LogError("API Key 檔案不存在：" + path);
            callback?.Invoke("");
        }
#endif
    }
}
