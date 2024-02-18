using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Factory : NetworkBehaviour
{
    [SerializeField] int playerNo;
    [SerializeField] int factoryNo;

    public int PlayerID { get { return playerNo; } }
    public int FactoryNo { get { return factoryNo; } }
}
