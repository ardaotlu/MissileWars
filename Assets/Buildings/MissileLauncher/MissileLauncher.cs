using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MissileLauncher : RocketLauncher
{
    [SerializeField] static int initalUnitSize = 2500;
    NetworkVariable<float> unitSize = new NetworkVariable<float>(initalUnitSize);



}
