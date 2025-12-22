using UnityEngine;

public class DebugLogger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Unity Project Debug Logger Started");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Render Pipeline: {QualitySettings.renderPipeline?.name ?? "Built-in"}");
    }

    void Update()
    {
        // Runtime hataları için loglama
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed - Testing audio system");
        }
    }
}
