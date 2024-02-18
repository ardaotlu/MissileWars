using DilmerGames.Core.Singletons;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{

    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private InputField joinCodeInput;

    //private PlayersMan playersManager;
    private bool hasServerStarted;

    //private GameHost gameHost;
    private bool waiting = false;


    private void Awake()
    {
        Cursor.visible = true;
    }    

    void Update()
    {/*
        if (waiting)
        {
            //playersInGameText.text = "WAITING FOR OTHERS..\n" + playersManager.PlayersInGame + "/" + playersManager.TotalPlayers;
            if (playersManager.PlayersInGame == playersManager.TotalPlayers)
            {
                waiting = false;
                playersManager.EveryoneConnectedCaller();
            }
        }*/
    }

    void Start()
    {
        //playersManager = FindObjectOfType<PlayersMan>();
        //gameHost = FindObjectOfType<GameHost>();


        // START SERVER
        /*startServerButton?.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton.StartServer())
                Logger.Instance.LogInfo("Server started...");
            else
                Logger.Instance.LogInfo("Unable to start server...");
        });*/

        // START HOST
        startHostButton?.onClick.AddListener(async () =>
        {
            // this allows the UnityMultiplayer and UnityMultiplayerRelay scene to work with and without
            // relay features - if the Unity transport is found and is relay protocol then we redirect all the 
            // traffic through the relay, else it just uses a LAN type (UNET) communication.
            if (RelayManager.Instance.IsRelayEnabled) 
                await RelayManager.Instance.SetupRelay();

            NetworkManager.Singleton.StartHost();
            waiting = true;

        });

        // START CLIENT
        startClientButton?.onClick.AddListener(async () =>
        {
            if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCodeInput.text))
                await RelayManager.Instance.JoinRelay(joinCodeInput.text);

            NetworkManager.Singleton.StartClient();

            waiting = true;

        });

        // STATUS TYPE CALLBACKS
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {

        };

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            hasServerStarted = true;
        };

    }
}
