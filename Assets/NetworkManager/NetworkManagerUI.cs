using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    private NetworkManager networkManager;
    [SerializeField] private Button HostBut;
    [SerializeField] private Button ClientBut;
    private bool waiting = false;

    private void Start()
    {
        HostBut.onClick.AddListener(() =>
        {
            networkManager = GetComponentInParent<NetworkManager>();
            networkManager.StartHost();
        });

        ClientBut.onClick.AddListener(() =>
        {
            networkManager = GetComponentInParent<NetworkManager>();
            networkManager.StartClient();
        });
    }


}
