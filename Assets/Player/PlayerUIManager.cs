using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : NetworkBehaviour
{
    Player playerToGet;
    GameObject myCanvas;
    Text manpowerText;
    Text goldText;
    Text res0Text;
    Text res1Text;
    Text res2Text;
    Text res3Text;

    void Start()
    {
        if (IsOwner)
        {
            playerToGet = transform.gameObject.GetComponent<Player>();

            if (OwnerClientId == 0) 
            {
                myCanvas = GameObject.FindWithTag("P0");
            }

            else if (OwnerClientId == 1) 
            {
                myCanvas = GameObject.FindWithTag("P1");
            }

            manpowerText=myCanvas.transform.GetChild(1).gameObject.GetComponentInChildren<Text>();
            goldText = myCanvas.transform.GetChild(2).gameObject.GetComponentInChildren<Text>();
            res0Text = myCanvas.transform.GetChild(3).gameObject.GetComponentInChildren<Text>();
            res1Text = myCanvas.transform.GetChild(4).gameObject.GetComponentInChildren<Text>();
            res2Text = myCanvas.transform.GetChild(5).gameObject.GetComponentInChildren<Text>();
            res3Text = myCanvas.transform.GetChild(6).gameObject.GetComponentInChildren<Text>();


        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsOwner && IsClient)
        {
            
            manpowerText.text = playerToGet.Manpower.ToString()+" +50";
            goldText.text = playerToGet.Gold.ToString()+" +"+playerToGet.GoldProduction;
            res0Text.text = playerToGet.Res0.ToString();
            res1Text.text = playerToGet.Res1.ToString();
            res2Text.text = playerToGet.Res2.ToString();
            res3Text.text = playerToGet.Res3.ToString();
        }
    }
}
