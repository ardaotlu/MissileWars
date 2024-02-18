using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RocketClass : NetworkBehaviour
{
    [SerializeField] float damage;
    [SerializeField] float radius;
    [SerializeField] float time;
    [SerializeField] float range;
    [SerializeField] int playerID;

    public int PlayerID { get { return playerID; } }
    public float Radius { get { return radius; } }

    public float Range { get { return range; } }
    public float Time { get { return time; } }

    public float Damage { get { return damage; } }


}
