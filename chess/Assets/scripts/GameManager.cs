using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;
using System.IO;

public enum GameState { MENU, PREGAME, RUNNING, PROMOTION, DRAWPOSSIBLE, OFFERINGDRAW, BUSY, PAUSED, GAMEOVER }

public enum MenuState { NONE, MAIN, HOST, JOIN, OPTIONS }

public enum NetworkType { NONE, SERVER, CLIENT }
public enum NetworkStatus { NONE, DISCONNECTED, CONNECTIONFAILED, NOCONNECTION, INITIALIZING, WAITING, CONNECTING, CONNECTED }

public struct SquarePosition
{
    public int x;
    public int y;

    public SquarePosition(int px, int py)
    {
        x = px;
        y = py;
    }
}

public struct Move {
    public int x;
    public int y;
    public bool entered;
    public int num;
    public static int count = 0;

    public Move(int mx, int my) {
        x = mx;
        y = my;
        entered = false;
        num = count;
        count++;
    }

    public void move(int mx, int my)
    {
        x = mx;
        y = my;
        entered = true;
        num = count;
        count++;
    }
}

public struct BoardState {
    public PieceInfo[,] bs;

    public BoardState(GameObject[,] sq) {
        bs = new PieceInfo[8,8];
        for(int i=0; i<8; i++) {
            for(int j=0; j<8; j++) {
                bs[i,j] = sq[i,j].GetComponent<PieceInterface>().pieceInfo;
            }
        }
    }

    public static bool operator==(BoardState lhs, BoardState rhs) {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (lhs.bs[i, j] != rhs.bs[i, j])
                    return false;
            }
        }
        return true;
    }

    public static bool operator !=(BoardState lhs, BoardState rhs)
    {
        return !(lhs == rhs);
    }

    public bool Equals(BoardState bs)
    {
        return this == bs;
    }

    public override bool Equals(object o)
    {
        if(!(o is BoardState))
            throw new System.ArgumentException("Argument is not of type BoardState");
        return Equals((BoardState)o);
    }

    public override int GetHashCode()
    {
        int hash = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                hash += (i * 8 + j) * bs[i, j].GetHashCode();
            }
        }
        return hash;
    }
}

public class GameManager : MonoBehaviour
{

    public GameState gameState;

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

    // Cursor Properties
    public GameObject defaultCursor;
    public GameObject currentCursor;

    public ArrayList actionObjects = new ArrayList();	// array to hold the action objects in case they need to be reactivated
    public int totalActionObjects;

    //public GameObject[] playerPieces;
    //public GameObject[] computerPieces;

    public GameObject selection;
    public bool selectionChanged;

    public GameObject[] pieces;
    public PieceColor turn;
    public Move move;
    public GameObject[,] squares;
    public int squareSize;
    public List<GameObject> highlighted;
    public List<string> moveList;
    public List<bool> sq; // another stupid hack to see info in unity
    public GameObject selectionRing;
    public ParticleSystem selectionParticles;
    public ParticleSystem movementParticles;
    public GameObject squareHighlight;
    public bool busyUpdate;
    public bool done;
    public GameObject dummyPiece;
    public GameObject tempHolder;

    public GameObject takingPiece;
    public bool pieceTaken;
    public int whitePiecesTaken;
    public int blackPiecesTaken;
    public int takenPieceOffset;

    public bool castle;
    public GameObject castleRook;
    public int castleRookX;
    public int castleRookY;
    public bool check;
    public GameObject checkPiece1;
    public GameObject checkPiece2;
    public bool checkmate;
    public bool stalemate;
    public bool draw;
    public bool promotion;
    public GameObject promotedPiece;
    public int moveRepetition;
    public bool justAfterThreefoldRepetition;
    public List<BoardState> boardStates;
    public int fiftyMoveCounter;

    bool firstGameOverUpdate;

    public bool whiteWon;

    public string clockstr;
    public float clockf;

    public int textTimer;
    public Color textColor;
    public string message;
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

    public Camera mainCamera;
    public Vector3 defaultCameraPosition;
    public Vector3 currentCameraPosition;

    public bool cameraProcessing;
    public bool moveAnimation;

    public GameObject announcer;

    public bool isMouse;
    public bool isKeyboard;
    public bool isController;

    public MenuState menuState;

    public bool singlePlayer;
    public PieceColor multiOwnColor;
    public bool relayed;
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


    bool debugging;



    public bool cerbCool1;
    public bool cerbCool2;
    public bool cerbCool3;
    public bool cerbCool4;
    public bool cerbCool5;
    public int cerbCount;

    /*
    public GameObject testParticles;
    public Vector3 pdiff;
    public Vector3 plast;
    */

    /*
    public void TraceMessage(string msg, [CallerLineNumber] int lineNum = 0)
    {
        Debug.Log(msg + " (at line " + System.Convert.ToString(lineNum) + ")");
    }
    */

