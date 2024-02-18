using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RangeMenuID : NetworkBehaviour
{
    [SerializeField] int rangeMenuID;
    public int RangeMenuNo { get { return rangeMenuID; } }
}
