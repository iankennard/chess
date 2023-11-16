using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.IO;

public enum FirstStart { TRUE = 1, FALSE = 2 }

public class MainMenu : MonoBehaviour
{

    public FirstStart firstStart;

    public MenuState menuState;

    public int screenWidth;
    public int screenHeight;
    public bool isFullScreen;
    public int res;
    public bool sound;
    public bool music;
    public float soundVol;
    public string soundVolString;
    public float musicVol;
    public string musicVolString;

    public static int resDefault = 5;
    public static float soundVolDefault = 1;
    public static float musicVolDefault = .25f;

    public GUIStyle textStyleMessageCaption;
    public GUIStyle textStyleMessage;
    public GUIStyle textStyleMessageLeftAlign;
    public GUIStyle textStyleMessageRightAlign;
    public GUIStyle textStyleMoveList;
    public GUIStyle textStyleButtons;
    public GUIStyle textBoxStyle;
    public GUIStyle checkBoxStyle;

    public bool GUIInitialization;
    public bool GUIErrorMsg;

    public bool singlePlayer;
    public PieceColor multiOwnColor;
    public NetworkType networkType;
    public NetworkStatus networkStatus;
    public string hostIP;
    public string hostPortString;
    public int hostPort;
    public int listenPort;
    public bool initializeServer;
    public bool connectionInProgress;
    public bool serverFound;
    public string serverid;
    public string token;
    public bool lan;
    public GameObject LANBroadcast;
    public bool LANAutoConnect;


    public bool cerbCool1;
    public bool cerbCool2;
    public bool cerbCool3;
    public bool cerbCool4;
    public bool cerbCool5;
    public int cerbCount;


