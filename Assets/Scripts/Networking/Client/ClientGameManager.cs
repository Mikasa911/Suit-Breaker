using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager
{
    private JoinAllocation allocation;

    private const string MenuSceneName = "Menu";

    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        AuthState authState = await AuthenticationWrapper.DoAuth();

        if (authState == AuthState.Authenticated)
        {
            return true;
        }

        return false;
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task<bool> StartClientAsync(bool isClassic, string joinCode = "s")
{
    joinCode = joinCode.ToUpper(); // FIXED

    GameMode clientMode = isClassic ? GameMode.Classic : GameMode.Special;
    Debug.Log($"[CLIENT] Trying to join mode: {clientMode}");
    Debug.Log($"[CLIENT] Join Code: {joinCode}");

    try
    {
        allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
    }
    catch (Exception e)
    {
        Debug.Log(e);
        return false; // ❌ failed to join
    }

    MyUtilities.LobbyCode = joinCode;

    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
    transport.SetRelayServerData(relayServerData);

    NetworkManager.Singleton.StartClient();

    return true; // ✅ success
}

    public void ExitClientRelay()
    {
        Debug.Log("[CLIENT] Exiting relay...");

        if (NetworkManager.Singleton == null)
            return;

        // Clear Relay transport FIRST
        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport != null)
            transport.SetRelayServerData(default);

        // Shutdown networking ONCE
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Clear cached values
        allocation = null;
        MyUtilities.LobbyCode = string.Empty;

        // Load menu
        SceneManager.LoadScene(MenuSceneName);
    }


}
