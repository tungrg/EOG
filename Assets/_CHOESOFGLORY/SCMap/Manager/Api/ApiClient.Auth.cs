using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public partial class ApiClient
{
    [SerializeField]
    public string authBaseUrl = "https://localhost:7126";

    public const string AccessToken = "AccessToken";
    
    public string GetAuthEndpoint(string endpointName) => $"{authBaseUrl}{endpointName}";
}