    public void Awake()
    {
        if((int)PlayerPrefs.GetInt("FirstStart") == 0)
            firstStart = FirstStart.TRUE;

        if (firstStart == FirstStart.TRUE)
        {
            res = -1;
            soundVol = -1;
            musicVol = -1;
            string line;
            if (!File.Exists("chess.ini"))
                File.WriteAllText("chess.ini", "Resolution 5\nSoundVolume 1\nMusicVolume .25");
            using (StreamReader file = new StreamReader("chess.ini"))
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] words = line.Split(' ');
                    if (words.Length > 1)
                    {
                        int hold;
                        float holdf;
                        switch (words[0])
                        {
                            case "Resolution":
                                if (!int.TryParse(words[1], out hold) || hold < 2 || hold > 8)
                                    res = resDefault;
                                else
                                    res = hold;
                                screenWidth = res * 256;
                                screenHeight = res * 144;
                                break;
                            case "SoundVolume":
                                if (!float.TryParse(words[1], out holdf) || holdf < 0 || holdf > 1)
                                    soundVol = soundVolDefault;
                                else
                                    soundVol = holdf;
                                sound = !(soundVol == 0);
                                soundVolString = System.Convert.ToString(soundVol);
                                break;
                            case "MusicVolume":
                                if (!float.TryParse(words[1], out holdf) || holdf < 0 || holdf > 1)
                                    musicVol = musicVolDefault;
                                else
                                    musicVol = holdf;
                                music = !(musicVol == 0);
                                musicVolString = System.Convert.ToString(musicVol);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            if (res == -1)
            {
                res = resDefault;
                screenWidth = res * 256;
                screenHeight = res * 144;
            }
            if (soundVol == -1)
            {
                soundVol = soundVolDefault;
                sound = !(soundVol == 0);
                soundVolString = System.Convert.ToString(soundVol);
            }
            if (musicVol == -1)
            {
                musicVol = musicVolDefault;
                music = !(musicVol == 0);
                musicVolString = System.Convert.ToString(musicVol);
            }

            isFullScreen = false;
            Screen.SetResolution(screenWidth, screenHeight, isFullScreen);
        }
        else
        {
            // load variables first
            screenWidth = PlayerPrefs.GetInt("ScreenWidth");
            screenHeight = PlayerPrefs.GetInt("ScreenHeight");
            isFullScreen = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsFullScreen"));
            res = PlayerPrefs.GetInt("Resolution");
            sound = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsSoundOn"));
            soundVol = PlayerPrefs.GetFloat("SoundVolumeFloat");
            soundVolString = PlayerPrefs.GetString("SoundVolumeString");
            music = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsMusicOn"));
            musicVol = PlayerPrefs.GetFloat("MusicVolumeFloat");
            musicVolString = PlayerPrefs.GetString("MusicVolumeString");
        }
    }
    
    public void Start()
    {
        if (firstStart == FirstStart.TRUE)
        {
            menuState = MenuState.MAIN;

            textStyleMessageCaption = new GUIStyle();
            textStyleMessage = new GUIStyle();
            textStyleMessageLeftAlign = new GUIStyle();
            textStyleMessageRightAlign = new GUIStyle();
            textStyleMoveList = new GUIStyle();
            textStyleButtons = new GUIStyle();
            textBoxStyle = new GUIStyle();
            checkBoxStyle = new GUIStyle();

            singlePlayer = true;
            multiOwnColor = PieceColor.WHITE;
            networkType = NetworkType.NONE;
            networkStatus = NetworkStatus.NONE;
            hostIP = "";
            hostPortString = "20000";
            hostPort = 20000;
            listenPort = 20000;
            initializeServer = false;
            connectionInProgress = false;
            serverFound = false;
            System.Random rand = new System.Random();
            serverid = "";
            for (int i = 0; i < 4; i++)
            {
                int ch = rand.Next(62);
                if (ch < 10)
                    serverid += System.Convert.ToString(ch);
                if (ch >= 10 && ch < 36)
                    serverid += System.Convert.ToChar(ch + 55).ToString();
                if (ch >= 36)
                    serverid += System.Convert.ToChar(ch + 61).ToString();
            }
            token = "";
            lan = false;
            LANBroadcast = GameObject.Find("lanbroadcast");
            if (LANBroadcast == null)
                throw new System.NullReferenceException("lanbroadcast not found in MainMenu");
            LANAutoConnect = false;
        }
        else
        {
            // load variables first
            menuState = (MenuState)PlayerPrefs.GetInt("MenuState");
            singlePlayer = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsSinglePlayer"));
            multiOwnColor = (PieceColor)PlayerPrefs.GetInt("MultiplayerColor");
            networkType = (NetworkType)PlayerPrefs.GetInt("NetworkType");
            networkStatus = (NetworkStatus)PlayerPrefs.GetInt("NetworkStatus");
            hostIP = PlayerPrefs.GetString("HostIP");
            hostPort = PlayerPrefs.GetInt("HostPort");
            hostPortString = System.Convert.ToString(hostPort);
            listenPort = PlayerPrefs.GetInt("ListenPort");
            token = PlayerPrefs.GetString("HostToken");
            lan = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsLAN"));
            LANAutoConnect = System.Convert.ToBoolean(PlayerPrefs.GetInt("IsLANAutoConnect"));
        }

        firstStart = FirstStart.FALSE;
        PlayerPrefs.SetInt("FirstStart", (int)FirstStart.FALSE);
        PlayerPrefs.Save();

        GUIInitialization = true;
        GUIErrorMsg = false;


        cerbCool1 = false;
        cerbCool2 = false;
        cerbCool3 = false;
        cerbCool4 = false;
        cerbCool5 = false;
        cerbCount = 0;
    }

    public void LoadGame()
    {
        // save variables first
        PlayerPrefs.SetInt("ScreenWidth", screenWidth);
        PlayerPrefs.SetInt("ScreenHeight", screenHeight);
        PlayerPrefs.SetInt("IsFullScreen", System.Convert.ToInt32(isFullScreen));
        PlayerPrefs.SetInt("Resolution", res);
        PlayerPrefs.SetInt("IsSoundOn", System.Convert.ToInt32(sound));
        PlayerPrefs.SetFloat("SoundVolumeFloat", soundVol);
        PlayerPrefs.SetString("SoundVolumeString", soundVolString);
        PlayerPrefs.SetInt("IsMusicOn", System.Convert.ToInt32(music));
        PlayerPrefs.SetFloat("MusicVolumeFloat", musicVol);
        PlayerPrefs.SetString("MusicVolumeString", musicVolString);
        PlayerPrefs.SetInt("MenuState", (int)menuState);
        PlayerPrefs.SetInt("IsSinglePlayer", System.Convert.ToInt32(singlePlayer));
        PlayerPrefs.SetInt("MultiplayerColor", (int)multiOwnColor);
        PlayerPrefs.SetInt("NetworkType", (int)networkType);
        PlayerPrefs.SetInt("NetworkStatus", (int)networkStatus);
        PlayerPrefs.SetString("HostIP", hostIP);
        PlayerPrefs.SetInt("HostPort", hostPort);
        PlayerPrefs.SetInt("ListenPort", listenPort);
        PlayerPrefs.SetString("HostToken", token);
        PlayerPrefs.SetInt("IsLAN", System.Convert.ToInt32(lan));
        PlayerPrefs.SetInt("IsLANAutoConnect", System.Convert.ToInt32(LANAutoConnect));
        PlayerPrefs.Save();

        Application.LoadLevel("default");
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteAll();
    }

    public void OnServerInitialized()
    {
        if (lan)
        {
            networkStatus = NetworkStatus.WAITING;
            if (LANAutoConnect)
                LANBroadcast.GetComponent<LANBroadcastService>().StartAnnounceBroadCasting();
        }
        else
            MasterServer.RegisterHost("PVbUTIKPDJby3Lmr", "VJ0jlVu30Xt4friH", serverid);
    }

    public void OnMasterServerEvent(MasterServerEvent e)
    {
        if (!lan)
        {
            if (e == MasterServerEvent.RegistrationSucceeded)
            {
                networkStatus = NetworkStatus.WAITING;
                MasterServer.ClearHostList();
                MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
            }
            if (e == MasterServerEvent.HostListReceived)
            {
                /*
                if (networkType == NetworkType.SERVER)
                {
                    if (MasterServer.PollHostList().Length != 0)
                    {
                        HostData[] hostData = MasterServer.PollHostList();
                        string ip = "";
                        for (int j = 0; j < hostData[0].ip.Length; j++)
                            ip += hostData[0].ip[j];
                        hostIP = ip;
                        hostPort = hostData[0].port;
                        hostPortString = System.Convert.ToString(hostPort);
                    }
                }
                */

                /*
                if (networkType == NetworkType.CLIENT)
                {
                    if (MasterServer.PollHostList().Length != 0)
                    {
                        HostData[] hostData = MasterServer.PollHostList();
                        for (int i = 0; i < hostData.Length; i++)
                        {
                            string ip = "";
                            for (int j = 0; j < hostData[i].ip.Length; j++)
                                ip += hostData[i].ip[j];
                            if (hostIP == ip && hostPort == hostData[i].port)
                                UnityEngine.Network.Connect(hostData[i], "NsrqkrMFQyZQ5qIW");
                        }
                    }
                }
                */
            }
        }
    }

    public void OnFailedToConnectToMasterServer(NetworkConnectionError e)
    {
        throw new System.Exception(e.ToString());
    }

    public void OnPlayerConnected(NetworkPlayer player)
    {
        networkStatus = NetworkStatus.CONNECTED;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
        LoadGame();
        // is there something we need to do here? (ie, reverse of remove RPCs, etc)
    }

    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        if (networkStatus != NetworkStatus.NONE)
            networkStatus = NetworkStatus.DISCONNECTED;
        UnityEngine.Network.RemoveRPCs(player);
    }

