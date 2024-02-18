using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

public class RocketLauncher : NetworkBehaviour
{
    [SerializeField] int initalUnitSize = 1500;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI unitSizeUI;
    [SerializeField] float damageFactor = 0.08f;
    [SerializeField] int launcherNo;
    [SerializeField] int playerNo;
    Transform target;
    Transform weapon;


    NetworkVariable<float> unitSize = new NetworkVariable<float>(0f);


    public int PlayerID{get { return playerNo; }}
    public int LauncherNo{get { return launcherNo; }}

    void Start()
    {
        slider=GetComponentInChildren<Slider>();
        unitSizeUI=GetComponentInChildren<TextMeshProUGUI>();
        weapon = transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.transform;

        if (IsServer)
        {
            NewUnitSizeServerRpc(initalUnitSize);
        }

    }

    private void Update()
    {
        if (IsClient)
        {
            unitSizeUI.text = ((int)unitSize.Value).ToString();
            slider.value = unitSize.Value;
        }
        if(IsServer)
        {
            FindClosestTarget();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<RocketClass>(out RocketClass rocket) && IsServer)
        {
            Debug.Log("hit rocket");
            if (rocket.PlayerID != playerNo)
            {
                Debug.Log("enemy rocket");
                NewUnitSizeServerRpc(rocket.Damage * (-1));

                if (unitSize.Value <= 0)
                {                    
                    Destroy(gameObject);
                }

                /*
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(collision.transform.position.x, collision.transform.position.z)) <= rocket.Radius)
                {

                    float damage = rocket.Damage * (1 - Vector3.Distance(transform.position, collision.transform.position) / rocket.Radius);
                    NewUnitSizeServerRpc(damage * (-1));
                }*/
            }
        }
    }

    IEnumerator DecreaseHealth()
    {
        yield return new WaitForSeconds(1f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NewUnitSizeServerRpc(float newSize)
    {
        unitSize.Value += newSize;
    }


    void FindClosestTarget()
    {
        Soldier[] enemies = FindObjectsOfType<Soldier>();

        Transform closestTarget = null;
        float maxDistance = Mathf.Infinity;

        foreach (Soldier e in enemies)
        {
            if(playerNo!=e.PlayerID)
            {
                float distance = Vector3.Distance(transform.position, e.transform.position);
                if (distance < maxDistance)
                {
                    closestTarget = e.transform;
                    maxDistance = distance;
                }
            }
        }
        target = closestTarget;

        if (closestTarget != null)
        {
            weapon.LookAt(target);
        }

    }


}
