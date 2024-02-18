using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Barracks : NetworkBehaviour
{
    [SerializeField] static int initalUnitSize = 10000;
    //[SerializeField] float powerDecreaseAt = 500f;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI unitSizeUI;
    [SerializeField] float damageFactor = 0.08f;
    [SerializeField] int barracksNo;
    [SerializeField] int playerNo;

    public event EventHandler player0BarracksDestroyed;
    public event EventHandler player1BarracksDestroyed;

    //enemy player
    Player enemyPlayer;
    bool enemyPlayerPicked = false;

    NetworkVariable<float> unitSize = new NetworkVariable<float>(initalUnitSize);
    bool inWar = false;
    //float damage;
    Soldier enemySoldier;


    public int PlayerID {get { return playerNo; }}
    public bool InWar { get { return inWar; } set { inWar = value; } }
    public int BarracksNo{get { return barracksNo; }}


    private void Update()
    {
        if (IsServer&&!enemyPlayerPicked)
        {
            Player[] players=FindObjectsOfType<Player>();
            foreach(Player p in players)
            {
                if((int)p.OwnerClientId!= playerNo) 
                {
                    enemyPlayerPicked = true;
                    enemyPlayer = p;
                }
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

                if (unitSize.Value <= 0)
                {
                    if (playerNo == 0)
                        player0BarracksDestroyed(this, EventArgs.Empty);
                    if (playerNo == 1)
                        player1BarracksDestroyed(this, EventArgs.Empty);

                    Soldier[] soldiers = GameObject.FindObjectsOfType<Soldier>();
                    foreach (Soldier s in soldiers)
                    {
                        if (s.PlayerID != playerNo && s.transform.position.x > (transform.position.x - 0.5f) && s.transform.position.x < (transform.position.x + 0.5f))
                        {
                            enemyPlayer.BarracksDisableRequest(BarracksNo);
                            enemyPlayer.ManpowerIncreaseRequest(((int)s.UnitSize) / 2);
                            enemyPlayer.GoldIncreaseRequest((int)(s.UnitSize * 0.45f) / 2);
                            Destroy(s.gameObject);
                        }
                    }
                    gameObject.GetComponent<NetworkObject>().Despawn();

                    //StartCoroutine(DelayedDeath());
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


        if (collision.gameObject.TryGetComponent<Soldier>(out Soldier soldier)&&IsServer)
        {
            if (soldier.PlayerID != playerNo)
            {
                inWar = true;

                enemySoldier = collision.gameObject.GetComponent<Soldier>();
                StartCoroutine(DecreaseHealth());
            }
        }
    }

    IEnumerator DecreaseHealth()
    {
        while (inWar && IsServer)
        {            
            NewUnitSizeServerRpc(enemySoldier.GetDamage() * damageFactor * (-1));

            if (unitSize.Value <= 0)
            {
                if (playerNo == 0)
                    player0BarracksDestroyed(this, EventArgs.Empty);
                if (playerNo == 1)
                    player1BarracksDestroyed(this, EventArgs.Empty);

                enemySoldier.InWar = false;
                Soldier[] soldiers = GameObject.FindObjectsOfType<Soldier>();
                foreach (Soldier s in soldiers)
                {
                    if (s.PlayerID != playerNo && s.transform.position.x > (transform.position.x - 0.5f) && s.transform.position.x < (transform.position.x + 0.5f))
                    {
                        
                        enemyPlayer.BarracksDisableRequest(BarracksNo);
                        enemyPlayer.ManpowerIncreaseRequest(((int)s.UnitSize) / 2);
                        Destroy(s.gameObject);
                    }
                }
                gameObject.GetComponent<NetworkObject>().Despawn();
            }
            yield return new WaitForSeconds(1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NewUnitSizeServerRpc(float newSize)
    {
        unitSize.Value += newSize;
    }

    IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.GetComponent<NetworkObject>().Despawn();
    }

}