    public void OnConnectedToServer()
    {
        networkStatus = NetworkStatus.CONNECTED;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
        LoadGame();
    }

    public void OnFailedToConnect()
    {
        networkStatus = NetworkStatus.CONNECTIONFAILED;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
    }

    public void OnDisconnectedFromServer()
    {
        if (networkStatus != NetworkStatus.NONE)
            networkStatus = NetworkStatus.DISCONNECTED;
    }

    public void ServerFound(string ip)
    {
        hostIP = ip;
        UnityEngine.Network.Connect(hostIP, hostPort, "NsrqkrMFQyZQ5qIW");
    }

    public void NoServerFound()
    {
        networkStatus = NetworkStatus.CONNECTIONFAILED;
    }

    public void Update()
    {
        // show a menu with the buttons: host server, connect to server, options, quit (actually goes in gui)

        if (networkType == NetworkType.SERVER && networkStatus == NetworkStatus.INITIALIZING)
        {
            if (initializeServer)
            {
                UnityEngine.Network.incomingPassword = "NsrqkrMFQyZQ5qIW";
                UnityEngine.Network.InitializeServer(32, listenPort, !UnityEngine.Network.HavePublicAddress());
                initializeServer = false;
            }
            // use OnServerInitialized()  (separate function from update)
            // use OnPlayerConnectedToServer() to get to gamestate as pregame
        }

        if (networkType == NetworkType.CLIENT && networkStatus == NetworkStatus.CONNECTING)
        {
            if (!connectionInProgress)
            {
                if (LANAutoConnect)
                    LANBroadcast.GetComponent<LANBroadcastService>().StartSearchBroadCasting(ServerFound, NoServerFound);
                else
                {
                    if (lan)
                        UnityEngine.Network.Connect(hostIP, hostPort, "NsrqkrMFQyZQ5qIW");
                }
                connectionInProgress = true;
            }
        }
        // use OnConnectedToServer()  (separate function from update)
    }

