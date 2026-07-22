using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTurnManager : NetworkBehaviour
{
   /* //NetworkVariable<ulong> playerID;
    [SerializeField] Button startGameButton;

    private void Start()
    {
        startGameButton.gameObject.SetActive(false);

    }
    public override void OnNetworkSpawn()
    {
        if (IsClient) 
        {
            Destroy(startGameButton.gameObject);
            return;
        }
        startGameButton.gameObject.SetActive(true);
    }*/
}
