using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [Header("Start Resources")]
    [SerializeField] int initalGold = 1000;
    [SerializeField] int initalManpower = 1200;

    [Header("Times")]
    [SerializeField] float soldierWaitTime = 5f;
    [SerializeField] float rocketLauncherWaitTime = 4f;
    [SerializeField] float misLauncherWaitTime = 10f;
    [SerializeField] float bigMisLauncherWaitTime = 20f;
    [SerializeField] float rocketWaitTime = 3.5f;
    [SerializeField] float sMisWaitTime = 6f;
    [SerializeField] float mMisWaitTime = 8f;
    [SerializeField] float bMisWaitTime = 18f;

    [Header("Costs")]
    [SerializeField] int soldierManCost = 1000;
    [SerializeField] int soldierGoldCost = 600;
    [SerializeField] int rocketLauncherCost = 200;
    [SerializeField] int misLauncherCost = 650;
    [SerializeField] int bigMisLauncherCost = 1200;
    [SerializeField] int rocketCost = 250;
    [SerializeField] int sMisCost = 650;
    [SerializeField] int mMisCost = 1150;
    [SerializeField] int bMisCost = 2000;

    [Header("Production")]
    [SerializeField] int goldProductionIncreasePerFourSeconds = 1;
    [SerializeField] int manpowerProduction = 50;

    Text countdownText;
    GameObject canvasToClose;
    GameObject canvasToOpen;


    NetworkVariable<bool> countdownStarted = new NetworkVariable<bool>(false);
    NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);
    NetworkVariable<bool> endScreenOpened = new NetworkVariable<bool>(false);
    NetworkVariable<int> winner = new NetworkVariable<int>();



    bool gameStartLocal = false;

    NetworkVariable<int> manpower = new NetworkVariable<int>(0);
    NetworkVariable<int> gold = new NetworkVariable<int>(0);
    NetworkVariable<int> res0 = new NetworkVariable<int>(0);
    NetworkVariable<int> res1 = new NetworkVariable<int>(0);
    NetworkVariable<int> res2 = new NetworkVariable<int>(0);
    NetworkVariable<int> res3 = new NetworkVariable<int>(0);
    NetworkVariable<int> goldProduction = new NetworkVariable<int>(40);
    NetworkVariable<bool> barracks0Disable = new NetworkVariable<bool>(false);
    NetworkVariable<bool> barracks1Disable = new NetworkVariable<bool>(false);
    NetworkVariable<bool> barracks2Disable = new NetworkVariable<bool>(false);


    public int Manpower { get { return manpower.Value; } }
    public int Gold { get { return gold.Value; } }
    public int GoldProduction { get { return goldProduction.Value; } }
    public int Res0 { get { return res0.Value; } }
    public int Res1 { get { return res1.Value; } }
    public int Res2 { get { return res2.Value; } }
    public int Res3 { get { return res3.Value; } }



    GameManager gameManager;
    GameObject selected;

    // Launcher and Range Control
    List<GameObject> firingLauncher=new List<GameObject>();
    GameObject possibleFiringLauncher;
    int rangeMenuID = 0;
    float[] ranges = new float[4] { 23f, 55f, 82f, 114f };
    float[] minRanges= new float[4] { 0f, 12f, 25f, 40f };
    float[] resetTime = new float[4] { 2f, 2.5f, 3.5f, 7f };
    bool rangeMenuOpened = false;
    SpriteRenderer rangeMenuArea = null;
    SpriteRenderer rangeMenuClick = null;
    Plane rangeMousePlane = new Plane(new Vector3(0, 1, 0), new Vector3(0, 0.5f, 0));

    // Barracks
    bool[] barracksClickable = new bool[3] { true,true,true };
    bool[] barracksBuilding = new bool[3] { false, false, false };


    Dictionary<int, IEnumerator> soldierCoroutines = new Dictionary<int, IEnumerator>();

    // Factory
    bool[] factoryClickable = new bool[2] { true, true};
    bool[] factoryBuilding = new bool[2] { false, false};
    Dictionary<int, IEnumerator> factoryCoroutines = new Dictionary<int, IEnumerator>();
    int factory0tempCost = 0;
    int factory1tempCost = 0;

    // BuildingTile
    bool[] tileClickable = new bool[12] { true, true, true, true, true, true, true, true, true, true, true, true };
    bool[] tileBuilding= new bool[12] { false, false, false, false, false, false, false, false, false, false, false, false };
    Dictionary<int, IEnumerator> tileCoroutines = new Dictionary<int, IEnumerator>();

    Text player1;



    private void Start()
    {
        if (IsOwner)
        {
            canvasToClose = GameObject.FindWithTag("Login");
            canvasToOpen= GameObject.FindWithTag("End");
            countdownText = GameObject.FindWithTag("Countdown").GetComponent<Text>();
        }
    }
    public override void OnNetworkSpawn()
    {
        gameManager = FindObjectOfType<GameManager>();
        gameManager.gameStarted += GameManager_gameStarted;
        gameManager.gameFinished += GameManager_gameFinished;
        if (IsOwner)
        {
            GoldIncreaseServerRpc(initalGold);
            ManpowerIncreaseServerRpc(initalManpower);
        }
    }

    private void GameManager_gameFinished(object sender, GameManager.KillListEventArgs e)
    {
        WinnerIDServerRpc(e.winnerID);
        GameEndedServerRpc();
        Invoke("endScreenOpenServerRpc",0.3f);
    }

    private void GameManager_gameStarted(object sender, System.EventArgs e)
    {
        CountdownStartedServerRpc();
        Invoke("GameStartedServerRpc", 3.3f);
    }

    IEnumerator Countdown()
    {
        int i = 3;
        while (i > 0)
        {
            countdownText.text=i.ToString();
            i--;
            yield return new WaitForSeconds(1);
        }
        countdownText.text = "GO";
        yield return new WaitForSeconds(0.3f);
        canvasToClose.SetActive(false);
    }


    [ServerRpc(RequireOwnership = false)]
    private void CountdownStartedServerRpc()
    {
        countdownStarted.Value = true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void CountdownEndedServerRpc()
    {
        countdownStarted.Value = false;
    }


    [ServerRpc(RequireOwnership = false)]
    private void GameStartedServerRpc()
    {
        gameStarted.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void GameEndedServerRpc()
    {
        gameStarted.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void endScreenOpenServerRpc()
    {
        endScreenOpened.Value = true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void endScreenCloseServerRpc()
    {
        endScreenOpened.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void WinnerIDServerRpc(int i)
    {
        winner.Value = i;
    }


    IEnumerator Production()
    {
        int x = 0;
        while(true)
        {
            yield return new WaitForSeconds(1f);
            GoldIncreaseServerRpc(goldProduction.Value);
            x++;
            if (x % 4 == 0&&goldProduction.Value<40)
            {
                GoldProductionIncreaseServerRpc(goldProductionIncreasePerFourSeconds);
            }
            ManpowerIncreaseServerRpc(manpowerProduction);
        }
    }



    private void Update()
    {
        if(countdownStarted.Value&&IsOwner)
        {
            StartCoroutine(Countdown());
            CountdownEndedServerRpc();
        }

        if(endScreenOpened.Value)
        {
            if(IsOwner)
            {
                canvasToOpen.transform.GetChild(0).gameObject.SetActive(true);
                GameObject text1 = canvasToOpen.transform.GetChild(1).gameObject;
                text1.SetActive(true);
                GameObject text0 = canvasToOpen.transform.GetChild(2).gameObject;
                text0.SetActive(true);

                if (winner.Value == 0)
                {
                    text1.GetComponent<Text>().text = "PLAYER 2 LOST";
                    text1.GetComponent<Text>().fontSize = 80;
                    text0.GetComponent<Text>().text = "PLAYER 1 WON";
                    text0.GetComponent<Text>().color = new Color(1, 0.6324f, 0, 1);
                }
                else if(winner.Value == 1)
                {
                    text1.GetComponent<Text>().text = "PLAYER 2 WON";
                    text0.GetComponent<Text>().fontSize = 80;
                    text0.GetComponent<Text>().text = "PLAYER 1 LOST";
                    text1.GetComponent<Text>().color = new Color(1, 0.6324f, 0, 1);
                }
                endScreenCloseServerRpc();
            }
        }

        if (!gameStarted.Value)
        {

            return;
        }


        if (IsOwner && IsClient)
        {
            if (!gameStartLocal)
            {
                StartCoroutine(Production());
                gameStartLocal= true;
            }

            if (Input.GetMouseButtonDown(0)&&!rangeMenuOpened)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                //Check if we hit something
                if (Physics.Raycast(ray, out hit))
                {

                    if (hit.transform.TryGetComponent<RocketLauncher>(out RocketLauncher r))
                    {
                        if (firingLauncher.Contains(r.gameObject))
                        {
                            return;
                        }

                        //Check if it's players barrack
                        if (r.PlayerID == (int)OwnerClientId)
                        {
                            //Close last opened menu if it exists
                            if (selected != null)
                                CancelSelected(selected);

                            //New opened menu
                            selected = hit.transform.gameObject;

                            //Get butMenu and open it
                            var butMenu = r.gameObject.transform.GetChild(2).gameObject;
                            butMenu.SetActive(true);


                            if(butMenu.transform.childCount == 1)
                            {
                                Button button = butMenu.GetComponentInChildren<Button>();
                                int i = button.gameObject.GetComponent<ButID>().ButNo;
                                int check = 0;
                                if (i == 0)
                                {
                                    check = Res0;
                                }
                                else if (i == 3)
                                {
                                    check = Res3;
                                }

                                button.onClick.AddListener(() =>
                                {
                                    if (check > 0)
                                    {
                                        OpenRocketRange(0, r.transform.position, OwnerClientId, r.gameObject, butMenu, button);
                                        selected = null;
                                    }
                                });
                            }
                            else if(butMenu.transform.childCount == 2)
                            {
                                Button button0= butMenu.transform.GetChild(0).gameObject.GetComponentInChildren<Button>();
                                Button button1 = butMenu.transform.GetChild(1).gameObject.GetComponentInChildren<Button>();

                                button0.onClick.AddListener(() =>
                                {
                                    if (Res1 > 0)
                                    {
                                        OpenRocketRange(0, r.transform.position, OwnerClientId, r.gameObject, butMenu, button0);
                                        selected = null;
                                    }
                                });

                                button1.onClick.AddListener(() =>
                                {
                                    if(Res2 > 0)
                                    {
                                        OpenRocketRange(1, r.transform.position, OwnerClientId, r.gameObject, butMenu, button1);
                                        selected = null;
                                    }
                                });


                            }

                        }
                    }

                    //Check if it's barrack if so we'll open butMenu
                    else if (hit.transform.TryGetComponent<Barracks>(out Barracks c))
                    {
                        if (c.BarracksNo == 0 && barracks0Disable.Value)
                            return;
                        else if (c.BarracksNo == 1 && barracks1Disable.Value)
                            return;
                        else if (c.BarracksNo == 2 && barracks2Disable.Value)
                            return;

                        //Check if it's players barrack
                        if (c.PlayerID==(int)OwnerClientId)
                        {
                            //Close last opened menu if it exists
                            if (selected != null)
                                CancelSelected(selected);

                            //New opened menu
                            selected = hit.transform.gameObject;

                            //Check if clicked before
                            if (barracksClickable[c.BarracksNo])
                            {
                                //Check if this barrack building
                                if (!barracksBuilding[c.BarracksNo])
                                {
                                    //Get butMenu and open it
                                    var butMenu = c.gameObject.transform.GetChild(2).gameObject;
                                    butMenu.SetActive(true);

                                    //Get button opened and add listener
                                    Button button = butMenu.GetComponentInChildren<Button>();
                                    button.onClick.AddListener(() => {
                                        if (Manpower >= soldierManCost && Gold >= soldierGoldCost)
                                        {
                                            GoldDecreaseServerRpc(soldierGoldCost);
                                            ManpowerDecreaseServerRpc(soldierManCost);
                                            IEnumerator cor = SoldierBuilding(c.transform.position, OwnerClientId, c.gameObject, butMenu, button, c.BarracksNo);
                                            soldierCoroutines.Add(c.BarracksNo, cor);
                                            StartCoroutine(cor);
                                        }
                                    });

                                    //Make it clicked
                                    barracksClickable[c.BarracksNo] = false;
                                }
                            }
                            else
                            {
                                StopCoroutine(soldierCoroutines[c.BarracksNo]);
                                soldierCoroutines.Remove(c.BarracksNo);
                                GameObject buildingMenu = c.transform.gameObject.transform.GetChild(3).gameObject;
                                buildingMenu.SetActive(false);
                                barracksClickable[c.BarracksNo] = true;
                                barracksBuilding[c.BarracksNo] = false;
                                GoldIncreaseServerRpc(soldierGoldCost);
                                ManpowerIncreaseServerRpc(soldierManCost);
                            }

                        }

                    }

                    //Check if it's tile if so we'll open butMenu
                    else if (hit.transform.TryGetComponent<BuildingTile>(out BuildingTile b))
                    {
                        //Check if it's tile
                        if (b.PlayerID == (int)OwnerClientId)
                        {
                            //Close last opened menu if it exists !!!    && tileAvaliable[b.LauncherNo]
                            if (selected != null)
                                CancelSelected(selected);

                            //New opened menu
                            selected = hit.transform.gameObject;

                            //Check if clicked before
                            if (tileClickable[b.LauncherNo])
                            {
                                //Check if this tile building
                                if (!tileBuilding[b.LauncherNo])
                                {
                                    //Get butMenu and open it
                                    var butMenu = b.gameObject.transform.GetChild(2).gameObject;
                                    butMenu.SetActive(true);

                                    //Get button opened and add listener
                                    Button button = butMenu.GetComponentInChildren<Button>();

                                    if (b.LauncherNo <= 5)
                                    {
                                        button.onClick.AddListener(() => 
                                        {
                                            if (Gold >= rocketLauncherCost)
                                            {
                                                GoldDecreaseServerRpc(rocketLauncherCost);
                                                IEnumerator cor = LauncherBuilding(b.transform.position, OwnerClientId, b.gameObject, butMenu, button, b.LauncherNo, rocketLauncherWaitTime);
                                                tileCoroutines.Add(b.LauncherNo, cor);
                                                StartCoroutine(cor);
                                            }
                                        });
                                    }
                                    else if (b.LauncherNo > 5 && b.LauncherNo <= 9)
                                    {
                                        button.onClick.AddListener(() =>
                                        {
                                            if (Gold >= misLauncherCost)
                                            {
                                                GoldDecreaseServerRpc(misLauncherCost);
                                                IEnumerator cor = LauncherBuilding(b.transform.position, OwnerClientId, b.gameObject, butMenu, button, b.LauncherNo, misLauncherWaitTime);
                                                tileCoroutines.Add(b.LauncherNo, cor);
                                                StartCoroutine(cor);
                                            }
                                        });
                                    }
                                    else if (b.LauncherNo >= 10)
                                    {
                                        button.onClick.AddListener(() =>
                                        {
                                            if (Gold >= bigMisLauncherCost)
                                            {
                                                GoldDecreaseServerRpc(bigMisLauncherCost);
                                                IEnumerator cor = LauncherBuilding(b.transform.position, OwnerClientId, b.gameObject, butMenu, button, b.LauncherNo, bigMisLauncherWaitTime);
                                                tileCoroutines.Add(b.LauncherNo, cor);
                                                StartCoroutine(cor);
                                            }
                                        });
                                    }

                                    //Make it clicked
                                    tileClickable[b.LauncherNo] = false;
                                }
                            }
                            else 
                            {
                                StopCoroutine(tileCoroutines[b.LauncherNo]);
                                tileCoroutines.Remove(b.LauncherNo);
                                GameObject buildingMenu = b.transform.gameObject.transform.GetChild(3).gameObject;
                                buildingMenu.SetActive(false);
                                tileClickable[b.LauncherNo] = true;
                                tileBuilding[b.LauncherNo] = false;

                                if (b.LauncherNo <= 5)
                                {
                                    GoldIncreaseServerRpc(rocketLauncherCost);
                                }
                                else if (b.LauncherNo > 5 && b.LauncherNo <= 9)
                                {
                                    GoldIncreaseServerRpc(misLauncherCost);
                                }
                                else if (b.LauncherNo >= 10)
                                {
                                    GoldIncreaseServerRpc(bigMisLauncherCost);
                                }

                            }
                        }
                    }

                    else if (hit.transform.TryGetComponent<Factory>(out Factory f))
                    {
                        //Check if it's players factory
                        if (f.PlayerID == (int)OwnerClientId)
                        {
                            //Close last opened menu if it exists
                            if (selected != null)
                                CancelSelected(selected);

                            //New opened menu
                            selected = hit.transform.gameObject;

                            //Check if clicked before
                            if (!factoryBuilding[f.FactoryNo])
                            {
                                //Check if this factory building
                                if (factoryClickable[f.FactoryNo])
                                {
                                    //Get butMenu and open it
                                    var butMenu = f.gameObject.transform.GetChild(2).gameObject;
                                    butMenu.SetActive(true);

                                    //Get button opened and add listener

                                    Button button0 = butMenu.transform.GetChild(0).gameObject.GetComponent<Button>();
                                    Button button1 = butMenu.transform.GetChild(1).gameObject.GetComponent<Button>();

                                    Button[] buttons=new Button[2] {button0,button1};


                                    if(f.FactoryNo == 0)
                                    {
                                        button0.onClick.AddListener(() => {
                                            if (Gold >= rocketCost)
                                            {
                                                GoldDecreaseServerRpc(rocketCost);
                                                IEnumerator cor = FactoryBuilding(f.gameObject, butMenu, buttons, f.FactoryNo, 0);
                                                factoryCoroutines.Add(f.FactoryNo, cor);
                                                StartCoroutine(cor);
                                                if (f.FactoryNo == 0)
                                                    factory0tempCost = rocketCost;
                                                else if (f.FactoryNo == 1)
                                                    factory1tempCost = rocketCost;
                                            }
                                        });

                                        button1.onClick.AddListener(() => {
                                            if (Gold >= sMisCost)
                                            {
                                                GoldDecreaseServerRpc(sMisCost);
                                                IEnumerator cor = FactoryBuilding(f.gameObject, butMenu, buttons, f.FactoryNo, 1);
                                                factoryCoroutines.Add(f.FactoryNo, cor);
                                                StartCoroutine(cor);
                                                if (f.FactoryNo == 0)
                                                    factory0tempCost = sMisCost;
                                                else if (f.FactoryNo == 1)
                                                    factory1tempCost = sMisCost;
                                            }
                                        });
                                    }

                                    else if (f.FactoryNo == 1)
                                    {
                                        button0.onClick.AddListener(() => {
                                            if (Gold >= mMisCost)
                                            {
                                                GoldDecreaseServerRpc(mMisCost);
                                                IEnumerator cor = FactoryBuilding(f.gameObject, butMenu, buttons, f.FactoryNo, 2);
                                                factoryCoroutines.Add(f.FactoryNo, cor);
                                                StartCoroutine(cor);
                                                if (f.FactoryNo == 0)
                                                    factory0tempCost = mMisCost;
                                                else if (f.FactoryNo == 1)
                                                    factory1tempCost = mMisCost;
                                            }
                                        });

                                        button1.onClick.AddListener(() => {
                                            if (Gold >= bMisCost)
                                            {
                                                GoldDecreaseServerRpc(bMisCost);
                                                IEnumerator cor = FactoryBuilding(f.gameObject, butMenu, buttons, f.FactoryNo, 3);
                                                factoryCoroutines.Add(f.FactoryNo, cor);
                                                StartCoroutine(cor);
                                                if (f.FactoryNo == 0)
                                                    factory0tempCost = bMisCost;
                                                else if (f.FactoryNo == 1)
                                                    factory1tempCost = bMisCost;
                                            }
                                        });
                                    }

                                    //Make it clicked
                                    factoryClickable[f.FactoryNo] = false;
                                    
                                }
                            }
                            else
                            {
                                StopCoroutine(factoryCoroutines[f.FactoryNo]);
                                factoryCoroutines.Remove(f.FactoryNo);
                                GameObject buildingMenus = f.transform.GetChild(3).gameObject;
                                foreach(Transform g in buildingMenus.transform)
                                {
                                    g.gameObject.SetActive(false);
                                }
                                buildingMenus.SetActive(false);

                                factoryClickable[f.FactoryNo] = true;
                                factoryBuilding[f.FactoryNo] = false;
                                
                                if(f.FactoryNo==0)
                                    GoldIncreaseServerRpc(factory0tempCost);
                                else if (f.FactoryNo == 1)
                                    GoldIncreaseServerRpc(factory1tempCost);
                            }
                        }
                    }
                }
            }

            if(Input.GetMouseButtonDown(1)&&!rangeMenuOpened)
            {
                if (selected != null)
                    CancelSelected(selected);
            }

            
            if(rangeMenuOpened&&rangeMenuArea!=null&&rangeMenuClick!=null)
            {                
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if(rangeMousePlane.Raycast(ray,out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    rangeMenuClick.transform.position = hitPoint;
                    /*
                    if (Vector3.Distance(rangeMenuArea.transform.position, rangeMenuClick.transform.position) < ranges[rangeMenuID])
                    {
                        rangeMenuArea.color = new Color(0,1,0,0.5f);
                    }
                    else
                    {
                        rangeMenuArea.color = new Color(1, 0, 0, 0.5f);
                    }*/
                }
            }

            if(rangeMenuOpened&& Input.GetMouseButtonDown(0) && rangeMenuArea != null && rangeMenuClick != null)
            {
                rangeMenuOpened = false;
                if (Vector3.Distance(rangeMenuArea.transform.position, rangeMenuClick.transform.position) <= ranges[rangeMenuID]&& Vector3.Distance(rangeMenuArea.transform.position, rangeMenuClick.transform.position) >= minRanges[rangeMenuID])
                {
                    RocketSendRequestServerRPC(rangeMenuID,rangeMenuArea.transform.position, rangeMenuClick.transform.position, OwnerClientId);
                    firingLauncher.Add(possibleFiringLauncher);

                    if(possibleFiringLauncher != null)
                    {
                        StartCoroutine(FiringOver(resetTime[rangeMenuID], possibleFiringLauncher));
                    }
                }
                rangeMenuArea.transform.parent.gameObject.SetActive(false);
                possibleFiringLauncher = null;
            }

            if (rangeMenuOpened && rangeMenuArea == null)
            {
                rangeMenuOpened = false;
            }

        }
    }

    void CancelSelected(GameObject g)
    {
        // get butMenu to close and get but to remove listeners,
        GameObject butMenu = g.gameObject.transform.GetChild(2).gameObject;

        if (butMenu.transform.childCount == 1)
        {
            Button button = butMenu.GetComponentInChildren<Button>();
            button.onClick.RemoveAllListeners();
        }
        else if (butMenu.transform.childCount == 2)
        {
            Button button0 = butMenu.transform.GetChild(0).gameObject.GetComponentInChildren<Button>();
            Button button1 = butMenu.transform.GetChild(1).gameObject.GetComponentInChildren<Button>();
            button0.onClick.RemoveAllListeners();
            button1.onClick.RemoveAllListeners();
        }

        butMenu.SetActive(false);

        // set clickable again
        if(g.transform.TryGetComponent<Barracks>(out Barracks c))
            barracksClickable[g.GetComponent<Barracks>().BarracksNo] = true;
        else if(g.transform.TryGetComponent<BuildingTile>(out BuildingTile b))
            tileClickable[b.LauncherNo] = true;
        else if(g.transform.TryGetComponent<Factory>(out Factory f))
            factoryClickable[f.FactoryNo] = true;
    }

    IEnumerator SoldierBuilding(Vector3 i, ulong senderID, GameObject barrack ,GameObject butMenu, Button but, int clicked)
    {
        //Last opened menu will close here so we can set selected null
        selected = null;
        //Building true
        barracksBuilding[clicked] = true;
        //We clicked the button so we remove listeners
        but.onClick.RemoveAllListeners();

        //Get buildingMenu opened
        GameObject buildingMenu=barrack.transform.GetChild(3).gameObject;
        buildingMenu.SetActive(true);

        //Close butMenu
        butMenu.SetActive(false);

        // buildingMenu animation
        float stepNum = soldierWaitTime*2;
        int j = 0;
        buildingMenu.GetComponentInChildren<Slider>().value = 5f;
        while (j < stepNum)
        {
            yield return new WaitForSeconds(0.5f);
            buildingMenu.GetComponentInChildren<Slider>().value -= 5f/stepNum;
            j++;
        }
        buildingMenu.SetActive(false);
        // buildingMenu closed

        SoldierRequestServerRPC(i, senderID);

        // clickable true, building false
        barracksClickable[clicked] = true;
        barracksBuilding[clicked] = false;
        soldierCoroutines.Remove(clicked);
    }

    IEnumerator FactoryBuilding(GameObject factory, GameObject butMenu, Button[] buttons, int clicked, int resNo)
    {
        //Last opened menu will close here so we can set selected null
        selected = null;
        //Building true
        factoryBuilding[clicked] = true;
        //We clicked the button so we remove listeners
        foreach(Button button in buttons)
        {
            button.onClick.RemoveAllListeners();
        }

        int childNo = 0;
        float stepNum = 0;
        if (resNo == 0)
        {
            stepNum = rocketWaitTime * 2;
            childNo = 0;
        }
        if (resNo == 1)
        {
            stepNum = sMisWaitTime * 2;
            childNo = 1;
        }
        if (resNo == 2)
        {
            stepNum = mMisWaitTime * 2;
            childNo = 0;
        }
        if (resNo == 3)
        {
            stepNum = bMisWaitTime * 2;
            childNo = 1;
        }

        //Get buildingMenu opened
        GameObject buildingMenus = factory.transform.GetChild(3).gameObject;
        buildingMenus.SetActive(true);
        GameObject buildingMenu=buildingMenus.transform.GetChild(childNo).gameObject;
        buildingMenu.SetActive(true);
        butMenu.SetActive(false);

        int j = 0;
        buildingMenu.GetComponentInChildren<Slider>().value = 5f;
        while (j < stepNum)
        {
            yield return new WaitForSeconds(0.5f);
            buildingMenu.GetComponentInChildren<Slider>().value -= 5f / stepNum;
            j++;
        }

        buildingMenu.SetActive(false);


        ResourceIncreaseServerRpc(resNo);

        // clickable true, building false
        factoryClickable[clicked] = true;
        factoryBuilding[clicked] = false;
        factoryCoroutines.Remove(clicked);
    }

    IEnumerator LauncherBuilding(Vector3 i, ulong senderID, GameObject rocketLauncher, GameObject butMenu, Button but, int clicked,float buildTime)
    {
        //Last opened menu will close here so we can set selected null
        selected = null;
        //Building true
        tileBuilding[clicked] = true;
        //We clicked the button so we remove listeners
        but.onClick.RemoveAllListeners();

        //Get buildingMenu opened
        GameObject buildingMenu = rocketLauncher.transform.GetChild(3).gameObject;
        buildingMenu.SetActive(true);

        //Close butMenu
        butMenu.SetActive(false);

        // buildingMenu animation
        float stepNum = 8;
        int j = 0;
        buildingMenu.GetComponentInChildren<Slider>().value = 5f;
        while (j < stepNum)
        {
            yield return new WaitForSeconds(buildTime/stepNum);
            buildingMenu.GetComponentInChildren<Slider>().value -= 5f/stepNum;
            j++;
        }
        buildingMenu.SetActive(false);
        // buildingMenu closed


        if (clicked <= 5)
        {
            RocketLauncherRequestServerRPC(i, senderID);
        }
        else if (clicked > 5 && clicked <= 9)
        {
            MissileLauncherRequestServerRPC(i, senderID);
        }
        else if (clicked >= 10)
        {
            BigMissileLauncherRequestServerRPC(i, senderID);
        }
        // clickable true, building false
        tileClickable[clicked] = true;
        tileBuilding[clicked] = false;
        tileCoroutines.Remove(clicked);
    }

    void OpenRocketRange(int childID,Vector3 i, ulong senderID, GameObject rocketLauncher, GameObject butMenu,Button but)
    {
        
        but.onClick.RemoveAllListeners();
        possibleFiringLauncher = rocketLauncher;
        GameObject rangeMenus= rocketLauncher.transform.GetChild(3).gameObject;
        rangeMenus.SetActive(true);
        GameObject rangeMenu= rangeMenus.transform.GetChild(childID).gameObject;
        rangeMenu.SetActive(true);
        
        rangeMenuID = rangeMenu.GetComponent<RangeMenuID>().RangeMenuNo;

        rangeMenuArea = rangeMenu.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        rangeMenuClick = rangeMenu.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();

        rangeMenu.SetActive(true);

        butMenu.SetActive(false);

        rangeMenuOpened = true;
    }



    [ServerRpc(RequireOwnership = false)]
    private void SoldierRequestServerRPC(Vector3 i, ulong senderID)
    {
        gameManager.BuildSoldier(i, senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RocketLauncherRequestServerRPC(Vector3 i, ulong senderID)
    {
        gameManager.BuildRocketLaucnher(i, senderID);
    }
    [ServerRpc(RequireOwnership = false)]
    private void MissileLauncherRequestServerRPC(Vector3 i, ulong senderID)
    {
        gameManager.BuildMissileLaucnher(i, senderID);
    }
    [ServerRpc(RequireOwnership = false)]
    private void BigMissileLauncherRequestServerRPC(Vector3 i, ulong senderID)
    {
        gameManager.BuildBigMissileLaucnher(i, senderID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RocketSendRequestServerRPC(int rocketID,Vector3 startPos,Vector3 endPos, ulong senderID)
    {
        if (rocketID == 0)
        {
            gameManager.RocketSend(startPos, endPos, senderID);
            res0.Value--;
        }
        else if (rocketID == 1)
        {
            gameManager.sMisSend(startPos, endPos, senderID);
            res1.Value--;
        }
        else if (rocketID == 2)
        {
            gameManager.mMisSend(startPos, endPos, senderID);
            res2.Value--;
        }
        else if (rocketID == 3)
        {
            gameManager.lMisSend(startPos, endPos, senderID);
            res3.Value--;
        }
    }

    IEnumerator FiringOver(float waitTime, GameObject g)
    {
        yield return new WaitForSeconds(waitTime);
        firingLauncher.Remove(g);
    }


    [ServerRpc(RequireOwnership = false)]
    private void ManpowerIncreaseServerRpc(int manpowerAmount)
    {
        manpower.Value += manpowerAmount;
    }
    [ServerRpc(RequireOwnership = false)]
    private void ManpowerDecreaseServerRpc(int manpowerAmount)
    {
        manpower.Value -= manpowerAmount;
    }

    [ServerRpc(RequireOwnership = false)]
    private void GoldIncreaseServerRpc(int goldAmount)
    {
        gold.Value += goldAmount;
    }

    [ServerRpc(RequireOwnership = false)]
    private void GoldDecreaseServerRpc(int goldAmount)
    {
        gold.Value -= goldAmount;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResourceIncreaseServerRpc(int resNo)
    {
        if (resNo == 0)
        {
            res0.Value++;
        }
        if (resNo == 1)
        {
            res1.Value++;
        }
        if (resNo == 2)
        {
            res2.Value++;
        }
        if (resNo == 3)
        {
            res3.Value++;
        }
    }

    public void ManpowerIncreaseRequest(int i)
    {
         ManpowerIncreaseServerRpc(i);
    }

    public void GoldIncreaseRequest(int i)
    {
        GoldIncreaseServerRpc(i);
    }

    [ServerRpc(RequireOwnership = false)]
    private void BarracksDisableServerRpc(int barracksNo)
    {
        if (barracksNo == 0)
        {
            barracks0Disable.Value = true ;
        }
        if (barracksNo == 1)
        {
            barracks1Disable.Value = true;
        }
        if (barracksNo == 2)
        {
            barracks2Disable.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GoldProductionIncreaseServerRpc(int amount)
    {
        goldProduction.Value += amount;
    }

    public void BarracksDisableRequest(int i)
    {
        BarracksDisableServerRpc(i);
    }

}
