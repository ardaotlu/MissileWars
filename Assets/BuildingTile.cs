using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingTile : MonoBehaviour
{
    [SerializeField] int playerID;
    [SerializeField] int launcherNo;
    public int PlayerID { get { return playerID; } }
    public int LauncherNo { get { return launcherNo; } }

}