    public bool inCheck()
    {
        foreach (GameObject g in pieces)
        {
            if (turn != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g))
                return true;
        }
        return false;
    }

    public bool pathToKing(GameObject attacker, int x, int y)
    {
        bool pathAvailable = false;

        PieceInfo a = attacker.GetComponent<PieceInterface>().pieceInfo;
        GameObject attackedKing;
        if (a.color == PieceColor.WHITE)
            attackedKing = pieces[16];
        else
            attackedKing = pieces[0];
        PieceInfo k = attackedKing.GetComponent<PieceInterface>().pieceInfo;
        int kingx = k.x;
        int kingy = k.y;
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
        {
            kingx = x;
            kingy = y;
        }
        int maxx = Mathf.Max(a.x, kingx);
        int maxy = Mathf.Max(a.y, kingy);
        int minx = Mathf.Min(a.x, kingx);
        int miny = Mathf.Min(a.y, kingy);
        int absx = Mathf.Abs(a.x - kingx);
        int absy = Mathf.Abs(a.y - kingy);
        switch (a.type)
        {
            case PieceType.Q:
                if (absx == 0)
                {
                    for (int i = miny + 1; i < maxy; i++)
                    {
                        if(squares[a.x, i] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x) + ", " + System.Convert.ToString(i));
                        if (i == maxy - 1 && squares[a.x, i] == null)
                            pathAvailable = true;
                        if (squares[a.x, i] != null)
                            break;
                    }
                }
                if (absy == 0)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, a.y] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(a.y));
                        if (i == maxx - 1 && squares[i, a.y] == null)
                            pathAvailable = true;
                        if (squares[i, a.y] != null)
                            break;
                    }
                }
                if (kingx - a.x == kingy - a.y)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, i - minx + miny] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(i - minx + miny));
                        if (i == maxx - 1 && squares[i, i - minx + miny] == null)
                            pathAvailable = true;
                        if (squares[i, i - minx + miny] != null)
                            break;
                    }
                }
                if (kingx - a.x == a.y - kingy)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, minx - i + maxy] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(minx - i + maxy));
                        if (i == maxx - 1 && squares[i, minx - i + maxy] == null)
                            pathAvailable = true;
                        if (squares[i, minx - i + maxy] != null)
                            break;
                    }
                }
                break;
            case PieceType.B:
                if (kingx - a.x == kingy - a.y)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, i - minx + miny] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(i - minx + miny));
                        if (i == maxx - 1 && squares[i, i - minx + miny] == null)
                            pathAvailable = true;
                        if (squares[i, i - minx + miny] != null)
                            break;
                    }
                }
                if (kingx - a.x == a.y - kingy)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, minx - i + maxy] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(minx - i + maxy));
                        if (i == maxx - 1 && squares[i, minx - i + maxy] == null)
                            pathAvailable = true;
                        if (squares[i, minx - i + maxy] != null)
                            break;
                    }
                }
                break;
            case PieceType.N:
                if (kingx == a.x - 1 && kingy == a.y - 2) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 1) + ", " + System.Convert.ToString(a.y - 2));  pathAvailable = true;}
                if (kingx == a.x + 1 && kingy == a.y - 2) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 1) + ", " + System.Convert.ToString(a.y - 2));  pathAvailable = true;}
                if (kingx == a.x - 2 && kingy == a.y - 1) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 2) + ", " + System.Convert.ToString(a.y - 1));  pathAvailable = true;}
                if (kingx == a.x + 2 && kingy == a.y - 1) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 2) + ", " + System.Convert.ToString(a.y - 1));  pathAvailable = true;}
                if (kingx == a.x - 2 && kingy == a.y + 1) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 2) + ", " + System.Convert.ToString(a.y + 1));  pathAvailable = true;}
                if (kingx == a.x + 2 && kingy == a.y + 1) { if (debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 2) + ", " + System.Convert.ToString(a.y + 1)); pathAvailable = true;}
                if (kingx == a.x - 1 && kingy == a.y + 2) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 1) + ", " + System.Convert.ToString(a.y + 2));  pathAvailable = true;}
                if (kingx == a.x + 1 && kingy == a.y + 2) { if(debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 1) + ", " + System.Convert.ToString(a.y + 2));  pathAvailable = true;}
                break;
            case PieceType.R:
                if (absx == 0)
                {
                    for (int i = miny + 1; i < maxy; i++)
                    {
                        if (squares[a.x, i] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x) + ", " + System.Convert.ToString(i));
                        if (i == maxy - 1 && squares[a.x, i] == null)
                            pathAvailable = true;
                        if (squares[a.x, i] != null)
                            break;
                    }
                }
                if (absy == 0)
                {
                    for (int i = minx + 1; i < maxx; i++)
                    {
                        if (squares[i, a.y] == null && debugging)
                            Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(i) + ", " + System.Convert.ToString(a.y));
                        if (i == maxx - 1 && squares[i, a.y] == null)
                            pathAvailable = true;
                        if (squares[i, a.y] != null)
                            break;
                    }
                }
                break;
            case PieceType.P:
                if (a.color == PieceColor.WHITE)
                {
                    if (kingx == a.x - 1 && kingy == a.y + 1) { if (debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 1) + ", " + System.Convert.ToString(a.y + 1)); pathAvailable = true; }
                    if (kingx == a.x + 1 && kingy == a.y + 1) { if (debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 1) + ", " + System.Convert.ToString(a.y + 1)); pathAvailable = true; }
                }
                if (a.color == PieceColor.BLACK)
                {
                    if (kingx == a.x - 1 && kingy == a.y - 1) { if (debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x - 1) + ", " + System.Convert.ToString(a.y - 1)); pathAvailable = true; }
                    if (kingx == a.x + 1 && kingy == a.y - 1) { if (debugging) Debug.Log(a.id.ToString() + " has path to " + System.Convert.ToString(a.x + 1) + ", " + System.Convert.ToString(a.y - 1)); pathAvailable = true; }
                }
                break;
            default:
                break;
        }
        return pathAvailable;
    }

    bool pathToKing(GameObject attacker)
    {
        return pathToKing(attacker, -1, -1);
    }

    public List<SquarePosition> validMoves(GameObject piece)
    {
        List<SquarePosition> plist = new List<SquarePosition>();
        PieceInfo p = piece.GetComponent<PieceInterface>().pieceInfo;
        if (p.x < 0 || p.x > 7 || p.y < 0 || p.y > 7)
            throw new System.IndexOutOfRangeException("the current piece (" + p.id.ToString() + ") has an out of bounds position: " + System.Convert.ToString(p.x) + ", " + System.Convert.ToString(p.y) + " in GameManager");
        squares[p.x, p.y] = null;
        switch (p.type)
        {
            case PieceType.K:
                if (p.x > 0 && p.y > 0 && (squares[p.x - 1, p.y - 1] == null || p.color != squares[p.x - 1, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x - 1, p.y - 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x-1, p.y-1));
                }
                if (p.x > 0 && (squares[p.x - 1, p.y] == null || p.color != squares[p.x - 1, p.y].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x - 1, p.y))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x - 1, p.y));
                }
                if (p.x > 0 && p.y < 7 && (squares[p.x - 1, p.y + 1] == null || p.color != squares[p.x - 1, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x - 1, p.y + 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x - 1, p.y + 1));
                }
                if (p.y > 0 && (squares[p.x, p.y - 1] == null || p.color != squares[p.x, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x, p.y - 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x, p.y - 1));
                }
                if (p.y < 7 && (squares[p.x, p.y + 1] == null || p.color != squares[p.x, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x, p.y + 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x, p.y + 1));
                }
                if (p.x < 7 && p.y > 0 && (squares[p.x + 1, p.y - 1] == null || p.color != squares[p.x + 1, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x + 1, p.y - 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x + 1, p.y - 1));
                }
                if (p.x < 7 && (squares[p.x + 1, p.y] == null || p.color != squares[p.x + 1, p.y].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x + 1, p.y))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x + 1, p.y));
                }
                if (p.x < 7 && p.y < 7 && (squares[p.x + 1, p.y + 1] == null || p.color != squares[p.x + 1, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    bool canGo = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x + 1, p.y + 1))
                            canGo = false;
                    }
                    if (canGo)
                        plist.Add(new SquarePosition(p.x + 1, p.y + 1));
                }
                if (!p.hasMoved && p.x == 4 && squares[p.x + 1, p.y] == null && squares[p.x + 2, p.y] == null && squares[p.x + 3, p.y] != null && !squares[p.x + 3, p.y].GetComponent<PieceInterface>().pieceInfo.hasMoved && !inCheck())
                {
                    bool canCastle = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x + 1, p.y))
                            canCastle = false;
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x + 2, p.y))
                            canCastle = false;
                    }
                    if (canCastle)
                        plist.Add(new SquarePosition(p.x + 2, p.y));
                }
                if (!p.hasMoved && p.x == 4 && squares[p.x - 1, p.y] == null && squares[p.x - 2, p.y] == null && squares[p.x - 3, p.y] == null && squares[p.x - 4, p.y] != null && !squares[p.x - 4, p.y].GetComponent<PieceInterface>().pieceInfo.hasMoved && !inCheck())
                {
                    bool canCastle = true;
                    foreach (GameObject g in pieces)
                    {
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x - 1, p.y))
                            canCastle = false;
                        if (p.color != g.GetComponent<PieceInterface>().pieceInfo.color && pathToKing(g, p.x - 2, p.y))
                            canCastle = false;
                    }
                    if (canCastle)
                        plist.Add(new SquarePosition(p.x - 2, p.y));
                }
                break;
            case PieceType.Q:
                if (check && ((checkPiece1 != null && checkPiece1.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N) || (checkPiece2 != null && checkPiece2.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N)))
                    break;
                for (int i = -1; p.x + i >= 0 && (squares[p.x + i, p.y] == null || p.color != squares[p.x + i, p.y].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x + i, p.y] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y));
                        break;
                    }
                    squares[p.x + i, p.y] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y] = null;
                        continue;
                    }
                    squares[p.x + i, p.y] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y));
                }
                for (int i = 1; p.x + i < 8 && (squares[p.x + i, p.y] == null || p.color != squares[p.x + i, p.y].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y));
                        break;
                    }
                    squares[p.x + i, p.y] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y] = null;
                        continue;
                    }
                    squares[p.x + i, p.y] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y));
                }
                for (int i = -1; p.y + i >= 0 && (squares[p.x, p.y + i] == null || p.color != squares[p.x, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x, p.y + i));
                        break;
                    }
                    squares[p.x, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x, p.y + i] = null;
                        continue;
                    }
                    squares[p.x, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x, p.y + i));
                }
                for (int i = 1; p.y + i < 8 && (squares[p.x, p.y + i] == null || p.color != squares[p.x, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x, p.y + i));
                        break;
                    }
                    squares[p.x, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x, p.y + i] = null;
                        continue;
                    }
                    squares[p.x, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x, p.y + i));
                }
                for (int i = -1; p.x + i >= 0 && p.y + i >= 0 && (squares[p.x + i, p.y + i] == null || p.color != squares[p.x + i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x + i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y + i));
                        break;
                    }
                    squares[p.x + i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y + i));
                }
                for (int i = 1; p.x + i < 8 && p.y + i < 8 && (squares[p.x + i, p.y + i] == null || p.color != squares[p.x + i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y + i));
                        break;
                    }
                    squares[p.x + i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y + i));
                }
                for (int i = 1; p.x - i >= 0 && p.y + i < 8 && (squares[p.x - i, p.y + i] == null || p.color != squares[p.x - i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x - i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x - i, p.y + i));
                        break;
                    }
                    squares[p.x - i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x - i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x - i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x - i, p.y + i));
                }
                for (int i = 1; p.x + i < 8 && p.y - i >= 0 && (squares[p.x + i, p.y - i] == null || p.color != squares[p.x + i, p.y - i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y - i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y - i));
                        break;
                    }
                    squares[p.x + i, p.y - i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y - i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y - i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y - i));
                }
                break;
            case PieceType.B:
                if (check && ((checkPiece1 != null && checkPiece1.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N) || (checkPiece2 != null && checkPiece2.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N)))
                    break;
                for (int i = -1; p.x + i >= 0 && p.y + i >= 0 && (squares[p.x + i, p.y + i] == null || p.color != squares[p.x + i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x + i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y + i));
                        break;
                    }
                    squares[p.x + i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y + i));
                }
                for (int i = 1; p.x + i < 8 && p.y + i < 8 && (squares[p.x + i, p.y + i] == null || p.color != squares[p.x + i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y + i));
                        break;
                    }
                    squares[p.x + i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y + i));
                }
                for (int i = 1; p.x - i >= 0 && p.y + i < 8 && (squares[p.x - i, p.y + i] == null || p.color != squares[p.x - i, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x - i, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x - i, p.y + i));
                        break;
                    }
                    squares[p.x - i, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x - i, p.y + i] = null;
                        continue;
                    }
                    squares[p.x - i, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x - i, p.y + i));
                }
                for (int i = 1; p.x + i < 8 && p.y - i >= 0 && (squares[p.x + i, p.y - i] == null || p.color != squares[p.x + i, p.y - i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y - i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y - i));
                        break;
                    }
                    squares[p.x + i, p.y - i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y - i] = null;
                        continue;
                    }
                    squares[p.x + i, p.y - i] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y - i));
                }
                break;
            case PieceType.N:
                if (check && ((checkPiece1 != null && checkPiece1.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N) || (checkPiece2 != null && checkPiece2.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N)))
                    break;
                if (p.x > 1 && p.y > 0 && (squares[p.x - 2, p.y - 1] == null || p.color != squares[p.x - 2, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x - 2, p.y - 1] == null)
                    {
                        squares[p.x - 2, p.y - 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 2, p.y - 1));
                        squares[p.x - 2, p.y - 1] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 2, p.y - 1));
                    }
                }
                if (p.x > 1 && p.y < 7 && (squares[p.x - 2, p.y + 1] == null || p.color != squares[p.x - 2, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x - 2, p.y + 1] == null)
                    {
                        squares[p.x - 2, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 2, p.y + 1));
                        squares[p.x - 2, p.y + 1] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 2, p.y + 1));
                    }
                }
                if (p.x > 0 && p.y > 1 && (squares[p.x - 1, p.y - 2] == null || p.color != squares[p.x - 1, p.y - 2].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x - 1, p.y - 2] == null)
                    {
                        squares[p.x - 1, p.y - 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y - 2));
                        squares[p.x - 1, p.y - 2] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y - 2));
                    }
                }
                if (p.x > 0 && p.y < 6 && (squares[p.x - 1, p.y + 2] == null || p.color != squares[p.x - 1, p.y + 2].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x - 1, p.y + 2] == null)
                    {
                        squares[p.x - 1, p.y + 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y + 2));
                        squares[p.x - 1, p.y + 2] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y + 2));
                    }
                }
                if (p.x < 7 && p.y > 1 && (squares[p.x + 1, p.y - 2] == null || p.color != squares[p.x + 1, p.y - 2].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x + 1, p.y - 2] == null)
                    {
                        squares[p.x + 1, p.y - 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y - 2));
                        squares[p.x + 1, p.y - 2] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y - 2));
                    }
                }
                if (p.x < 7 && p.y < 6 && (squares[p.x + 1, p.y + 2] == null || p.color != squares[p.x + 1, p.y + 2].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x + 1, p.y + 2] == null)
                    {
                        squares[p.x + 1, p.y + 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y + 2));
                        squares[p.x + 1, p.y + 2] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y + 2));
                    }
                }
                if (p.x < 6 && p.y > 0 && (squares[p.x + 2, p.y - 1] == null || p.color != squares[p.x + 2, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x + 2, p.y - 1] == null)
                    {
                        squares[p.x + 2, p.y - 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 2, p.y - 1));
                        squares[p.x + 2, p.y - 1] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 2, p.y - 1));
                    }
                }
                if (p.x < 6 && p.y < 7 && (squares[p.x + 2, p.y + 1] == null || p.color != squares[p.x + 2, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color))
                {
                    if (squares[p.x + 2, p.y + 1] == null)
                    {
                        squares[p.x + 2, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 2, p.y + 1));
                        squares[p.x + 2, p.y + 1] = null;
                    }
                    else
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 2, p.y + 1));
                    }
                }
                break;
            case PieceType.R:
                if (check && ((checkPiece1 != null && checkPiece1.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N) || (checkPiece2 != null && checkPiece2.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N)))
                    break;
                for (int i = -1; p.x + i >= 0 && (squares[p.x + i, p.y] == null || p.color != squares[p.x + i, p.y].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x + i, p.y] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y));
                        break;
                    }
                    squares[p.x + i, p.y] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y] = null;
                        continue;
                    }
                    squares[p.x + i, p.y] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y));
                }
                for (int i = 1; p.x + i < 8 && (squares[p.x + i, p.y] == null || p.color != squares[p.x + i, p.y].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x + i, p.y] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x + i, p.y));
                        break;
                    }
                    squares[p.x + i, p.y] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x + i, p.y] = null;
                        continue;
                    }
                    squares[p.x + i, p.y] = null;
                    plist.Add(new SquarePosition(p.x + i, p.y));
                }
                for (int i = -1; p.y + i >= 0 && (squares[p.x, p.y + i] == null || p.color != squares[p.x, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i--)
                {
                    if (squares[p.x, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x, p.y + i));
                        break;
                    }
                    squares[p.x, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x, p.y + i] = null;
                        continue;
                    }
                    squares[p.x, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x, p.y + i));
                }
                for (int i = 1; p.y + i < 8 && (squares[p.x, p.y + i] == null || p.color != squares[p.x, p.y + i].GetComponent<PieceInterface>().pieceInfo.color); i++)
                {
                    if (squares[p.x, p.y + i] != null && !inCheck())
                    {
                        plist.Add(new SquarePosition(p.x, p.y + i));
                        break;
                    }
                    squares[p.x, p.y + i] = dummyPiece;
                    if (inCheck())
                    {
                        squares[p.x, p.y + i] = null;
                        continue;
                    }
                    squares[p.x, p.y + i] = null;
                    plist.Add(new SquarePosition(p.x, p.y + i));
                }
                break;
            case PieceType.P:
                if (check && ((checkPiece1 != null && checkPiece1.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N) || (checkPiece2 != null && checkPiece2.GetComponent<PieceInterface>().pieceInfo.type == PieceType.N)))
                    break;
                if (p.color == PieceColor.WHITE)
                {
                    if (p.y == 1 && (squares[p.x, p.y + 1] == null) && (squares[p.x, p.y + 2] == null))
                    {
                        squares[p.x, p.y + 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x, p.y + 2));
                        squares[p.x, p.y + 2] = null;
                    }
                    if (p.y < 7 && squares[p.x, p.y + 1] == null)
                    {
                        squares[p.x, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x, p.y + 1));
                        squares[p.x, p.y + 1] = null;
                    }
                    if (p.x > 0 && p.y < 7 && squares[p.x - 1, p.y + 1] != null && p.color != squares[p.x - 1, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color)
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y + 1));
                    }
                    if (p.x < 7 && p.y < 7 && squares[p.x + 1, p.y + 1] != null && p.color != squares[p.x + 1, p.y + 1].GetComponent<PieceInterface>().pieceInfo.color)
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y + 1));
                    }
                    if (p.x > 0 && p.y < 7 && squares[p.x - 1, p.y + 1] == null && squares[p.x - 1, p.y] != null && p.color != squares[p.x - 1, p.y].GetComponent<PieceInterface>().pieceInfo.color && squares[p.x - 1, p.y].GetComponent<PieceInterface>().pieceInfo.enPassantable)
                    {
                        squares[p.x - 1, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y + 1));
                        squares[p.x - 1, p.y + 1] = null;
                    }
                    if (p.x < 7 && p.y < 7 && squares[p.x + 1, p.y + 1] == null && squares[p.x + 1, p.y] != null && p.color != squares[p.x + 1, p.y].GetComponent<PieceInterface>().pieceInfo.color && squares[p.x + 1, p.y].GetComponent<PieceInterface>().pieceInfo.enPassantable)
                    {
                        squares[p.x + 1, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y + 1));
                        squares[p.x + 1, p.y + 1] = null;
                    }
                }
                else
                {
                    if (p.y == 6 && squares[p.x, p.y - 1] == null && squares[p.x, p.y - 2] == null)
                    {
                        squares[p.x, p.y - 2] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x, p.y - 2));
                        squares[p.x, p.y - 2] = null;
                    }
                    if (p.y > 0 && squares[p.x, p.y - 1] == null)
                    {
                        squares[p.x, p.y - 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x, p.y - 1));
                        squares[p.x, p.y - 1] = null;
                    }
                    if (p.x > 0 && p.y > 0 && squares[p.x - 1, p.y - 1] != null && p.color != squares[p.x - 1, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color)
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y - 1));
                    }
                    if (p.x < 7 && p.y > 0 && squares[p.x + 1, p.y - 1] != null && p.color != squares[p.x + 1, p.y - 1].GetComponent<PieceInterface>().pieceInfo.color)
                    {
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y - 1));
                    }
                    if (p.x > 0 && p.y < 7 && squares[p.x - 1, p.y - 1] == null && squares[p.x - 1, p.y] != null && p.color != squares[p.x - 1, p.y].GetComponent<PieceInterface>().pieceInfo.color && squares[p.x - 1, p.y].GetComponent<PieceInterface>().pieceInfo.enPassantable)
                    {
                        squares[p.x - 1, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x - 1, p.y - 1));
                        squares[p.x - 1, p.y + 1] = null;
                    }
                    if (p.x < 7 && p.y < 7 && squares[p.x + 1, p.y - 1] == null && squares[p.x + 1, p.y] != null && p.color != squares[p.x + 1, p.y].GetComponent<PieceInterface>().pieceInfo.color && squares[p.x + 1, p.y].GetComponent<PieceInterface>().pieceInfo.enPassantable)
                    {
                        squares[p.x + 1, p.y + 1] = dummyPiece;
                        if (!inCheck())
                            plist.Add(new SquarePosition(p.x + 1, p.y - 1));
                        squares[p.x + 1, p.y + 1] = null;
                    }
                }
                break;
            default:
                break;
        }
        squares[p.x, p.y] = piece;
        return plist;
    }

    public bool noMoves()
    {
        foreach (GameObject g in pieces)
        {
            if (g.GetComponent<PieceInterface>().pieceInfo.color != turn)
            {
                if (validMoves(g).Count > 0)
                    return false;
            }
        }
        return true;
    }

    void Awake() {
        /*
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
                            screenWidth = res*256;
                            screenHeight = res*144;
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
        */


        //gameState = GameState.MENU;
        gameState = GameState.PREGAME;
        squareSize = 8;
    }

    void Start() {
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

        debugging = false;

        // Hide/Show the sys. cursor when the game starts
        Screen.showCursor = true;
        currentCursor = defaultCursor;

        mainCamera = Camera.main; //GameObject.Find("Main Camera");
        if (mainCamera == null)
            throw new System.NullReferenceException("no camera");

        defaultCameraPosition = mainCamera.transform.position;
        currentCameraPosition = defaultCameraPosition;

        cameraProcessing = false;
        moveAnimation = false;
        busyUpdate = true;
        done = false;

        if(GameObject.Find("pieces") == null)
            throw new System.NullReferenceException("pieces not found in GameManager");
        pieces = GameObject.Find("pieces").GetComponent<Pieces>().getPieces();
        turn = PieceColor.WHITE;
        selection = null;
        selectionChanged = false;
        move = new Move(-1, -1);

        squares = new GameObject[,] { { pieces[6], pieces[8], null, null, null, null, pieces[24], pieces[22] },
                                      { pieces[4], pieces[9], null, null, null, null, pieces[25], pieces[20] },
                                      { pieces[2], pieces[10], null, null, null, null, pieces[26], pieces[18] },
                                      { pieces[1], pieces[11], null, null, null, null, pieces[27], pieces[17] },
                                      { pieces[0], pieces[12], null, null, null, null, pieces[28], pieces[16] },
                                      { pieces[3], pieces[13], null, null, null, null, pieces[29], pieces[19] },
                                      { pieces[5], pieces[14], null, null, null, null, pieces[30], pieces[21] },
                                      { pieces[7], pieces[15], null, null, null, null, pieces[31], pieces[23] } };
        sq = new List<bool>();
        for (int i = 0; i < 64; i++)
            sq.Add(false);
        highlighted = new List<GameObject>();
        moveList = new List<string>();
        selectionRing = GameObject.Find("selectionring");
        if (selectionRing == null)
            throw new System.NullReferenceException("selectionring not found in GameManager");

        selectionParticles = GameObject.Find("selectionparticles").GetComponent<ParticleSystem>();
        if (selectionParticles == null)
            throw new System.NullReferenceException("selectionparticles not found in GameManager");

        movementParticles = GameObject.Find("movementparticles").GetComponent<ParticleSystem>();
        if (movementParticles == null)
            throw new System.NullReferenceException("movementparticles not found in GameManager");

        squareHighlight = GameObject.Find("squarehighlight");
        if (squareHighlight == null)
            throw new System.NullReferenceException("squarehighlight not found in GameManager");
        dummyPiece = GameObject.Find("dummypiece");
        if (dummyPiece == null)
            throw new System.NullReferenceException("dummypiece not found in GameManager");
        dummyPiece.GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.NONE, PieceType.NONE, PieceColor.NONE, -1, -1);
        tempHolder = null;

        takingPiece = null;
        pieceTaken = false;
        whitePiecesTaken = 0;
        blackPiecesTaken = 0;
        takenPieceOffset = 6;

        castle = false;
        castleRook = null;
        castleRookX = -1;
        castleRookY = -1;
        check = false;
        checkPiece1 = null;
        checkPiece2 = null;
        checkmate = false;
        stalemate = false;
        draw = false;
        promotion = false;
        promotedPiece = null;
        moveRepetition = 0;
        justAfterThreefoldRepetition = false;
        boardStates = new List<BoardState>();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (squares[i, j] == null)
                    squares[i, j] = dummyPiece;
            }
        }
        boardStates.Add(new BoardState(squares));
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (squares[i, j] == dummyPiece)
                    squares[i, j] = null;
            }
        }
        fiftyMoveCounter = 0;

        firstGameOverUpdate = false;

        whiteWon = false;

        clockstr = "0:00:00";
        clockf = 0;

        textTimer = 0;
        textColor = new Color(1,1,1,1);
        message = "";

        textStyleMessageCaption = new GUIStyle();
        textStyleMessage = new GUIStyle();
        textStyleMessageLeftAlign = new GUIStyle();
        textStyleMessageRightAlign = new GUIStyle();
        textStyleMoveList = new GUIStyle();
        textStyleButtons = new GUIStyle();
        textBoxStyle = new GUIStyle();
        checkBoxStyle = new GUIStyle();
        GUIInitialization = true;

        GUIErrorMsg = false;

        announcer = GameObject.Find("announcer");
        if (announcer == null)
            throw new System.NullReferenceException("announcer not found in GameManager");

        menuState = MenuState.MAIN;

        relayed = false;
        initializeServer = false;
        connectionInProgress = false;
        serverFound = false;
        

        cerbCool1 = false;
        cerbCool2 = false;
        cerbCool3 = false;
        cerbCool4 = false;
        cerbCool5 = false;
        cerbCount = 0;

        /*
        testParticles = GameObject.Find("testparticles");
        if (testParticles == null)
            throw new System.NullReferenceException("testparticles not found in GameManager");
        pdiff = Vector3.zero;
        plast = testParticles.transform.position;
        */
    }

    public void OnServerInitialized()
    {
        if (lan)
        {
            networkStatus = NetworkStatus.WAITING;
            if(LANAutoConnect)
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
        gameState = GameState.PREGAME;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
        // is there something we need to do here? (ie, reverse of remove RPCs, etc)
    }

    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        if(networkStatus != NetworkStatus.NONE)
            networkStatus = NetworkStatus.DISCONNECTED;
        UnityEngine.Network.RemoveRPCs(player);

        //gameState = GameState.MENU;
        UnloadGame();
    }

    public void OnConnectedToServer()
    {
        networkStatus = NetworkStatus.CONNECTED;
        gameState = GameState.PREGAME;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
    }

    public void OnFailedToConnect()
    {
        networkStatus = NetworkStatus.CONNECTIONFAILED;
        if (LANAutoConnect)
            LANBroadcast.GetComponent<LANBroadcastService>().StopBroadCasting();
    }

    public void OnDisconnectedFromServer()
    {
        if(networkStatus != NetworkStatus.NONE)
            networkStatus = NetworkStatus.DISCONNECTED;

        //gameState = GameState.MENU;
        UnloadGame();
    }

    [RPC]
    public void RPCCall(int sel, bool ment, int mx, int my, bool senderIsServer)
    {
        if ((senderIsServer && UnityEngine.Network.isClient) || (!senderIsServer && UnityEngine.Network.isServer))
        {
            if (sel >= 0 && sel < 32)
                selection = pieces[sel];
            if (ment)
            {
                move.entered = true;
                move.x = mx;
                move.y = my;
            }
            relayed = true;
            if (UnityEngine.Network.isServer)
                Debug.Log(pieces[sel].GetComponent<PieceInterface>().pieceInfo.color.ToString() + " has moved " + pieces[sel].GetComponent<PieceInterface>().pieceInfo.id.ToString() + " to " + System.Convert.ToString(move.x) + ", " + System.Convert.ToString(move.y) + " (call received by server)");
            else
                Debug.Log(pieces[sel].GetComponent<PieceInterface>().pieceInfo.color.ToString() + " has moved " + pieces[sel].GetComponent<PieceInterface>().pieceInfo.id.ToString() + " to " + System.Convert.ToString(move.x) + ", " + System.Convert.ToString(move.y) + " (call received by client)");
        }
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

    public void UnloadGame()
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

        Application.LoadLevel("menu");
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteAll();
    }

    public void Update()
    {
        // checks that look for errors
        if (debugging)
        {
            foreach (GameObject g in pieces)
            {
                int x = -1, y = -1;
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (g == squares[i, j])
                        {
                            x = i;
                            y = j;
                            break;
                        }
                    }
                }
                if (x != g.GetComponent<PieceInterface>().pieceInfo.x || y != g.GetComponent<PieceInterface>().pieceInfo.y)
                    //throw new System.Exception(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + "'s position (" + System.Convert.ToString(g.GetComponent<PieceInterface>().pieceInfo.x) + "," + System.Convert.ToString(g.GetComponent<PieceInterface>().pieceInfo.y) + ") does not match with its position in the squares array (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
                    Debug.Log(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + "'s position (" + System.Convert.ToString(g.GetComponent<PieceInterface>().pieceInfo.x) + "," + System.Convert.ToString(g.GetComponent<PieceInterface>().pieceInfo.y) + ") does not match with its position in the squares array (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
                if (g.GetComponent<PieceInterface>().pieceInfo.alive && (x == -1 || y == -1))
                    //throw new System.Exception(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + " is alive, but the squares array does not reflect this: (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
                    Debug.Log(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + " is alive, but the squares array does not reflect this: (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
                if (!g.GetComponent<PieceInterface>().pieceInfo.alive && (x != -1 || y != -1))
                    //throw new System.Exception(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + " is dead, but the squares array does not reflect this: (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
                    Debug.Log(g.GetComponent<PieceInterface>().pieceInfo.id.ToString() + " is dead, but the squares array does not reflect this: (" + System.Convert.ToString(x) + "," + System.Convert.ToString(y) + ")");
            }
        }

        //if (!singlePlayer && gameState != GameState.MENU && UnityEngine.Network.peerType == NetworkPeerType.Disconnected)
        if (!singlePlayer && UnityEngine.Network.peerType == NetworkPeerType.Disconnected)
        {
            networkStatus = NetworkStatus.DISCONNECTED;

            //gameState = GameState.MENU;
            UnloadGame();
        }

        /*
        if (gameState == GameState.MENU)
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
        }*/

        if (singlePlayer)
        {

            if (checkmate)
            {
                gameState = GameState.GAMEOVER;
                textTimer = 200;
                if (whiteWon)
                    message = "White wins";
                else
                    message = "Black wins";
                if (firstGameOverUpdate)
                {
                    if (moveList.Count % 2 != 0)
                        moveList.Add("");
                    if (checkmate)
                    {
                        if (whiteWon)
                            moveList.Add("1-0");
                        else
                            moveList.Add("0-1");
                    }
                    if (stalemate || draw)
                        moveList.Add("½-½");
                    firstGameOverUpdate = false;
                }
            }

            if (Input.GetKeyUp("p"))
            {
                if (gameState == GameState.RUNNING)
                    gameState = GameState.PAUSED;
                else
                {
                    if (gameState == GameState.PAUSED)
                        gameState = GameState.RUNNING;
                }
            }
            if (Input.GetKeyUp("d"))
            {
                debugging = !debugging;
            }

            if (gameState == GameState.BUSY)
            {
                if (busyUpdate)
                {
                    busyUpdate = false;
                    if (moveAnimation)
                    {
                        if (selection != null)
                        {
                            selection.GetComponent<PieceInterface>().MovePiece(move.x, move.y);
                            selection = null;
                            if (pieceTaken)
                            {
                                if (takingPiece.GetComponent<PieceInterface>().pieceInfo.color == PieceColor.WHITE)
                                {
                                    takingPiece.GetComponent<PieceInterface>().Die(new Vector3(72, 2, 60 - whitePiecesTaken * takenPieceOffset));
                                    whitePiecesTaken++;
                                }
                                else
                                {
                                    takingPiece.GetComponent<PieceInterface>().Die(new Vector3(-8, 2, 4 + blackPiecesTaken * takenPieceOffset));
                                    blackPiecesTaken++;
                                }
                                pieceTaken = false;
                                takingPiece = null;
                            }
                        }

                        mainCamera.GetComponent<CameraMovement>().cameraState = CameraState.FOLLOWPIECE;
                        cameraProcessing = true;
                    }
                }
                if (done)
                {
                    gameState = GameState.RUNNING;
                    busyUpdate = true;
                    done = false;
                }
            }
            if (gameState == GameState.GAMEOVER)
            {

            }
            if (gameState == GameState.PREGAME)
            {
                // menus for setting up piece color, timer, etc
                gameState = GameState.RUNNING;
            }

        }

        else // multiplayer
        {
            if (checkmate)
            {
                gameState = GameState.GAMEOVER;
                textTimer = 200;
                if (whiteWon)
                    message = "White wins";
                else
                    message = "Black wins";
                if (firstGameOverUpdate)
                {
                    if (moveList.Count % 2 != 0)
                        moveList.Add("");
                    if (checkmate)
                    {
                        if (whiteWon)
                            moveList.Add("1-0");
                        else
                            moveList.Add("0-1");
                    }
                    if (stalemate || draw)
                        moveList.Add("½-½");
                    firstGameOverUpdate = false;
                }
            }

            if (networkType == NetworkType.SERVER && Input.GetKeyUp("p"))
            {
                if (gameState == GameState.RUNNING)
                    gameState = GameState.PAUSED;
                else
                {
                    if (gameState == GameState.PAUSED)
                        gameState = GameState.RUNNING;
                }
            }
            if (networkType == NetworkType.SERVER && Input.GetKeyUp("d"))
            {
                debugging = !debugging;
            }

            if (gameState == GameState.BUSY)
            {
                if (busyUpdate)
                {
                    busyUpdate = false;
                    if (moveAnimation)
                    {
                        if (selection != null)
                        {
                            selection.GetComponent<PieceInterface>().MovePiece(move.x, move.y);
                            selection = null;
                            if (pieceTaken)
                            {
                                if (takingPiece.GetComponent<PieceInterface>().pieceInfo.color == PieceColor.WHITE)
                                {
                                    takingPiece.GetComponent<PieceInterface>().Die(new Vector3(72, 2, 60 - whitePiecesTaken * takenPieceOffset));
                                    whitePiecesTaken++;
                                }
                                else
                                {
                                    takingPiece.GetComponent<PieceInterface>().Die(new Vector3(-8, 2, 4 + blackPiecesTaken * takenPieceOffset));
                                    blackPiecesTaken++;
                                }
                                pieceTaken = false;
                                takingPiece = null;
                            }
                        }

                        mainCamera.GetComponent<CameraMovement>().cameraState = CameraState.FOLLOWPIECE;
                        cameraProcessing = true;
                    }
                }
                if (done)
                {
                    gameState = GameState.RUNNING;
                    busyUpdate = true;
                    done = false;
                }
            }
            if (gameState == GameState.GAMEOVER)
            {

            }
            if (gameState == GameState.PREGAME)
            {
                LANAutoConnect = false;

                // menus for setting up piece color, timer, etc
                if (networkType == NetworkType.SERVER)
                {
                    multiOwnColor = PieceColor.WHITE;
                    mainCamera.transform.position = new Vector3(32, 60, -32);
                    mainCamera.transform.LookAt(new Vector3(32, 0, 32));
                }
                if (networkType == NetworkType.CLIENT)
                {
                    multiOwnColor = PieceColor.BLACK;
                    mainCamera.transform.position = new Vector3(32, 60, 96);
                    mainCamera.transform.LookAt(new Vector3(32, 0, 32));
                }
                gameState = GameState.RUNNING;
            }
        }

        if(gameState == GameState.RUNNING)
        {
            // clock
            clockf += Time.deltaTime;
            clockstr = System.Convert.ToString((int)(clockf / 3600)) + ":" + System.Convert.ToString((int)(clockf / 60) % 60).PadLeft(2, '0') + ":" + System.Convert.ToString((int)clockf % 60).PadLeft(2, '0');

            /*
            if (!moveAnimation)
            {
                movementParticles.transform.position = new Vector3(Random.Range(-2048, 2048), 0, Random.Range(-2048, 2048));
                movementParticles.GetComponent<ParticleSystem>().Play();
                movementParticles.GetComponent<ParticleSystem>().startSpeed = 200;
            }
            //-----------------------------------------------------------//
            if(testParticles.transform.position.x < 32 && testParticles.transform.position.z <= 16)
                testParticles.transform.position += new Vector3(Time.deltaTime * 8,0,0);
            if(testParticles.transform.position.x >= 32 && testParticles.transform.position.z < 32)
                testParticles.transform.position += new Vector3(0,0,Time.deltaTime * 8);
            if (testParticles.transform.position.x > 16 && testParticles.transform.position.z >= 32)
                testParticles.transform.position -= new Vector3(Time.deltaTime * 8,0,0);
            if (testParticles.transform.position.x <= 16 && testParticles.transform.position.z > 16)
                testParticles.transform.position -= new Vector3(0, 0, Time.deltaTime * 8);
            testParticles.GetComponent<ParticleSystem>().Play();

            pdiff = testParticles.transform.position - plast;
            plast = testParticles.transform.position;
            pdiff = new Vector3(pdiff.x, -pdiff.z, 0);

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1000];
            int len = testParticles.GetComponent<ParticleSystem>().GetParticles(particles);
            for (int i = 0; i < len; i++)
                particles[i].position -= pdiff;
            testParticles.GetComponent<ParticleSystem>().SetParticles(particles, len);
            //-----------------------------------------------------------//
            */

            if (selection != null)
            {
                if (selectionChanged)
                {
                    // network stuff
                    //------------------------------------------------------------------------------------
                    if (!singlePlayer)
                    {
                        if (relayed)
                            relayed = false;
                        else
                        {
                            int sel = -1;
                            for (int i = 0; i < 32; i++)
                            {
                                if (selection == pieces[i])
                                    sel = i;
                            }
                            networkView.RPC("RPCCall", RPCMode.All, sel, false, -1, -1, UnityEngine.Network.isServer);
                        }
                    }
                    //------------------------------------------------------------------------------------

                    selectionParticles.transform.position = selection.transform.position + new Vector3(0, -3.95f, 0);
                    selectionParticles.Play();

                    // selectionRing.GetComponent<SelectionRing>().startSpawn();      // <---- deprecated
                    foreach (GameObject l in highlighted)
                        Destroy(l);
                    highlighted.Clear();
                    List<SquarePosition> moves = validMoves(selection);
                    foreach (SquarePosition p in moves)
                    {
                        GameObject temp = (GameObject)Instantiate(squareHighlight, new Vector3(squareSize*(p.x + .5f), 1.05f, squareSize*(p.y + .5f)), Quaternion.identity);
                        temp.GetComponent<Highlight>().x = p.x; temp.GetComponent<Highlight>().y = p.y;
                        highlighted.Add(temp);
                    }
                    selectionChanged = false;
                }
            }
            else
            {
                foreach (GameObject l in highlighted)
                    Destroy(l);
                highlighted.Clear();

                selectionParticles.Stop();
            }

            if (move.entered)
            {
                // network stuff
                //------------------------------------------------------------------------------------
                if (!singlePlayer)
                {
                    if (relayed)
                        relayed = false;
                    else
                    {
                        int sel = -1;
                        for (int i = 0; i < 32; i++)
                        {
                            if (selection == pieces[i])
                                sel = i;
                        }
                        networkView.RPC("RPCCall", RPCMode.All, sel, true, move.x, move.y, UnityEngine.Network.isServer);
                    }
                }
                //------------------------------------------------------------------------------------

                if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P && ((turn == PieceColor.WHITE && move.y == 7) || ((turn == PieceColor.BLACK && move.y == 0)))) // pawn promotion piece selection
                {
                    gameState = GameState.PROMOTION;
                    promotion = true;
                    promotedPiece = selection;
                }
                else
                    promotion = false;

                string moveNotation = "";
                if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.K && selection.GetComponent<PieceInterface>().pieceInfo.x == move.x - 2) // castle
                    moveNotation += "0-0";
                else
                {
                    if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.K && selection.GetComponent<PieceInterface>().pieceInfo.x == move.x + 2) // queenside castle
                        moveNotation += "0-0-0";
                    else
                    {
                        if (selection.GetComponent<PieceInterface>().pieceInfo.type != PieceType.P)
                        {
                            List<GameObject> sameTypePieces = new List<GameObject>();
                            foreach (GameObject g in pieces)
                            {
                                if (selection != g && selection.GetComponent<PieceInterface>().pieceInfo.type == g.GetComponent<PieceInterface>().pieceInfo.type && selection.GetComponent<PieceInterface>().pieceInfo.color == g.GetComponent<PieceInterface>().pieceInfo.color)
                                    sameTypePieces.Add(g);
                            }
                            if (selection.GetComponent<PieceInterface>().pieceInfo.type != PieceType.P)
                                moveNotation += selection.GetComponent<PieceInterface>().pieceInfo.type.ToString(); // piece letter
                            // current location
                            bool ambiguous = false;
                            foreach (GameObject g in sameTypePieces)
                            {
                                if (pathToKing(g, move.x, move.y))
                                    ambiguous = true;
                            }
                            if (ambiguous)
                            {
                                bool needRank = false;
                                foreach (GameObject g in sameTypePieces)
                                {
                                    if (selection.GetComponent<PieceInterface>().pieceInfo.x == g.GetComponent<PieceInterface>().pieceInfo.x)
                                        needRank = true;
                                }
                                if (!needRank)
                                    moveNotation += System.Convert.ToChar(selection.GetComponent<PieceInterface>().pieceInfo.x + 97).ToString();
                                else
                                {
                                    bool needFile = false;
                                    foreach (GameObject g in sameTypePieces)
                                    {
                                        if (selection.GetComponent<PieceInterface>().pieceInfo.y == g.GetComponent<PieceInterface>().pieceInfo.y)
                                            needFile = true;
                                    }
                                    if (!needFile)
                                        moveNotation += selection.GetComponent<PieceInterface>().pieceInfo.y + 1;
                                    else
                                        moveNotation += System.Convert.ToChar(selection.GetComponent<PieceInterface>().pieceInfo.x + 97).ToString() + (selection.GetComponent<PieceInterface>().pieceInfo.y + 1);
                                }
                            }
                        }
                        else // pathToKing doesn't work for pawns because it checks for checks, which we don't want unless the pawn is actually taking something
                        {
                            if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P && move.x != selection.GetComponent<PieceInterface>().pieceInfo.x && squares[move.x, move.y] == null) // en passant
                                moveNotation += System.Convert.ToChar(selection.GetComponent<PieceInterface>().pieceInfo.x + 97).ToString();
                            else
                            {
                                bool ambiguous = false;
                                foreach (GameObject g in pieces)
                                {
                                    if (g != selection && g.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P && pathToKing(g, move.x, move.y))
                                        ambiguous = true;
                                }
                                if(squares[move.x, move.y] != null && ambiguous)
                                    moveNotation += System.Convert.ToChar(selection.GetComponent<PieceInterface>().pieceInfo.x + 97).ToString();
                            }
                        }
                        if (squares[move.x, move.y] != null) // capture
                            moveNotation += "x";
                        moveNotation += System.Convert.ToChar(move.x + 97).ToString() + (move.y + 1); // destination
                    }
                        // the check part of the notation is added in the check phase
                }

                // piece taken
                if (squares[move.x, move.y] != null)
                {
                    takingPiece = squares[move.x, move.y];
                    pieceTaken = true;
                }
                else // en passant
                {
                    if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P && move.x != selection.GetComponent<PieceInterface>().pieceInfo.x && squares[move.x, move.y] == null)
                    {
                        takingPiece = squares[move.x, move.y];
                        pieceTaken = true;
                    }
                }

                // castle
                if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.K && move.x - selection.GetComponent<PieceInterface>().pieceInfo.x == 2)
                {
                    PieceInfo p = selection.GetComponent<PieceInterface>().pieceInfo;
                    castle = true;
                    castleRook = squares[p.x + 3, p.y];
                    castleRookX = p.x + 1;
                    castleRookY = p.y;
                }
                if (selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.K && move.x - selection.GetComponent<PieceInterface>().pieceInfo.x == -2)
                {
                    PieceInfo p = selection.GetComponent<PieceInterface>().pieceInfo;
                    castle = true;
                    castleRook = squares[p.x - 4, p.y];
                    castleRookX = p.x - 1;
                    castleRookY = p.y;
                }

                squares[selection.GetComponent<PieceInterface>().pieceInfo.x, selection.GetComponent<PieceInterface>().pieceInfo.y] = null;
                squares[move.x, move.y] = selection;
                selection.GetComponent<PieceInterface>().pieceInfo.x = move.x;
                selection.GetComponent<PieceInterface>().pieceInfo.y = move.y;
                selection.GetComponent<PieceInterface>().x = move.x;
                selection.GetComponent<PieceInterface>().y = move.y;
                move.entered = false;

                if (castle)
                {
                    squares[castleRook.GetComponent<PieceInterface>().pieceInfo.x, castleRook.GetComponent<PieceInterface>().pieceInfo.y] = null;
                    squares[castleRookX, castleRookY] = castleRook;
                    castleRook.GetComponent<PieceInterface>().pieceInfo.x = castleRookX;
                    castleRook.GetComponent<PieceInterface>().pieceInfo.y = castleRookY;
                }

                foreach(GameObject g in pieces) {
                    if(g.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P)
                        g.GetComponent<PieceInterface>().pieceInfo.enPassantable = false;
                }
                if(selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P && (selection.GetComponent<PieceInterface>().pieceInfo.y == move.y+2 || selection.GetComponent<PieceInterface>().pieceInfo.y == move.y-2))
                    selection.GetComponent<PieceInterface>().pieceInfo.enPassantable = true;

                if(!promotion) {
                    check = false;
                    foreach(GameObject g in pieces)
                    {
                        if (g.GetComponent<PieceInterface>().pieceInfo.color == turn && pathToKing(g))
                        {
                            if (checkPiece1 == null)
                            {
                                check = true;
                                checkPiece1 = g;
                                moveNotation += "+";
                            }
                            else
                            {
                                checkPiece2 = g;
                                break;
                            }
                        }
                    }
                    if(!check)
                    {
                        checkPiece1 = null;
                        checkPiece2 = null;
                    }
                    if (noMoves())
                    {
                        if (check)
                        {
                            checkmate = true;
                            if (turn == PieceColor.WHITE)
                                whiteWon = true;
                            moveNotation += "+";
                            textTimer = 200;
                            message = "Checkmate.";
                            announcer.audio.clip = GameObject.Find("sndCheckmate").audio.clip;
                            announcer.audio.Play();
                        }
                        else
                        {
                            stalemate = true;
                            textTimer = 200;
                            message = "Stalemate.";
                            announcer.audio.clip = GameObject.Find("sndStalemate").audio.clip;
                            announcer.audio.Play();
                        }
                        firstGameOverUpdate = true;
                    }
                    else {
                        if (check)
                        {
                            textTimer = 200;
                            message = "Check!";
                            announcer.audio.clip = GameObject.Find("sndCheck").audio.clip;
                            announcer.audio.Play();
                        }
                    }
                }

                moveList.Add(moveNotation); // move list is finally updated (except for pawn promotion)

                foreach (GameObject l in highlighted)
                    Destroy(l);
                highlighted.Clear();

                moveAnimation = true;
                gameState = GameState.BUSY;

                fiftyMoveCounter++;
                if (squares[move.x, move.y] != null || selection.GetComponent<PieceInterface>().pieceInfo.type == PieceType.P)
                    fiftyMoveCounter = 0;

                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (squares[i, j] == null)
                            squares[i, j] = dummyPiece;
                    }
                }
                boardStates.Add(new BoardState(squares));
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (squares[i, j] == dummyPiece)
                            squares[i, j] = null;
                    }
                }
                moveRepetition = 0;
                for (int i = 0; i < boardStates.Count - 1; i++)
                {
                    if (boardStates[boardStates.Count - 1] == boardStates[i])
                        moveRepetition++;
                    if (moveRepetition >= 3)
                        break;
                }

                if (turn == PieceColor.WHITE)
                    turn = PieceColor.BLACK;
                else
                    turn = PieceColor.WHITE;

                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (squares[i,j] == null)
                            sq[j*8+i] = false;
                        else
                            sq[j*8+i] = true;
                    }
                }
                selectionParticles.transform.position = new Vector3(8, -32, 2048);
            }
            else
            {

            }
        }
        if (textTimer > 0)
            textTimer--;
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

        if (debugging)
        {
            textColor.a = 1;
            GUI.color = textColor;
            GUI.Label(new Rect(Screen.width / 2 - 30 * res, Screen.height / 2 - 10*res, 60*res, 20*res), "Debugger", textStyleMessageCaption);
        }

        // clock
        GUI.Label(new Rect(8, 8, 60, 20), clockstr, textStyleMessageRightAlign);

        /*
        if (gameState == GameState.MENU)
        {
            if (menuState == MenuState.MAIN)
            {
                GUI.Box(new Rect(10*res, 10*res, Screen.width - 20*res, Screen.height - 20*res), "");
                GUI.Box(new Rect(10*res, 10*res, Screen.width - 20*res, Screen.height - 20*res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Chess", textStyleMessageCaption);
                if (GUI.Button(new Rect(Screen.width / 2 - 12*res, Screen.height / 2, 24*res, 5*res), "Single Player", textStyleButtons))
                {
                    gameState = GameState.PREGAME;
                    singlePlayer = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 12*res, Screen.height / 2 + 6*res, 24*res, 5*res), "Host a Game", textStyleButtons))
                {
                    menuState = MenuState.HOST;
                    singlePlayer = false;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 12*res, Screen.height / 2 + 12*res, 24*res, 5*res), "Join a Game", textStyleButtons))
                {
                    menuState = MenuState.JOIN;
                    singlePlayer = false;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 12*res, Screen.height / 2 + 18*res, 24*res, 5*res), "Options", textStyleButtons))
                    menuState = MenuState.OPTIONS;
                if (GUI.Button(new Rect(Screen.width / 2 - 12*res, Screen.height / 2 + 24*res, 24*res, 5*res), "Quit", textStyleButtons))
                    Application.Quit();
            }
            if (menuState == MenuState.HOST)
            {
                networkType = NetworkType.SERVER;
                if (networkStatus == NetworkStatus.NONE)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Host a Game", textStyleMessageCaption);
                    GUI.Label(new Rect(Screen.width / 2 - 21*res, Screen.height / 2, 20*res, 5*res), "Port Number:", textStyleMessageRightAlign);
                    hostPortString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2, 20*res, 5*res), hostPortString, 5, textBoxStyle);
                    GUI.Label(new Rect(Screen.width / 2 - 21*res, Screen.height / 2 + 6*res, 20*res, 5*res), "LAN:", textStyleMessageRightAlign);
                    lan = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6*res + (int)(2*res/5), 5*res, 5*res), lan, "", checkBoxStyle);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 12*res, 20*res, 5*res), "Host", textStyleButtons))
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
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 18*res, 20*res, 5*res), "AutoLAN", textStyleButtons))
                    {
                        GUIErrorMsg = false;
                        LANAutoConnect = true;
                        initializeServer = true;
                        networkStatus = NetworkStatus.INITIALIZING;
                        lan = true;
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 24*res, 20*res, 5*res), "Back", textStyleButtons))
                    {
                        GUIErrorMsg = false;
                        menuState = MenuState.MAIN;
                    }
                    if(GUIErrorMsg)
                        GUI.Label(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 30*res, 20*res, 5*res), "Invalid IP/port", textStyleMessage);
                }
                if (networkStatus == NetworkStatus.NOCONNECTION)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 30*res, Screen.height / 2, 60*res, 5*res), "You are not connected to the internet", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 6*res, 20*res, 5*res), "OK", textStyleButtons))
                        networkStatus = NetworkStatus.NONE;
                }
                if (networkStatus == NetworkStatus.INITIALIZING)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 80*res, Screen.height / 4 - 15*res, 160*res, 30*res), "Initializing Server", textStyleMessageCaption);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2, 20*res, 5*res), "Cancel", textStyleButtons))
                    {
                        networkStatus = NetworkStatus.NONE;
                        UnityEngine.Network.Disconnect(200);
                    }
                }
                if (networkStatus == NetworkStatus.WAITING)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 80*res, Screen.height / 4 - 15*res, 160*res, 30*res), "Waiting for Players", textStyleMessageCaption);
                    GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2, 40*res, 5*res), "Your IP address is:", textStyleMessageRightAlign);
                    if(lan)
                        hostIP = UnityEngine.Network.player.ipAddress;
                    GUI.Label(new Rect(Screen.width / 2 + res, Screen.height / 2, 40*res, 5*res), hostIP, textStyleMessageLeftAlign);
                    GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2 + 6*res, 40*res, 5*res), "Your port number is:", textStyleMessageRightAlign);
                    if (lan)
                    {
                        hostPort = UnityEngine.Network.player.port;
                        hostPortString = System.Convert.ToString(hostPort);
                    }
                    GUI.Label(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6*res, 40*res, 5*res), hostPortString, textStyleMessageLeftAlign);
                    //if (!lan)
                    //{
                    //    GUI.Label(new Rect(Screen.width / 2 - 205, Screen.height / 2 + 60, 200, 25), "Give this token to client:", textStyleMessageRightAlign);
                    //    if (serverFound)
                    //        GUI.Label(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 60, 200, 25), serverid, textStyleMessageLeftAlign);
                    //}
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 + 12*res, 80*res, 5*res), "Connection type status: " + UnityEngine.Network.TestConnectionNAT(), textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 18*res, 20*res, 5*res), "Disconnect", textStyleButtons))
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
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Error", textStyleMessageCaption);
                    GUI.Label(new Rect(Screen.width / 2 - 10*res, Screen.height / 2, 20*res, 5*res), "Disconnected", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 6*res, 20*res, 5*res), "OK", textStyleButtons))
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
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Join a Game", textStyleMessageCaption);

                    GUI.Label(new Rect(Screen.width / 2 - 21*res, Screen.height / 2 + 12*res, 20*res, 5*res), "LAN:", textStyleMessageRightAlign);
                    lan = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 12 * res + (int)(2 * res / 5), 5*res, 5*res), lan, "", checkBoxStyle);
                    //if (lan)
                    //{
                        GUI.Label(new Rect(Screen.width / 2 - 21*res, Screen.height / 2, 20*res, 5*res), "IP Address:", textStyleMessageRightAlign);
                        hostIP = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2, 30*res, 5*res), hostIP, 15, textBoxStyle);
                        GUI.Label(new Rect(Screen.width / 2 - 21*res, Screen.height / 2 + 12*res, 20*res, 5*res), "Port Number:", textStyleMessageRightAlign);
                        hostPortString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6*res, 20*res, 5*res), hostPortString, 5, textBoxStyle);
                    //}
                    //else
                    //{
                    //    GUI.Label(new Rect(Screen.width / 2 - 105, Screen.height / 2, 100, 25), "Server token:", textStyleMessageRightAlign);
                    //    token = GUI.TextField(new Rect(Screen.width / 2 + 5, Screen.height / 2, 150, 25), token, 4, textBoxStyle);
                    //}
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 18*res, 20*res, 5*res), "Join", textStyleButtons))
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
                        //else
                        //{
                        //    networkStatus = NetworkStatus.CONNECTING;
                        //    MasterServer.ClearHostList();
                        //    MasterServer.RequestHostList("PVbUTIKPDJby3Lmr");
                        //    GUIErrorMsg = false;
                        //}
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 24*res, 20*res, 5*res), "AutoLAN", textStyleButtons))
                    {
                        GUIErrorMsg = false;
                        LANAutoConnect = true;
                        networkStatus = NetworkStatus.CONNECTING;
                        lan = true;
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 30*res, 20*res, 5*res), "Back", textStyleButtons))
                    {
                        GUIErrorMsg = false;
                        menuState = MenuState.MAIN;
                    }
                    if(GUIErrorMsg)
                        GUI.Label(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 36*res, 20*res, 5*res), "Invalid IP/port", textStyleMessage);
                }
                if (networkStatus == NetworkStatus.CONNECTING)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Connecting", textStyleMessageCaption);
                    
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2, 20*res, 5*res), "Cancel", textStyleButtons))
                    {
                        networkStatus = NetworkStatus.NONE;
                        UnityEngine.Network.Disconnect(200);
                    }
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 + 12*res, 80*res, 5*res), "Connection type status: " + UnityEngine.Network.TestConnectionNAT(), textStyleMessage);

                    if (!lan)
                    {
                        if (MasterServer.PollHostList().Length != 0 && !connectionInProgress)
                        {
                            //HostData[] hostData = MasterServer.PollHostList();
                            //for (int i = 0; i < hostData.Length; i++)
                            //{
                            //    if (hostData[i].comment == token)
                            //    {
                            //        UnityEngine.Network.Connect(hostData[i], "NsrqkrMFQyZQ5qIW");
                            //        connectionInProgress = true;
                            //        break;
                            //    }
                            //}
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
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Error", textStyleMessageCaption);
                    GUI.Label(new Rect(Screen.width / 2 - 20*res, Screen.height / 2, 40*res, 5*res), "Failed to connect to host", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 6*res, 20*res, 5*res), "OK", textStyleButtons))
                    {
                        networkStatus = NetworkStatus.NONE;
                        UnityEngine.Network.Disconnect(200);
                    }
                }
                if (networkStatus == NetworkStatus.DISCONNECTED)
                {
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Box(new Rect(10 * res, 10 * res, Screen.width - 20 * res, Screen.height - 20 * res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 4 - 15*res, 80*res, 30*res), "Error", textStyleMessageCaption);
                    GUI.Label(new Rect(Screen.width / 2 - 20*res, Screen.height / 2, 40*res, 5*res), "Disconnected from host", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 6*res, 20*res, 5*res), "OK", textStyleButtons))
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
                GUI.Label(new Rect(Screen.width / 2 - 80*res, Screen.height / 4 - 15*res, 160*res, 30*res), "Options", textStyleMessageCaption);

                GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2, 40*res, 5*res), "Resolution:", textStyleMessageRightAlign);
                if (GUI.Button(new Rect(Screen.width / 2 + res, Screen.height / 2, 5*res, 5*res), "<", textStyleButtons))
                {
                    if (res > 2)
                        res--;
                    screenWidth = res * 256;
                    screenHeight = res * 144;
                    Screen.SetResolution(screenWidth, screenHeight, isFullScreen);
                }
                GUI.Label(new Rect(Screen.width / 2 + 8*res, Screen.height / 2, 20*res, 5*res), System.Convert.ToString(screenWidth) + "x" + System.Convert.ToString(screenHeight), textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 + 30*res, Screen.height / 2, 5*res, 5*res), ">", textStyleButtons))
                {
                    if (res < 8)
                        res++;
                    screenWidth = res * 256;
                    screenHeight = res * 144;
                    Screen.SetResolution(screenWidth, screenHeight, isFullScreen);
                }
                GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2 + 6*res, 40*res, 5*res), "Sound Effects:", textStyleMessageRightAlign);
                sound = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 6 * res + (int)(2 * res / 5), 5*res, 5*res), sound, "", checkBoxStyle);
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
                GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2 + 12*res, 40*res, 5*res), "Sound Volume:", textStyleMessageRightAlign);
                soundVolString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 12*res, 8*res, 5*res), soundVolString, 3, textBoxStyle);
                float hold;
                if ((!float.TryParse(soundVolString, out hold) || hold < 0 || hold > 1) && soundVolString != "." && soundVolString != "")
                    soundVolString = System.Convert.ToString(soundVol);
                else
                    soundVol = hold;
                foreach (GameObject p in pieces)
                    p.audio.volume = soundVol;
                announcer.audio.volume = soundVol;
                GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2 + 18*res, 40*res, 5*res), "Music:", textStyleMessageRightAlign);
                music = GUI.Toggle(new Rect(Screen.width / 2 + res, Screen.height / 2 + 18 * res + (int)(2 * res / 5), 5*res, 5*res), music, "", checkBoxStyle);
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
                GUI.Label(new Rect(Screen.width / 2 - 41*res, Screen.height / 2 + 24*res, 40*res, 5*res), "Music Volume:", textStyleMessageRightAlign);
                musicVolString = GUI.TextField(new Rect(Screen.width / 2 + res, Screen.height / 2 + 24*res, 8*res, 5*res), musicVolString, 3, textBoxStyle);
                if ((!float.TryParse(musicVolString, out hold) || hold < 0 || hold > 1) && musicVolString != "." && musicVolString != "")
                    musicVolString = System.Convert.ToString(musicVol);
                else
                    musicVol = hold;
                mainCamera.audio.volume = musicVol;

                File.WriteAllText("chess.ini", "Resolution " + System.Convert.ToString(res) + "\nSoundVolume " + System.Convert.ToString(soundVol) + "\nMusicVolume " + System.Convert.ToString(musicVol));

                if (GUI.Button(new Rect(Screen.width / 2 - 10*res, Screen.height / 2 + 30*res, 20*res, 5*res), "Back", textStyleButtons))
                {
                    menuState = MenuState.MAIN;
                }

                //GUI.Label(new Rect(Screen.width / 2 - 305, Screen.height / 2 - 30, 300, 25), "Is Cerb cool:", textStyleMessageRightAlign);
                //cerbCool1 = GUI.Toggle(new Rect(Screen.width / 2 + 5, Screen.height / 2 - 30 + 2, 25, 25), cerbCool1, "", checkBoxStyle);
                //GUI.Label(new Rect(Screen.width / 2 - 305, Screen.height / 2, 300, 25), "Is Cerb very cool:", textStyleMessageRightAlign);
                //cerbCool2 = GUI.Toggle(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 2, 25, 25), cerbCool2, "", checkBoxStyle);
                //GUI.Label(new Rect(Screen.width / 2 - 305, Screen.height / 2 + 30, 300, 25), "Is Cerb super cool:", textStyleMessageRightAlign);
                //cerbCool3 = GUI.Toggle(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 30 + 2, 25, 25), cerbCool3, "", checkBoxStyle);
                //GUI.Label(new Rect(Screen.width / 2 - 305, Screen.height / 2 + 60, 300, 25), "Is Cerb ultra cool:", textStyleMessageRightAlign);
                //cerbCool4 = GUI.Toggle(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 60 + 2, 25, 25), cerbCool4, "", checkBoxStyle);
                //GUI.Label(new Rect(Screen.width / 2 - 305, Screen.height / 2 + 90, 300, 25), "Is Cerb balls-to-the-wall fucking awesome cool:", textStyleMessageRightAlign);
                //cerbCool5 = GUI.Toggle(new Rect(Screen.width / 2 + 5, Screen.height / 2 + 90 + 2, 25, 25), cerbCool5, "", checkBoxStyle);
                //cerbCount = 0;
                //if(cerbCool1) cerbCount++;
                //if(cerbCool2) cerbCount++;
                //if(cerbCool3) cerbCount++;
                //if(cerbCool4) cerbCount++;
                //if(cerbCool5) cerbCount++;
                //GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 120, 400, 25), "You currently have answered " + System.Convert.ToString(cerbCount) + " survey questions correctly", textStyleMessage);
                //if(cerbCount == 5) {
                //    GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 150, 400, 25), "You have successfully completed this survey!", textStyleMessage);
                //    if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 180, 100, 25), "OK", textStyleButtons))
                //    {
                //        menuState = MenuState.MAIN;
                //    }
                //}
            }
            if (menuState == MenuState.NONE)
            {

            }
        }*/
        
        if (gameState == GameState.PREGAME)
        {
            // setup for the game
        }

        if(gameState == GameState.RUNNING) {
            if (justAfterThreefoldRepetition)
                gameState = GameState.DRAWPOSSIBLE;
            else
            {
                if (!singlePlayer)
                {
                    GUI.Box(new Rect(Screen.width / 2 - 12*res, 4*res, 24*res, 5*res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 12*res, 4*res, 24*res, 5*res), turn.ToString() + " to play", textStyleMessage);
                }

                GUI.Box(new Rect(Screen.width - 32*res, 4*res, 24*res, 38*res), "");
                if (GUI.Button(new Rect(Screen.width - 30*res, 8*res, 20*res, 6*res), "Resign", textStyleButtons))
                {
                    gameState = GameState.GAMEOVER;
                    if (turn == PieceColor.WHITE)
                    {
                        whiteWon = false;
                        message = "White resigns.";
                    }
                    else
                    {
                        whiteWon = true;
                        message = "Black resigns.";
                    }
                }
                if (GUI.Button(new Rect(Screen.width - 30*res, 16*res, 20*res, 6*res), "Offer Draw", textStyleButtons))
                    gameState = GameState.OFFERINGDRAW;
                if (GUI.Button(new Rect(Screen.width - 30*res, 24*res, 20*res, 6*res), "Main Menu", textStyleButtons))
                    //gameState = GameState.MENU;
                    UnloadGame();
                if (GUI.Button(new Rect(Screen.width - 30*res, 32*res, 20*res, 6*res), "Quit", textStyleButtons))
                    Application.Quit();
            }
        }

        if(gameState == GameState.PROMOTION) {
            bool pieceUpdated = false;
            textColor.a = 1;
            GUI.color = textColor;
            GUI.Box(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 15*res, 80*res, 30*res), "");
            GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 + 7*res, 80*res, 4*res), "Which piece would you like to promote to?", textStyleMessage);
            if (promotedPiece.GetComponent<PieceInterface>().pieceInfo.color == PieceColor.BLACK)
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 38*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[17].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.Q;
                    moveList[moveList.Count-1] = moveList[moveList.Count-1]+"/Q";
                    promotedPiece.renderer.material = pieces[17].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[18].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.B;
                    moveList[moveList.Count-1] = moveList[moveList.Count-1]+"/B";
                    promotedPiece.renderer.material = pieces[18].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[20].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.N;
                    moveList[moveList.Count-1] = moveList[moveList.Count-1]+"/N";
                    promotedPiece.renderer.material = pieces[20].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 + 22*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[22].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.R;
                    moveList[moveList.Count-1] = moveList[moveList.Count-1]+"/R";
                    promotedPiece.renderer.material = pieces[22].renderer.material;
                    pieceUpdated = true;
                }
            }
            else
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 38*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[1].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.Q;
                    moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "/Q";
                    promotedPiece.renderer.material = pieces[1].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[2].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.B;
                    moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "/B";
                    promotedPiece.renderer.material = pieces[2].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[4].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.N;
                    moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "/N";
                    promotedPiece.renderer.material = pieces[4].renderer.material;
                    pieceUpdated = true;
                }
                if (GUI.Button(new Rect(Screen.width / 2 + 22*res, Screen.height / 2 - 11*res, 16*res, 16*res), pieces[6].renderer.material.mainTexture))
                {
                    promotedPiece.GetComponent<PieceInterface>().pieceInfo.type = PieceType.R;
                    moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "/R";
                    promotedPiece.renderer.material = pieces[6].renderer.material;
                    pieceUpdated = true;
                }
            }

            if (pieceUpdated)
            {
                check = false;
                foreach (GameObject g in pieces)
                {
                    if (g.GetComponent<PieceInterface>().pieceInfo.color != turn && pathToKing(g)) // != turn because the turn already changed to next player
                    {
                        if (checkPiece1 == null)
                        {
                            check = true;
                            checkPiece1 = g;
                            moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "+";
                        }
                        else
                        {
                            checkPiece2 = g;
                            break;
                        }
                    }
                }
                if (!check)
                {
                    checkPiece1 = null;
                    checkPiece2 = null;
                }
                if (noMoves())
                {
                    if (check)
                    {
                        checkmate = true;
                        if (turn == PieceColor.WHITE)
                            whiteWon = true;
                        moveList[moveList.Count - 1] = moveList[moveList.Count - 1] + "+";
                        textTimer = 200;
                        message = "Checkmate.";
                        announcer.audio.clip = GameObject.Find("sndCheckmate").audio.clip;
                        announcer.audio.Play();
                    }
                    else
                    {
                        stalemate = true;
                        textTimer = 200;
                        message = "Stalemate.";
                        announcer.audio.clip = GameObject.Find("sndStalemate").audio.clip;
                        announcer.audio.Play();
                    }
                    firstGameOverUpdate = true;
                }
                else
                {
                    if (check)
                    {
                        textTimer = 200;
                        message = "Check!";
                        announcer.audio.clip = GameObject.Find("sndCheck").audio.clip;
                        announcer.audio.Play();
                    }
                }

                gameState = GameState.BUSY;
            }
        }

        if (gameState == GameState.DRAWPOSSIBLE)
        {
            if (fiftyMoveCounter >= 50)
            {
                GUI.Box(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 15*res, 80*res, 30*res), "");
                GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 10*res, 80*res, 8*res), "Would you like to declare a draw via the fifty-move rule?", textStyleMessage);
                if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2 + 2*res, 16*res, 8*res), "Yes", textStyleButtons))
                {
                    gameState = GameState.GAMEOVER;
                    draw = true;
                    textTimer = 200;
                    message = "Draw.";
                    announcer.audio.clip = GameObject.Find("sndDraw").audio.clip;
                    announcer.audio.Play();
                }
                if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2 + 2*res, 16*res, 8*res), "No", textStyleButtons))
                {
                    gameState = GameState.BUSY;
                    cameraProcessing = true;
                }
            }
            else
            {
                if (moveRepetition >= 3)
                {
                    justAfterThreefoldRepetition = true;
                    GUI.Box(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 15*res, 80*res, 30*res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 10*res, 80*res, 8*res), "Would you like to declare a draw via the threefold repetition rule?", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2 + 2*res, 16*res, 8*res), "Yes", textStyleButtons))
                    {
                        gameState = GameState.GAMEOVER;
                        draw = true;
                        textTimer = 200;
                        message = "Draw.";
                        announcer.audio.clip = GameObject.Find("sndDraw").audio.clip;
                        announcer.audio.Play();
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2 + 2*res, 16*res, 8*res), "No", textStyleButtons))
                    {
                        gameState = GameState.BUSY;
                        cameraProcessing = true;
                    }
                }
                if (justAfterThreefoldRepetition)
                {
                    justAfterThreefoldRepetition = false;
                    GUI.Box(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 15*res, 80*res, 30*res), "");
                    GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 10*res, 80*res, 8*res), "Would you like to declare a draw via the threefold repetition rule?", textStyleMessage);
                    if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2 + 2*res, 16*res, 8*res), "Yes", textStyleButtons))
                    {
                        gameState = GameState.GAMEOVER;
                        draw = true;
                        textTimer = 200;
                        message = "Draw.";
                        announcer.audio.clip = GameObject.Find("sndDraw").audio.clip;
                        announcer.audio.Play();
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2 + 2*res, 16*res, 8*res), "No", textStyleButtons))
                    {
                        gameState = GameState.BUSY;
                        cameraProcessing = true;
                    }
                }
            }
        }

        if (gameState == GameState.OFFERINGDRAW)
        {
            GUI.Box(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 15*res, 80*res, 30*res), "");
            if (turn == PieceColor.WHITE)
                GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 8*res, 80*res, 4*res), "Black: do you accept the draw offer?", textStyleMessage);
            else
                GUI.Label(new Rect(Screen.width / 2 - 40*res, Screen.height / 2 - 8*res, 80*res, 4*res), "White: do you accept the draw offer?", textStyleMessage);
            if (GUI.Button(new Rect(Screen.width / 2 - 18*res, Screen.height / 2, 16*res, 8*res), "Yes", textStyleButtons))
            {
                gameState = GameState.GAMEOVER;
                draw = true;
                textTimer = 200;
                message = "Draw.";
                announcer.audio.clip = GameObject.Find("sndDraw").audio.clip;
                announcer.audio.Play();
            }
            if (GUI.Button(new Rect(Screen.width / 2 + 2*res, Screen.height / 2, 16*res, 8*res), "No", textStyleButtons))
            {
                gameState = GameState.RUNNING;
            }
        }

        if (gameState == GameState.PAUSED)
        {
            textColor.a = 1;
            GUI.color = textColor;
            GUI.Box(new Rect(Screen.width / 2 - 30*res, Screen.height / 2 - 10*res, 60*res, 20*res), "");
            GUI.Label(new Rect(Screen.width / 2 - 30*res, Screen.height / 2 - 10*res, 60*res, 20*res), "Paused", textStyleMessageCaption);
            GUI.Box(new Rect(2*res, 2*res, Screen.width - 4*res, Screen.height / 2 - 14*res), "");
            GUI.Label(new Rect(3*res, 3*res, 20*res, 4*res), "Move list:", textStyleMoveList);
            for (int i = 0; i < moveList.Count; i++)
            {
                int minx = (i / 16) * 32*res + 8*res;
                if (i % 2 == 1)
                    minx += 16*res;
                int miny = (i % 16)/2 * 5*res + 7*res;
                if(i % 2 == 0)
                    GUI.Label(new Rect(minx, miny, 15*res, 5*res), System.Convert.ToString(i/2 + 1) + ". " + moveList[i], textStyleMoveList);
                else
                    GUI.Label(new Rect(minx, miny, 15*res, 5*res), moveList[i], textStyleMoveList);
            }
        }

        if (gameState == GameState.BUSY)
        {
            if (textTimer > 0)
            {
                textColor.a = (float)textTimer / 200;

                GUI.color = textColor;
                //GUI.backgroundColor = textColor;
                GUI.Box(new Rect(Screen.width / 2 - 20*res, Screen.height / 2 - 10*res, 40*res, 20*res), "");
                GUI.Label(new Rect(Screen.width / 2 - 20*res, Screen.height / 2 - 10*res, 40*res, 20*res), message, textStyleMessageCaption);
            }
            else
            {
                textColor.a = 0;
                message = "";

            }
        }

        if (gameState == GameState.GAMEOVER)
        {
            textColor.a = 1;
            GUI.color = textColor;
            GUI.Box(new Rect(Screen.width / 2 - 60*res, Screen.height / 2 - 10*res, 120*res, 20*res), "");
            GUI.Label(new Rect(Screen.width / 2 - 60*res, Screen.height / 2 - 50, 120*res, 20*res), message, textStyleMessageCaption);
            GUI.Box(new Rect(2*res, 2*res, Screen.width - 4*res, Screen.height / 2 - 14*res), "");
            GUI.Label(new Rect(3*res, 3*res, 20*res, 4*res), "Move list:", textStyleMoveList);
            for (int i = 0; i < moveList.Count; i++)
            {
                int minx = (i / 16) * 32*res + 8*res;
                if (i % 2 == 1)
                    minx += 16*res;
                int miny = (i % 16) / 2 * 5*res + 7*res;
                if (i % 2 == 0)
                    GUI.Label(new Rect(minx, miny, 15*res, 5*res), System.Convert.ToString(i / 2 + 1) + ". " + moveList[i], textStyleMoveList);
                else
                    GUI.Label(new Rect(minx, miny, 15*res, 5*res), moveList[i], textStyleMoveList);
            }

            GUI.Box(new Rect(Screen.width / 2 - 12 * res, Screen.height / 2, 24 * res, 22 * res), "");
            if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 4 * res, 20 * res, 6 * res), "Main Menu", textStyleButtons))
                UnloadGame();
            if (GUI.Button(new Rect(Screen.width / 2 - 10 * res, Screen.height / 2 + 12 * res, 20 * res, 6 * res), "Quit", textStyleButtons))
                Application.Quit();
        }
    }

}
