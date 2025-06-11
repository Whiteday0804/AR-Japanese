using UnityEngine;
using Unity.Barracuda;
using Vuforia;
using System.Collections;

public class ObjectDetector : MonoBehaviour
{
    [Header("Model Settings")]
    public NNModel modelAsset;
    public Vector2Int inputSize = new Vector2Int(320, 240);
    
    [Header("UI Settings")]
    public GameObject canvasToShow; // 指向包含圖片與按鈕的 UI GameObject
    
    [Header("Performance Settings")]
    [Range(0.001f, 0.5f)]
    private float confidenceThreshold = 0.00001121f;
    public bool useGPUWorker = true; // 優先使用GPU
    
    private Model model;
    private IWorker worker;
    private RenderTexture rt;
    private Camera mainCamera;
    private bool isProcessing = false;
    private bool hasPlayedSound = false;
    
    // 物件池化用的變數
    private Texture2D reusableTexture;
    private Texture2D reusableResizedTexture;
    
    // 幀跳過機制 - 用於在保持高頻率的同時控制處理負載
    private int frameSkipCount = 0;
    private int frameSkipInterval = 2; // 每3幀處理一次（約20FPS檢測）
    
    // 效能自適應
    private float avgProcessingTime = 0f;
    private int processedFrames = 0;
    private const float targetProcessingTime = 0.033f; // 目標：每次處理不超過33ms

    void Start()
    {
        InitializeDetector();
    }

    void InitializeDetector()
    {
        try
        {
            // 載入模型
            model = ModelLoader.Load(modelAsset);
            
            // 根據平台選擇最佳的 Worker 類型
            WorkerFactory.Type workerType = DetermineOptimalWorkerType();
            worker = WorkerFactory.CreateWorker(workerType, model);
            
            // 初始化相機和渲染紋理
            mainCamera = Camera.main;
            rt = new RenderTexture(inputSize.x, inputSize.y, 0, RenderTextureFormat.RGB565);
            rt.filterMode = FilterMode.Bilinear;
            rt.Create();
            
            // 預先分配紋理以避免運行時分配
            reusableTexture = new Texture2D(inputSize.x, inputSize.y, TextureFormat.RGB565, false);
            reusableResizedTexture = new Texture2D(inputSize.x, inputSize.y, TextureFormat.RGB565, false);
            
            // 初始隱藏 UI
            if (canvasToShow != null)
                canvasToShow.SetActive(false);
            
            Debug.Log($"ObjectDetector initialized with worker type: {workerType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize ObjectDetector: {e.Message}");
        }
    }

    WorkerFactory.Type DetermineOptimalWorkerType()
    {
        // 根據平台和設定選擇最佳 Worker
        if (useGPUWorker && SystemInfo.supportsComputeShaders)
        {
            return WorkerFactory.Type.ComputePrecompiled;
        }
        else if (SystemInfo.processorCount >= 4)
        {
            return WorkerFactory.Type.CSharpBurst;
        }
        else
        {
            return WorkerFactory.Type.CSharp;
        }
    }

    void Update()
    {
        // 使用 Update + 幀跳過機制，在高頻率檢測與效能間取得平衡
        frameSkipCount++;
        
        if (frameSkipCount >= frameSkipInterval && !isProcessing)
        {
            frameSkipCount = 0;
            StartCoroutine(ProcessFrame());
        }
        
        // 動態調整幀跳過間隔以維持效能
        AdjustPerformanceSettings();
    }

    IEnumerator ProcessFrame()
    {
        isProcessing = true;
        float startTime = Time.realtimeSinceStartup;
        
        // 捕獲相機幀
        bool captureSuccess = CaptureVuforiaCameraFrame();
        if (!captureSuccess)
        {
            isProcessing = false;
            yield break;
        }

        Tensor input = null;
        Tensor output = null;

        try
        {
            // 轉換輸入
            input = TransformInput(reusableTexture);
            
            // 執行推理
            worker.Execute(input);
            
            // 獲取輸出
            output = worker.PeekOutput();
            
            // 處理檢測結果
            bool detectionFound = ProcessDetectionResults(output);
            
            // 更新UI狀態
            UpdateUIState(detectionFound);
            
            // 記錄處理時間用於效能調整
            float processingTime = Time.realtimeSinceStartup - startTime;
            UpdatePerformanceMetrics(processingTime);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ProcessFrame: {e.Message}");
        }
        finally
        {
            // 清理資源
            input?.Dispose();
            output?.Dispose();
            isProcessing = false;
        }

        // 非同步等待，避免完全阻塞
        yield return null;
    }

    bool CaptureVuforiaCameraFrame()
    {
        try
        {
            VuforiaBehaviour vb = VuforiaBehaviour.Instance;
            if (vb == null || vb.DevicePoseBehaviour == null || mainCamera == null)
                return false;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = rt;
            
            mainCamera.targetTexture = rt;
            mainCamera.Render();
            
            // 直接讀取到預分配的紋理
            reusableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            reusableTexture.Apply();

            mainCamera.targetTexture = null;
            RenderTexture.active = currentRT;
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error capturing camera frame: {e.Message}");
            return false;
        }
    }

