using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Soldier : NetworkBehaviour
{
    [SerializeField] int initalUnitSize = 1000;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float powerDecreaseAt = 500f;
    [SerializeField] float powerDecreasePercentage = 0.08f;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI unitSizeUI;
    [SerializeField] float damageFactor = 0.08f;
    [SerializeField] int playerNo;
    [SerializeField]
    Animator[] animators = new Animator[3];

    
    NetworkVariable<float> unitSize=new NetworkVariable<float>(1000f);
    bool inWar = false;
    float damage;
    Soldier enemySoldier;
    Barracks enemyBarracks;
    public int PlayerID { get { return playerNo; } }
    public bool InWar { get { return inWar; } set { inWar = value; } }
    public float UnitSize { get { return unitSize.Value; }}



    // Update is called once per frame
    void Update()
    {
        if (!inWar&&IsServer)
        {
            if ((playerNo == 0 && transform.position.z <= 109.4f) || (playerNo==1&& transform.position.z >= 20.6f))
            {
                if (playerNo==0)
                    transform.position += new Vector3(0, 0, Time.deltaTime * moveSpeed);
                else if(playerNo==1)
                    transform.position += new Vector3(0, 0, -Time.deltaTime * moveSpeed);
            }
            
            else
            {
                if (playerNo == 0)
                    transform.position += new Vector3(0, 0, -0.5f);
                else if (playerNo == 1)
                    transform.position += new Vector3(0, 0, +0.5f);
            }
        }

        if (IsClient)
        {
            unitSizeUI.text = ((int)unitSize.Value).ToString();
            slider.value = unitSize.Value;
        }
    }



    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<RocketClass>(out RocketClass rocket) && IsServer)
        {
            if (rocket.PlayerID != playerNo)
            {
                NewUnitSizeServerRpc(rocket.Damage * (-1));

                if(unitSize.Value <= 0) 
                {
                    if (enemySoldier != null)
                    {
                        enemySoldier.inWar = false;
                        foreach (Animator a in enemySoldier.animators)
                        {
                            a.SetBool("shooting", false);
                        }
                    }
                    Destroy(gameObject);
                }

                float x = Vector2.Distance(new Vector2(transform.GetChild(0).position.x, transform.GetChild(0).position.z), new Vector2(collision.transform.position.x, collision.transform.position.z));
                /*
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(collision.transform.position.x, collision.transform.position.z)) <= rocket.Radius)
                {
                    float damage = rocket.Damage * (1 - (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(collision.transform.position.x, collision.transform.position.z)) / (rocket.Radius*2)));
                    NewUnitSizeServerRpc(damage * (-1));
                }*/
            }
        }
        

        if (collision.gameObject.TryGetComponent<Soldier>(out Soldier soldier)&& IsServer)
        {
            // enemy soldiers meet
            if (soldier.playerNo != playerNo)
            {
                // if not inWar then start war
                if (!inWar)
                {
                    inWar = true;
                    foreach (Animator a in animators)
                    {
                        a.SetBool("shooting", true);
                    }
                    enemySoldier = collision.gameObject.GetComponent<Soldier>();

                    // player0 controls the war
                    if (playerNo == 0)
                    {
                        StartCoroutine(SoldierWar());
                    }
                }
                else
                {
                    enemyBarracks.StopAllCoroutines();
                    enemySoldier = collision.gameObject.GetComponent<Soldier>();
                    // player0 controls the war
                    if (playerNo == 0)
                    {
                        StartCoroutine(SoldierWar());
                    }
                }

            }
            // allied soldier meet
            else
            {
                if (inWar)
                {
                    GetReinforcement(soldier.unitSize.Value);
                    Destroy(soldier.gameObject);
                }
                else
                {
                    if (playerNo == 0&& transform.position.z>soldier.transform.position.z)
                    {
                        GetReinforcement(soldier.unitSize.Value);
                        Destroy(soldier.gameObject);
                    }
                    else if(playerNo==1&& transform.position.z < soldier.transform.position.z)
                    {
                        GetReinforcement(soldier.unitSize.Value);
                        Destroy(soldier.gameObject);
                    }
                }
            }
        }
        else if(collision.gameObject.TryGetComponent<Barracks>(out Barracks barracks) && IsServer)
        {
            if (barracks.PlayerID != playerNo)
            {
                inWar = true;
                foreach (Animator a in animators)
                {
                    a.SetBool("shooting", true);
                }
                enemyBarracks = barracks;
            }
        }
    }



    public float GetDamage()
    {
        int iterationCount = (int)(unitSize.Value / powerDecreaseAt);
        damage = 0;
        for(int i=0;i< iterationCount ; i++)
        {
            damage += powerDecreaseAt * Mathf.Max(0,1f-i* powerDecreasePercentage);
        }
        damage += (unitSize.Value % powerDecreaseAt) * Mathf.Max(0,1f - iterationCount * powerDecreasePercentage);
        return damage;
    }

    IEnumerator SoldierWar()
    {
        while (inWar && IsServer)
        {
            float damage0= GetDamage()* damageFactor * (-1);
            float damage1 = enemySoldier.GetDamage()*damageFactor * (-1);

            NewUnitSizeServerRpc(damage1);
            enemySoldier.NewUnitSizeServerRpc(damage0);

            if (unitSize.Value <= 1)
            {
                enemySoldier.inWar = false;
                foreach (Animator a in enemySoldier.animators)
                {
                    a.SetBool("shooting", false);
                }
                enemySoldier.enemySoldier = null;
                Destroy(gameObject);
            }
            if (enemySoldier.unitSize.Value <= 1)
            {
                inWar = false;
                foreach (Animator a in animators)
                {
                    a.SetBool("shooting", false);
                }
                Destroy(enemySoldier.gameObject);
                enemySoldier = null;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void GetReinforcement(float reinforceUnitSize)
    {
        if(IsServer)
        NewUnitSizeServerRpc(reinforceUnitSize);
    }


    [ServerRpc(RequireOwnership =false)]
    public void NewUnitSizeServerRpc(float newSize)
    {
        unitSize.Value += newSize;
    }

}