    public void OnGUI()
    {
        if (GUIInitialization)
        {
            textStyleMessageCaption = new GUIStyle();// ("label");
            //textStyleMessageCaption = GUI.skin.GetStyle("label");
            textStyleMessageCaption.alignment = TextAnchor.MiddleCenter;
            textStyleMessageCaption.fontSize = 64;
            textStyleMessageCaption.normal.textColor = new Color(1, 1, 1, 1);

            textStyleMessage = new GUIStyle();//("label");
            //textStyleMessage = GUI.skin.GetStyle("label");
            textStyleMessage.alignment = TextAnchor.MiddleCenter;
            textStyleMessage.fontSize = 12;
            textStyleMessage.normal.textColor = new Color(1, 1, 1, 1);

            textStyleMoveList = new GUIStyle();//("label");
            //textStyleMoveList = GUI.skin.GetStyle("label");
            textStyleMoveList.alignment = TextAnchor.UpperLeft;
            textStyleMoveList.fontSize = 12;
            textStyleMoveList.normal.textColor = new Color(1, 1, 1, 1);

            textStyleMessageLeftAlign = new GUIStyle();//("label");
            //textStyleMessage = GUI.skin.GetStyle("label");
            textStyleMessageLeftAlign.alignment = TextAnchor.MiddleLeft;
            textStyleMessageLeftAlign.fontSize = 12;
            textStyleMessageLeftAlign.normal.textColor = new Color(1, 1, 1, 1);

            textStyleMessageRightAlign = new GUIStyle();//("label");
            //textStyleMessage = GUI.skin.GetStyle("label");
            textStyleMessageRightAlign.alignment = TextAnchor.MiddleRight;
            textStyleMessageRightAlign.fontSize = 12;
            textStyleMessageRightAlign.normal.textColor = new Color(1, 1, 1, 1);

            textStyleButtons = new GUIStyle("button");
            //textStyleButtons = GUI.skin.GetStyle("button");
            textStyleButtons.alignment = TextAnchor.MiddleCenter;
            textStyleButtons.fontSize = 16;

            textBoxStyle = new GUIStyle("textarea");
            //textBoxStyle = GUI.skin.GetStyle("textarea");
            textBoxStyle.alignment = TextAnchor.MiddleLeft;
            textBoxStyle.fontSize = 12;
            //textBoxStyle.normal.textColor = new Color(1, 1, 1, 1);

            checkBoxStyle = new GUIStyle("toggle");
            checkBoxStyle.alignment = TextAnchor.MiddleLeft;
            checkBoxStyle.fontSize = 12;
            //checkBoxStyle.normal.textColor = new Color(1, 1, 1, 1);

            GUIInitialization = false;
        }

        // scale to screen resolution
        textStyleMessageCaption.fontSize = (int)(64 * res / 5f);
        textStyleMessage.fontSize = (int)(12 * res / 5f);
        textStyleMoveList.fontSize = (int)(12 * res / 5f);
        textStyleMessageLeftAlign.fontSize = (int)(12 * res / 5f);
        textStyleMessageRightAlign.fontSize = (int)(12 * res / 5f);
        textStyleButtons.fontSize = (int)(16 * res / 5f);
        textBoxStyle.fontSize = (int)(12 * res / 5f);
        checkBoxStyle.fontSize = (int)(12 * res / 5f);

        if (menuState == MenuState.MAIN)
        {
            GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
            GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
            GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Chess", textStyleMessageCaption);
            if (GUI.Button(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2, 24 * res, 5 * res), "Single Player", textStyleButtons))
            {
                singlePlayer = true;
                LoadGame();
            }
            if (GUI.Button(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2 + 6 * res, 24 * res, 5 * res), "Host a Game", textStyleButtons))
            {
                menuState = MenuState.HOST;
                singlePlayer = false;
            }
            if (GUI.Button(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2 + 12 * res, 24 * res, 5 * res), "Join a Game", textStyleButtons))
            {
                menuState = MenuState.JOIN;
                singlePlayer = false;
            }
            if (GUI.Button(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2 + 18 * res, 24 * res, 5 * res), "Options", textStyleButtons))
                menuState = MenuState.OPTIONS;
            if (GUI.Button(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2 + 24 * res, 24 * res, 5 * res), "Quit", textStyleButtons))
                Application.Quit();
        }
        if (menuState == MenuState.HOST)
        {
            networkType = NetworkType.SERVER;
            if (networkStatus == NetworkStatus.NONE)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Host a Game", textStyleMessageCaption);
                GUI.Label(new Rect(Screen.width / 2 - 21 * res, Screen.height / 2, 20 * res, 5 * res), "Port Number:", textStyleMessageRightAlign);
                hostPortString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2, 20 * res, 5 * res), hostPortString, 5, textBoxStyle);
                GUI.Label(new Rect(Screen.width / 2 - 21 * res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), "LAN:", textStyleMessageRightAlign);
                lan = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6 * res + (int)(2 * res / 5), 5 * res, 5 * res), lan, "", checkBoxStyle);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 12 * res, 20 * res, 5 * res), "Host", textStyleButtons))
                {
                    int hold;
                    if (!int.TryParse(hostPortString, out hold) || hold < 0 || hold > 65535)
                    {
                        hostPortString = "";
                        GUIErrorMsg = true;
                    }
                    else
                    {
                        hostPort = System.Convert.ToInt32(hostPortString);
                        listenPort = hostPort;
                        hostIP = UnityEngine.Network.player.ipAddress;
                        initializeServer = true;
                        networkStatus = NetworkStatus.INITIALIZING;
                        GUIErrorMsg = false;
                    }
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 18 * res, 20 * res, 5 * res), "AutoLAN", textStyleButtons))
                {
                    GUIErrorMsg = false;
                    LANAutoConnect = true;
                    initializeServer = true;
                    networkStatus = NetworkStatus.INITIALIZING;
                    lan = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 24 * res, 20 * res, 5 * res), "Back", textStyleButtons))
                {
                    GUIErrorMsg = false;
                    menuState = MenuState.MAIN;
                }
                if (GUIErrorMsg)
                    GUI.Label(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 30 * res, 20 * res, 5 * res), "Invalid IP/port", textStyleMessage);
            }
            if (networkStatus == NetworkStatus.NOCONNECTION)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 30 * res, Screen.height / 2, 60 * res, 5 * res), "You are not connected to the internet", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), "OK", textStyleButtons))
                    networkStatus = NetworkStatus.NONE;
            }
            if (networkStatus == NetworkStatus.INITIALIZING)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 80 * res, Screen.height / 4 - 15 * res, 160 * res, 30 * res), "Initializing Server", textStyleMessageCaption);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2, 20 * res, 5 * res), "Cancel", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }
            }
            if (networkStatus == NetworkStatus.WAITING)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 80 * res, Screen.height / 4 - 15 * res, 160 * res, 30 * res), "Waiting for Players", textStyleMessageCaption);
                GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2, 40 * res, 5 * res), "Your IP address is:", textStyleMessageRightAlign);
                if (lan)
                    hostIP = UnityEngine.Network.player.ipAddress;
                GUI.Label(new Rect(Screen.width / 2 + res, Screen.height / 2, 40 * res, 5 * res), hostIP, textStyleMessageLeftAlign);
                GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2 + 6 * res, 40 * res, 5 * res), "Your port number is:", textStyleMessageRightAlign);
                if (lan)
                {
                    hostPort = UnityEngine.Network.player.port;
                    hostPortString = System.Convert.ToString(hostPort);
                }
                GUI.Label(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6 * res, 40 * res, 5 * res), hostPortString, textStyleMessageLeftAlign);
                /*
                if (!lan)
                {
                    GUI.Label(new Rect(Screen.width / 2 - 205, Screen.height / 2 + 60, 200, 25), "Give this token to client:", textStyleMessageRightAlign);
                    if (serverFound)
                        GUI.Label(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 60, 200, 25), serverid, textStyleMessageLeftAlign);
                }*/
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 2 + 12 * res, 80 * res, 5 * res), "Connection type status: " + UnityEngine.Network.TestConnectionNAT(), textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 18 * res, 20 * res, 5 * res), "Disconnect", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }

                if (!lan)
                {
                    if (MasterServer.PollHostList().Length != 0)
                    {
                        if (!serverFound)
                        {
                            HostData[] hostData = MasterServer.PollHostList();
                            for (int i = 0; i < hostData.Length; i++)
                            {
                                if (hostData[i].comment == serverid)
                                {
                                    WebClient client = new WebClient();
                                    string ret = client.DownloadString("http://checkip.dyndns.org");
                                    string[] par = ret.Split(':');
                                    string[] par2 = par[1].Split('<');
                                    if (par2[0].Length >= 7)
                                    {
                                        hostIP = par2[0].Substring(1);
                                        hostPortString = System.Convert.ToString(hostPort);
                                    }
                                    else
                                        hostIP = "Error";
                                    break;
                                }
                            }
                            serverFound = true;
                        }
                    }
                    else
                    {
                        hostIP = "Querying...";
                        hostPortString = "";
                        MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
                    }
                }
            }
            if (networkStatus == NetworkStatus.DISCONNECTED)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Error", textStyleMessageCaption);
                GUI.Label(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2, 20 * res, 5 * res), "Disconnected", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), "OK", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }
            }
        }
        if (menuState == MenuState.JOIN)
        {
            networkType = NetworkType.CLIENT;
            if (networkStatus == NetworkStatus.NONE)
            {
                connectionInProgress = false;

                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Join a Game", textStyleMessageCaption);

                GUI.Label(new Rect(Screen.width / 2 - 21 * res, Screen.height / 2 + 12 * res, 20 * res, 5 * res), "LAN:", textStyleMessageRightAlign);
                lan = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 12 * res + (int)(2 * res / 5), 5 * res, 5 * res), lan, "", checkBoxStyle);
                //if (lan)
                //{
                GUI.Label(new Rect(Screen.width / 2 - 21 * res, Screen.height / 2, 20 * res, 5 * res), "IP Address:", textStyleMessageRightAlign);
                hostIP = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2, 30 * res, 5 * res), hostIP, 15, textBoxStyle);
                GUI.Label(new Rect(Screen.width / 2 - 21 * res, Screen.height / 2 + 12 * res, 20 * res, 5 * res), "Port Number:", textStyleMessageRightAlign);
                hostPortString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), hostPortString, 5, textBoxStyle);
                /*}
                else
                {
                    GUI.Label(new Rect(Screen.width / 2 - 105, Screen.height / 2, 100, 25), "Server token:", textStyleMessageRightAlign);
                    token = GUI.TextField(new Rect(Screen.width / 2 + 5, Screen.height / 2, 150, 25), token, 4, textBoxStyle);
                }*/
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 18 * res, 20 * res, 5 * res), "Join", textStyleButtons))
                {
                    //if (lan)
                    //{
                    bool invalid = false;
                    string[] s = hostIP.Split('.');
                    int hold;
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (!int.TryParse(s[i], out hold) || hold < 0 || hold > 255)
                            invalid = true;
                    }
                    if (!int.TryParse(hostPortString, out hold) || hold < 0 || hold > 65535)
                        invalid = true;
                    if (invalid)
                    {
                        hostIP = "";
                        hostPortString = "";
                        GUIErrorMsg = true;
                    }
                    else
                    {
                        if (lan)
                        {
                            hostPort = System.Convert.ToInt32(hostPortString);
                            listenPort = hostPort;
                            networkStatus = NetworkStatus.CONNECTING;
                        }
                        else
                        {
                            networkStatus = NetworkStatus.CONNECTING;
                            MasterServer.ClearHostList();
                            MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
                            GUIErrorMsg = false;
                        }

                        //MasterServer.ClearHostList();
                        //MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
                    }
                    //}
                    /*
                    else
                    {
                        networkStatus = NetworkStatus.CONNECTING;
                        MasterServer.ClearHostList();
                        MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
                        GUIErrorMsg = false;
                    }*/
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 24 * res, 20 * res, 5 * res), "AutoLAN", textStyleButtons))
                {
                    GUIErrorMsg = false;
                    LANAutoConnect = true;
                    networkStatus = NetworkStatus.CONNECTING;
                    lan = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 30 * res, 20 * res, 5 * res), "Back", textStyleButtons))
                {
                    GUIErrorMsg = false;
                    menuState = MenuState.MAIN;
                }
                if (GUIErrorMsg)
                    GUI.Label(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 36 * res, 20 * res, 5 * res), "Invalid IP/port", textStyleMessage);
            }
            if (networkStatus == NetworkStatus.CONNECTING)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Connecting", textStyleMessageCaption);

                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2, 20 * res, 5 * res), "Cancel", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 2 + 12 * res, 80 * res, 5 * res), "Connection type status: " + UnityEngine.Network.TestConnectionNAT(), textStyleMessage);

                if (!lan)
                {
                    if (MasterServer.PollHostList().Length != 0 && !connectionInProgress)
                    {
                        /*
                        HostData[] hostData = MasterServer.PollHostList();
                        for (int i = 0; i < hostData.Length; i++)
                        {
                            if (hostData[i].comment == token)
                            {
                                UnityEngine.Network.Connect(hostData[i], "NsrqkrMFQyZQ5qIW");
                                connectionInProgress = true;
                                break;
                            }
                        }*/
                        HostData[] hostData = MasterServer.PollHostList();
                        for (int i = 0; i < hostData.Length; i++)
                        {
                            string ip = "";
                            for (int j = 0; j < hostData[i].ip.Length; j++)
                                ip += hostData[i].ip[j];
                            if (hostIP == ip && hostPort == hostData[i].port)
                                UnityEngine.Network.Connect(hostData[i], "NsrqkrMFQyZQ5qIW");
                        }
                    }
                }
            }
            if (networkStatus == NetworkStatus.CONNECTIONFAILED)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Error", textStyleMessageCaption);
                GUI.Label(new Rect(Screen.width / 2 - 20 * res, Screen.height / 2, 40 * res, 5 * res), "Failed to connect to host", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), "OK", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }
            }
            if (networkStatus == NetworkStatus.DISCONNECTED)
            {
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40 * res, Screen.height / 4 - 15 * res, 80 * res, 30 * res), "Error", textStyleMessageCaption);
                GUI.Label(new Rect(Screen.width / 2 - 20 * res, Screen.height / 2, 40 * res, 5 * res), "Disconnected from host", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 6 * res, 20 * res, 5 * res), "OK", textStyleButtons))
                {
                    networkStatus = NetworkStatus.NONE;
                    UnityEngine.Network.Disconnect(200);
                }
            }
        }
        if (menuState == MenuState.OPTIONS)
        {
            GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
            GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
            GUI.Label(new Rect(Screen.width / 2 - 80 * res, Screen.height / 4 - 15 * res, 160 * res, 30 * res), "Options", textStyleMessageCaption);

            GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2, 40 * res, 5 * res), "Resolution:", textStyleMessageRightAlign);
            if (GUI.Button(new Rect(Screen.width / 2 + res, Screen.height / 2, 5 * res, 5 * res), "<", textStyleButtons))
            {
                if (res > 2)
                    res--;
                screenWidth = res * 256;
                screenHeight = res * 144;
                Screen.SetResolution(screenWidth, screenHeight, isFullScreen);
            }
            GUI.Label(new Rect(Screen.width / 2 + 8 * res, Screen.height / 2, 20 * res, 5 * res), System.Convert.ToString(screenWidth) + "x" + System.Convert.ToString(screenHeight), textStyleMessage);
            if (GUI.Button(new Rect(Screen.width / 2 + 30 * res, Screen.height / 2, 5 * res, 5 * res), ">", textStyleButtons))
            {
                if (res < 8)
                    res++;
                screenWidth = res * 256;
                screenHeight = res * 144;
                Screen.SetResolution(screenWidth, screenHeight, isFullScreen);
            }
            GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2 + 6 * res, 40 * res, 5 * res), "Sound Effects:", textStyleMessageRightAlign);
            sound = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6 * res + (int)(2 * res / 5), 5 * res, 5 * res), sound, "", checkBoxStyle);
            if (!sound)
            {
                soundVol = 0;
                soundVolString = "0";
            }
            else
            {
                if (soundVol == 0)
                {
                    soundVol = 1;
                    soundVolString = "1";
                }
            }
            GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2 + 12 * res, 40 * res, 5 * res), "Sound Volume:", textStyleMessageRightAlign);
            soundVolString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 12 * res, 8 * res, 5 * res), soundVolString, 3, textBoxStyle);
            float hold;
            if ((!float.TryParse(soundVolString, out hold) || hold < 0 || hold > 1) && soundVolString != "." && soundVolString != "")
                soundVolString = System.Convert.ToString(soundVol);
            else
                soundVol = hold;
            //foreach (GameObject p in pieces)
            //    p.audio.volume = soundVol;
            //announcer.audio.volume = soundVol;
            GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2 + 18 * res, 40 * res, 5 * res), "Music:", textStyleMessageRightAlign);
            music = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 18 * res + (int)(2 * res / 5), 5 * res, 5 * res), music, "", checkBoxStyle);
            if (!music)
            {
                musicVol = 0;
                musicVolString = "0";
            }
            else
            {
                if (musicVol == 0)
                {
                    musicVol = 1;
                    musicVolString = "1";
                }
            }
            GUI.Label(new Rect(Screen.width / 2 - 41 * res, Screen.height / 2 + 24 * res, 40 * res, 5 * res), "Music Volume:", textStyleMessageRightAlign);
            musicVolString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 24 * res, 8 * res, 5 * res), musicVolString, 3, textBoxStyle);
            if ((!float.TryParse(musicVolString, out hold) || hold < 0 || hold > 1) && musicVolString != "." && musicVolString != "")
                musicVolString = System.Convert.ToString(musicVol);
            else
                musicVol = hold;
            //mainCamera.audio.volume = musicVol;

            File.WriteAllText("chess.ini", "Resolution " + System.Convert.ToString(res) + "\nSoundVolume " + System.Convert.ToString(soundVol) + "\nMusicVolume " + System.Convert.ToString(musicVol));

            if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 30 * res, 20 * res, 5 * res), "Back", textStyleButtons))
            {
                menuState = MenuState.MAIN;
            }

            /*GUI.Box(new Rect(11*res, Screen.height / 2 - res, Screen.width / 3 - 4 * res, 32*res), "");
            GUI.Label(new Rect(Screen.width / 3 - 61*res, Screen.height / 2, 60 * res, 5*res), "Is Cerb cool:", textStyleMessageRightAlign);
            cerbCool1 = GUI.Toggle(new Rect(Screen.width / 3 + res, Screen.height / 2 + (int)(2 * res / 5), 5 * res, 5 * res), cerbCool1, "", checkBoxStyle);
            GUI.Label(new Rect(Screen.width / 3 - 61 * res, Screen.height / 2 + 5 * res, 60 * res, 5 * res), "Is Cerb very cool:", textStyleMessageRightAlign);
            cerbCool2 = GUI.Toggle(new Rect(Screen.width / 3 + res, Screen.height / 2 + 5*res + (int)(2 * res / 5), 5 * res, 5 * res), cerbCool2, "", checkBoxStyle);
            GUI.Label(new Rect(Screen.width / 3 - 61 * res, Screen.height / 2 + 10 * res, 60 * res, 5 * res), "Is Cerb super cool:", textStyleMessageRightAlign);
            cerbCool3 = GUI.Toggle(new Rect(Screen.width / 3 + res, Screen.height / 2 + 10 * res + (int)(2 * res / 5), 5 * res, 5 * res), cerbCool3, "", checkBoxStyle);
            GUI.Label(new Rect(Screen.width / 3 - 61 * res, Screen.height / 2 + 15 * res, 60 * res, 5 * res), "Is Cerb ultra cool:", textStyleMessageRightAlign);
            cerbCool4 = GUI.Toggle(new Rect(Screen.width / 3 + res, Screen.height / 2 + 15 * res + (int)(2 * res / 5), 5 * res, 5 * res), cerbCool4, "", checkBoxStyle);
            GUI.Label(new Rect(Screen.width / 3 - 61 * res, Screen.height / 2 + 20 * res, 60 * res, 5 * res), "Is Cerb balls-to-the-wall fucking awesome cool:", textStyleMessageRightAlign);
            cerbCool5 = GUI.Toggle(new Rect(Screen.width / 3 + res, Screen.height / 2 + 20 * res + (int)(2 * res / 5), 5 * res, 5 * res), cerbCool5, "", checkBoxStyle);
            cerbCount = 0;
            if(cerbCool1) cerbCount++;
            if(cerbCool2) cerbCount++;
            if(cerbCool3) cerbCount++;
            if(cerbCool4) cerbCount++;
            if(cerbCool5) cerbCount++;
            GUI.Label(new Rect(2*res, Screen.height / 2 + 25*res, 80*res, 5*res), "You currently have answered " + System.Convert.ToString(cerbCount) + " survey questions correctly", textStyleMessage);
            if(cerbCount == 5) {
                GUI.Label(new Rect(Screen.width / 2 - 80*res, Screen.height / 2 + 30*res, 160*res, 5*res), "You have successfully completed this survey and now may exit the options menu!", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 35*res, 20*res, 5*res), "OK", textStyleButtons))
                {
                    menuState = MenuState.MAIN;
                }
            }*/
        }

    }
}