    Tensor TransformInput(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        float[] floatValues = new float[inputSize.x * inputSize.y * 3];

        // 優化的像素轉換
        int pixelCount = pixels.Length;
        for (int i = 0; i < pixelCount; ++i)
        {
            int baseIndex = i * 3;
            Color32 pixel = pixels[i];
            
            // 使用位運算優化除法
            floatValues[baseIndex] = pixel.r * 0.003921569f; // 1/255
            floatValues[baseIndex + 1] = pixel.g * 0.003921569f;
            floatValues[baseIndex + 2] = pixel.b * 0.003921569f;
        }

        return new Tensor(1, inputSize.y, inputSize.x, 3, floatValues);
    }

    bool ProcessDetectionResults(Tensor output)
    {
        float bestConf = 0f;
        float bestX = 0f, bestY = 0f;

        int numDetections = output.shape[2]; // 3549 detections
        int numAttributes = output.shape[3]; // 13 attributes per detection

        // 只檢查前面的檢測結果以提高效能
        int maxDetectionsToCheck = Mathf.Min(1000, numDetections);
        
        for (int i = 0; i < maxDetectionsToCheck; i++)
        {
            float conf = output[0, 0, i, 4]; // Confidence at index 4
            
            if (conf > bestConf)
            {
                bestConf = conf;
                bestX = output[0, 0, i, 0]; // Center x at index 0
                bestY = output[0, 0, i, 1]; // Center y at index 1
            }
            
            // 早期退出：如果已經找到高信心度的檢測
            if (bestConf > confidenceThreshold) break;
        }

        bool detectionFound = bestConf >= confidenceThreshold;
        
        if (detectionFound)
        {
            // 只在需要時計算世界位置
            Vector3 worldPos = EstimateWorldPositionOfObject(bestX, bestY);
            Debug.Log($"Object detected - conf: {bestConf}, world pos: {worldPos}");
        }

        return detectionFound;
    }

    Vector3 EstimateWorldPositionOfObject(float normalizedX, float normalizedY)
    {
        // 轉換標準化座標到像素座標
        int pixelX = (int)(normalizedX * inputSize.x);
        int pixelY = (int)((1f - normalizedY) * inputSize.y); // 翻轉Y軸

        // 轉換到世界位置
        Vector3 screenPoint = new Vector3(pixelX, pixelY, 0.5f);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPoint);

        return worldPos;
    }

    void UpdateUIState(bool detectionFound)
    {
        if (canvasToShow == null) return;

        // 直接更新UI狀態，確保即時響應
        if (canvasToShow.activeSelf != detectionFound)
        {
            canvasToShow.SetActive(detectionFound);
        }
    }

    void UpdatePerformanceMetrics(float processingTime)
    {
        processedFrames++;
        avgProcessingTime = (avgProcessingTime * (processedFrames - 1) + processingTime) / processedFrames;
        
        // 每100幀重置統計，避免數值過大
        if (processedFrames >= 100)
        {
            processedFrames = 50;
            avgProcessingTime *= 0.5f;
        }
    }

    void AdjustPerformanceSettings()
    {
        // 根據平均處理時間動態調整幀跳過間隔
        if (avgProcessingTime > targetProcessingTime)
        {
            // 處理太慢，增加幀跳過間隔
            frameSkipInterval = Mathf.Min(frameSkipInterval + 1, 5);
        }
        else if (avgProcessingTime < targetProcessingTime * 0.5f && frameSkipInterval > 1)
        {
            // 處理很快，可以減少幀跳過間隔
            frameSkipInterval = Mathf.Max(frameSkipInterval - 1, 1);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // 當應用暫停時停止處理以節省電池
        if (pauseStatus)
        {
            StopAllCoroutines();
        }
        else if (gameObject.activeInHierarchy)
        {
            // 重新開始處理
            frameSkipCount = 0;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // 當應用失去焦點時也停止處理
        if (!hasFocus)
        {
            StopAllCoroutines();
        }
        else if (gameObject.activeInHierarchy)
        {
            // 重新開始處理
            frameSkipCount = 0;
        }
    }

    void OnDestroy()
    {
        // 清理所有資源
        StopAllCoroutines();
        
        worker?.Dispose();
        
        if (rt != null)
        {
            rt.Release();
            DestroyImmediate(rt);
        }
        
        if (reusableTexture != null)
            DestroyImmediate(reusableTexture);
            
        if (reusableResizedTexture != null)
            DestroyImmediate(reusableResizedTexture);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void OnEnable()
    {
        if (worker != null)
        {
            frameSkipCount = 0;
        }
    }

    public void SetFrameSkipInterval(int skipFrames)
    {
        frameSkipInterval = Mathf.Clamp(skipFrames, 1, 10);
    }

    public float GetCurrentDetectionFPS()
    {
        return frameSkipInterval > 0 ? (1f / Time.deltaTime) / frameSkipInterval : 0f;
    }

    public float GetAverageProcessingTime()
    {
        return avgProcessingTime * 1000f; // 轉換為毫秒
    }

    public int GetCurrentFrameSkipInterval()
    {
        return frameSkipInterval;
    }
}