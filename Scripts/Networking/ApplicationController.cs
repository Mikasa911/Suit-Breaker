using System.Collections;
using System.Collections.Generic;
using Unity.Networking;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

public class ApplicationController :NetworkBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private ServerSingleton serverPrefab;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {

        }
        else
        {
            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();

            ClientSingleton clientSingleton = Instantiate(clientPrefab);        
            bool authenticated = await clientSingleton.CreateClient();

            

            // Go to main menu
            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }
        }

    }
}
