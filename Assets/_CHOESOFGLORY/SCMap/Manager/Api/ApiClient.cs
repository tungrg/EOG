using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public partial class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instance
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if you want it to persist across scenes
        }
    }  
    
    public void Post<TRequest, TResponse>(string endpoint, TRequest data, Action<TResponse> onSuccess,
        Action<ProblemDetails> onError)
    {
        StartCoroutine(
            Request(
                UnityWebRequest.Post(endpoint, JsonUtility.ToJson(data),"application/json"),
            onSuccess, onError));
    }

    private IEnumerator Request<T>(
        UnityWebRequest webRequest,
        Action<T> onComplete,
        Action<ProblemDetails> onError)
    {
        using (webRequest)
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(JsonUtility.FromJson<T>(webRequest.downloadHandler.text));
            }
            else
            {
                var contentType = webRequest.GetResponseHeader("Content-Type");

                if (contentType != null && contentType.Contains("application/problem+json"))
                {
                    try
                    {
                        onError?.Invoke(JsonUtility.FromJson<ProblemDetails>(webRequest.downloadHandler.text));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Failed to parse application/problem+json: " + ex.Message);
                        onError?.Invoke(new ProblemDetails()
                        {
                            detail = "Something went wrong.",
                        });
                    }
                }
            }
        }
    }
}
