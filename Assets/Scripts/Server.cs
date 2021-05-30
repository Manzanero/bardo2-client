#pragma warning disable 0649

using System;
using System.Collections;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

public class Server : MonoBehaviour
{
    public static bool serverReady;
    public static string BaseUrl { get; private set; }
    private static string _basicAuth;

    public static void SetCredentials(string username, string password)
    {
        _basicAuth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
    }

    private void Start()
    {
        var url = Application.absoluteURL;
        BaseUrl = url == "" ? "http://localhost" : url.Substring(0, url.IndexOf('/', 10));

        serverReady = true;
    }

    public class Response
    {
        public int status;
        public string message;
        public string date;
        public bool exception;

        public static implicit operator bool(Response value)
        {
            return !value.exception;
        }
    }

    private static UnityWebRequest AddCommonHeaders(UnityWebRequest request)
    {
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", _basicAuth);
        return request;
    }

    public static UnityWebRequest GetRequest(string url)
    {
        var request = UnityWebRequest.Get(url);
        request = AddCommonHeaders(request);
        request.SendWebRequest();
        return request;
    }

    public static UnityWebRequest PostRequest<T>(string url, T data)
    {
        var request = new UnityWebRequest(url) {method = "POST"};
        var json = data is string s ? s : JsonUtility.ToJson(data);
        var uploader = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)) {contentType = "application/json"};
        request.uploadHandler = uploader;
        request = AddCommonHeaders(request);
        request.SendWebRequest();
        return request;
    }

    public static UnityWebRequest DeleteRequest(string url)
    {
        var request = UnityWebRequest.Delete(url);
        request = AddCommonHeaders(request);
        request.SendWebRequest();
        return request;
    }

    public static T GetResponse<T>(UnityWebRequest request) where T : Response, new()
    {
        return GetResponse<T>(request, true);
    }

    public static T GetResponse<T>(UnityWebRequest request, bool raiseExceptions) where T : Response, new()
    {
        if (request.method == "POST") request.uploadHandler.Dispose();
        var data = request.downloadHandler.data;
        if (data == null)
            throw new Exception($"[Server] no data");

        var jsonResponse = Encoding.Default.GetString(data);
        var serializable = new T();
        try
        {
            try
            {
                JsonUtility.FromJsonOverwrite(jsonResponse, serializable);
            }
            catch (ArgumentException)
            {
                throw new Exception($"[Server] JSON error: {jsonResponse}");
            }

            if (serializable.status >= 300)
                throw new Exception($"[Server] Status {serializable.status}. Error: {serializable.message}. " +
                                    $"Url: {request.url}");
            if (request.result.ToString() != "Success")
                throw new Exception($"[Server] Result: {request.result}. Body (if any): {jsonResponse}");
            if (World.debugging)
                Debug.Log($"[Server] message: {serializable.message}. " +
                          $"Url: {request.url}. " +
                          $"Response: {jsonResponse}");
            return serializable;
        }
        catch (Exception e)
        {
            if (raiseExceptions) throw;

            if (World.debugging) Debug.LogWarning(e.Message);

            serializable.exception = true;
            return serializable;
        }
    }
}