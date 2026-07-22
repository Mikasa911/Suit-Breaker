using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager
{
    private Allocation allocation;
    private string joinCode;

    private const int MaxConnections = 4;
    private const string ClassicGameSceneName = "ClassicGame";
    private const string SpecialGameSceneName = "SpecialGame";
    private GameMode HostedMode;

    public GameMode GetGameMode()
    {
        return HostedMode;
    }
    public void SetGameMode(bool isClassic)
    {
        if (isClassic)
            HostedMode = GameMode.Classic;
        else
            HostedMode = GameMode.Special;
    }
    public async Task<bool> StartHostAsync(bool isClassic)
    {
        HostedMode = isClassic ? GameMode.Classic : GameMode.Special;
        Debug.Log($"[HOST] Hosting mode: {HostedMode}");

        try
        {
            allocation = await Relay.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return false;
        }

        try
        {
            joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return false;
        }

        MyUtilities.LobbyCode = joinCode;
        Debug.Log($"[HOST] Join Code: {joinCode}");

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();

        // SUCCESS → load scene
        NetworkManager.Singleton.SceneManager.LoadScene(SpecialGameSceneName, LoadSceneMode.Single);

        return true;
    }

    public void ExitHostRelay()
    {
        Debug.Log("[HOST] Closing relay...");

        if (NetworkManager.Singleton == null)
            return;

        // Clear Relay transport FIRST
        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport != null)
            transport.SetRelayServerData(default);

        // Shutdown networking ONCE (this disconnects all clients)
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Clear local cached data
        allocation = null;
        joinCode = string.Empty;
        MyUtilities.LobbyCode = string.Empty;

        // Load menu
        SceneManager.LoadScene("Menu");
    }

}
