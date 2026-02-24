using System;
using System.Collections;
using System.Collections.Generic;
using ModelContextProtocol.Samples;
using ModelContextProtocol.Unity;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public MCPForUnityServer server;
    // Start is called before the first frame update
    async void Start()
    {
        await server.StartServerAsync();
        server.Server.RegisterToolsFromClass(typeof(CustomTools));
    }
    

    private async void OnDestroy()
    {
        await server.StopServerAsync();
    }
}
