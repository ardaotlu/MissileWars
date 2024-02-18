using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] float delayTime = 3f;
    NetworkVariable<int> playersInGame = new NetworkVariable<int>(0);
    public event EventHandler gameStarted;
    public event EventHandler<KillListEventArgs> gameFinished;

    private bool gameStartInitiated = false;
    private int player0BarrackCount = 3;
    private int player1BarrackCount = 3;

    [SerializeField] GameObject[] soldierPrefab;

    [SerializeField] GameObject[] rocketLauncherPrefabs;
    [SerializeField] GameObject[] missileLauncherPrefabs;
    [SerializeField] GameObject[] bigMissileLauncherPrefabs;

    [SerializeField] GameObject[] rocketPrefabs;
    [SerializeField] GameObject[] smallMisPrefabs;
    [SerializeField] GameObject[] mediumMisPrefabs;
    [SerializeField] GameObject[] largeMisPrefabs;

    [SerializeField] AnimationCurve riseCurve;
    [SerializeField] AnimationCurve fallCurve;

    public class KillListEventArgs : EventArgs
    {
        public int winnerID;
    }



    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += ((id) =>
        {
            if (IsServer)
            {
                playersInGame.Value++;
            }

        });

        NetworkManager.Singleton.OnClientDisconnectCallback += ((id) =>
        {
            if (IsServer)
            {
                playersInGame.Value--;
            }
        });

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Barracks[] barracks=FindObjectsOfType<Barracks>();
            foreach(Barracks barrack in barracks)
            {
                barrack.player0BarracksDestroyed += Barrack_player0BarracksDestroyed;
                barrack.player1BarracksDestroyed += Barrack_player1BarracksDestroyed;
            }
        }
    }

    private void Barrack_player1BarracksDestroyed(object sender, EventArgs e)
    {
        player1BarrackCount--;
        if(player1BarrackCount == 0)
        {
            gameFinished(this, new KillListEventArgs { winnerID = 0 });
        }
    }

    private void Barrack_player0BarracksDestroyed(object sender, EventArgs e)
    {
        player0BarrackCount--;
        if (player0BarrackCount == 0)
        {
            gameFinished(this, new KillListEventArgs { winnerID = 1 });
        }
    }

    private void Update()
    {
        if(IsServer&&!gameStartInitiated&&playersInGame.Value==2)
        {
            gameStartInitiated = true;
            StartGame();
        }
    }


    public void StartGame()
    {
        gameStarted(this, EventArgs.Empty);
        Debug.Log("Started");
    }

    // Building soldier, buildings, sending rockets
    public void BuildSoldier(Vector3 i, ulong senderID)
    {
        if (senderID == 0)
        {
            GameObject go = Instantiate(soldierPrefab[(int)senderID], i + new Vector3(0, 0, 7f), Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
        }
        else if (senderID == 1)
        {
            GameObject go = Instantiate(soldierPrefab[(int)senderID], i + new Vector3(0, 0, -5f), Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void BuildRocketLaucnher(Vector3 i, ulong senderID)
    {
        GameObject go = Instantiate(rocketLauncherPrefabs[(int)senderID], i, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
    public void BuildMissileLaucnher(Vector3 i, ulong senderID)
    {
        GameObject go = Instantiate(missileLauncherPrefabs[(int)senderID], i, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
    public void BuildBigMissileLaucnher(Vector3 i, ulong senderID)
    {
        GameObject go = Instantiate(bigMissileLauncherPrefabs[(int)senderID], i, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }

    public void RocketSend(Vector3 startPos, Vector3 endPos, ulong senderID)
    {
        GameObject go = Instantiate(rocketPrefabs[(int)senderID], startPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(RocketMove(go,startPos,endPos));
    }
    public void sMisSend(Vector3 startPos, Vector3 endPos, ulong senderID)
    {
        GameObject go = Instantiate(smallMisPrefabs[(int)senderID], startPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(RocketMove(go, startPos, endPos));
    }

    public void mMisSend(Vector3 startPos, Vector3 endPos, ulong senderID)
    {
        GameObject go = Instantiate(mediumMisPrefabs[(int)senderID], startPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(RocketMove(go, startPos, endPos));
    }

    public void lMisSend(Vector3 startPos, Vector3 endPos, ulong senderID)
    {
        GameObject go = Instantiate(largeMisPrefabs[(int)senderID], startPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(RocketMove(go, startPos, endPos));
    }


    IEnumerator RocketMove(GameObject rocket, Vector3 startPos, Vector3 endPos)
    {
        RocketEffectsClientRpc(rocket, 1);
        RocketClass rocketProp = rocket.GetComponent<RocketClass>();

        float timeMultiplier = Vector3.Distance(startPos, endPos) / rocketProp.Range;
        var timeStart = Time.fixedTime;

        // MOVE
        Vector3 midPos = (startPos + endPos) / 2;
        midPos -= new Vector3(0, Vector3.Distance(startPos, endPos) / 2, 0);
        // Interpolate over the arc relative to center
        Vector3 riseRelCenter = startPos - midPos;
        Vector3 setRelCenter = endPos - midPos;

        //ROTATE
        rocket.transform.LookAt(endPos);
        Quaternion from = rocket.transform.rotation;
        from.eulerAngles+= new Vector3(310, 0, 0);

        Quaternion mid = rocket.transform.rotation;

        Quaternion to = rocket.transform.rotation;
        to.eulerAngles += new Vector3(50, 0, 0);


        
        float fracComplete = 0f;
        while (fracComplete < 1f)
        {

            fracComplete += Time.deltaTime/(rocketProp.Time*timeMultiplier);
            rocket.transform.position = Vector3.Slerp(riseRelCenter, setRelCenter, fracComplete);
            rocket.transform.position += midPos;

            //ROTATE
            if (fracComplete < 0.5f)
            {
                rocket.transform.rotation = Quaternion.Slerp(from,mid,fracComplete*2);
            }
            else
            {
                rocket.transform.rotation = Quaternion.Slerp(mid, to, (fracComplete-0.5f)*2);
            }


            yield return new WaitForEndOfFrame();
        }

        var timeEnd = Time.fixedTime;
        Debug.Log("Time of flight: " + (timeEnd - timeStart));

        RocketEffectsClientRpc(rocket, 2);
        rocket.transform.GetChild(0).gameObject.SetActive(false);
        Destroy(rocket, 0.8f);


    }


    [ClientRpc]
    void RocketEffectsClientRpc(NetworkObjectReference playerGameObject,int no)
    {

        if (!playerGameObject.TryGet(out NetworkObject networkObject))
        {
            Debug.Log("error");
        }

        networkObject.transform.GetChild(no).gameObject.GetComponent<ParticleSystem>().Play();
    }


}
