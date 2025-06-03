using UnityEngine;

public static class APIKeyLoader
{
    private static string apiKey;

    public static string GetAPIKey()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            TextAsset keyFile = Resources.Load<TextAsset>("APIkey");
            if (keyFile != null)
            {
                apiKey = keyFile.text.Trim();
            }
            else
            {
                Debug.LogError("API 金鑰載入失敗！");
            }
        }
        return apiKey;
    }
}
