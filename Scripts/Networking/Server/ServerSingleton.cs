using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    private static ServerSingleton instance;

    public ServerGameManager GameManager;

    public static ServerSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; }

            instance = FindObjectOfType<ServerSingleton>();

            if (instance == null)
            {
                Debug.LogError("No ServerSingleton in the scene!");
                return null;
            }

            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateServer()
    {
        GameManager = new ServerGameManager();
    }
}
