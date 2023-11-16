using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PID { WK, WQ, WB1, WB2, WN1, WN2, WR1, WR2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, BK, BQ, BB1, BB2, BN1, BN2, BR1, BR2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, NONE }
public enum PieceType { K, Q, B, N, R, P, NONE }
public enum PieceColor { WHITE, BLACK, NONE }

public struct PieceInfo {

    public PID id;
    public PieceType type;
    public PieceColor color;
    public int x;
    public int y;
    public bool alive;
    public bool hasMoved;
    public bool enPassantable;

    public PieceInfo(PID i, PieceType t, PieceColor c, int px, int py) {
        id = i;
        type = t;
        color = c;
        x = px;
        y = py;
        alive = true;
        hasMoved = false;
        enPassantable = false;
    }

    public static bool operator ==(PieceInfo lhs, PieceInfo rhs)
    {
        if (lhs.type != rhs.type)
            return false;
        if (lhs.color != rhs.color)
            return false;
        if (lhs.x != rhs.x)
            return false;
        if (lhs.y != rhs.y)
            return false;
        if (lhs.alive != rhs.alive)
            return false;
        if (lhs.hasMoved != rhs.hasMoved)
            return false;
        if (lhs.enPassantable != rhs.enPassantable)
            return false;
        return true;
    }

    public static bool operator !=(PieceInfo lhs, PieceInfo rhs)
    {
        return !(lhs == rhs);
    }

    public bool Equals(PieceInfo pi)
    {
        return this == pi;
    }

    public override bool Equals(object o)
    {
        if (!(o is PieceInfo))
            throw new System.ArgumentException("Argument is not of type PieceInfo");
        return Equals((PieceInfo)o);
    }

    public override int GetHashCode()
    {
        return System.Convert.ToInt32(type) + System.Convert.ToInt32(color) * 16 + System.Convert.ToInt32(alive) * 32 + System.Convert.ToInt32(hasMoved) * 64 + System.Convert.ToInt32(enPassantable) * 128 + (x ^ y) * 256;
    }

}

public class Pieces : MonoBehaviour {

    public GameObject[] pieces;

	public void Awake() {
        pieces = new GameObject[32];
        pieces[0] = transform.Find("whiteking").gameObject; pieces[0].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WK, PieceType.K, PieceColor.WHITE, 4, 0);
        pieces[1] = transform.Find("whitequeen").gameObject; pieces[1].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WQ, PieceType.Q, PieceColor.WHITE, 3, 0);
        pieces[2] = transform.Find("whitebishop1").gameObject; pieces[2].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WB1, PieceType.B, PieceColor.WHITE, 2, 0);
        pieces[3] = transform.Find("whitebishop2").gameObject; pieces[3].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WB2, PieceType.B, PieceColor.WHITE, 5, 0);
        pieces[4] = transform.Find("whiteknight1").gameObject; pieces[4].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WN1, PieceType.N, PieceColor.WHITE, 1, 0);
        pieces[5] = transform.Find("whiteknight2").gameObject; pieces[5].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WN2, PieceType.N, PieceColor.WHITE, 6, 0);
        pieces[6] = transform.Find("whiterook1").gameObject; pieces[6].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WR1, PieceType.R, PieceColor.WHITE, 0, 0);
        pieces[7] = transform.Find("whiterook2").gameObject; pieces[7].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WR2, PieceType.R, PieceColor.WHITE, 7, 0);
        pieces[8] = transform.Find("whitepawn1").gameObject; pieces[8].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP1, PieceType.P, PieceColor.WHITE, 0, 1);
        pieces[9] = transform.Find("whitepawn2").gameObject; pieces[9].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP2, PieceType.P, PieceColor.WHITE, 1, 1);
        pieces[10] = transform.Find("whitepawn3").gameObject; pieces[10].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP3, PieceType.P, PieceColor.WHITE, 2, 1);
        pieces[11] = transform.Find("whitepawn4").gameObject; pieces[11].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP4, PieceType.P, PieceColor.WHITE, 3, 1);
        pieces[12] = transform.Find("whitepawn5").gameObject; pieces[12].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP5, PieceType.P, PieceColor.WHITE, 4, 1);
        pieces[13] = transform.Find("whitepawn6").gameObject; pieces[13].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP6, PieceType.P, PieceColor.WHITE, 5, 1);
        pieces[14] = transform.Find("whitepawn7").gameObject; pieces[14].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP7, PieceType.P, PieceColor.WHITE, 6, 1);
        pieces[15] = transform.Find("whitepawn8").gameObject; pieces[15].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.WP8, PieceType.P, PieceColor.WHITE, 7, 1);
        pieces[16] = transform.Find("blackking").gameObject; pieces[16].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BK, PieceType.K, PieceColor.BLACK, 4, 7);
        pieces[17] = transform.Find("blackqueen").gameObject; pieces[17].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BQ, PieceType.Q, PieceColor.BLACK, 3, 7);
        pieces[18] = transform.Find("blackbishop1").gameObject; pieces[18].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BB1, PieceType.B, PieceColor.BLACK, 2, 7);
        pieces[19] = transform.Find("blackbishop2").gameObject; pieces[19].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BB2, PieceType.B, PieceColor.BLACK, 5, 7);
        pieces[20] = transform.Find("blackknight1").gameObject; pieces[20].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BN1, PieceType.N, PieceColor.BLACK, 1, 7);
        pieces[21] = transform.Find("blackknight2").gameObject; pieces[21].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BN2, PieceType.N, PieceColor.BLACK, 6, 7);
        pieces[22] = transform.Find("blackrook1").gameObject; pieces[22].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BR1, PieceType.R, PieceColor.BLACK, 0, 7);
        pieces[23] = transform.Find("blackrook2").gameObject; pieces[23].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BR2, PieceType.R, PieceColor.BLACK, 7, 7);
        pieces[24] = transform.Find("blackpawn1").gameObject; pieces[24].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP1, PieceType.P, PieceColor.BLACK, 0, 6);
        pieces[25] = transform.Find("blackpawn2").gameObject; pieces[25].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP2, PieceType.P, PieceColor.BLACK, 1, 6);
        pieces[26] = transform.Find("blackpawn3").gameObject; pieces[26].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP3, PieceType.P, PieceColor.BLACK, 2, 6);
        pieces[27] = transform.Find("blackpawn4").gameObject; pieces[27].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP4, PieceType.P, PieceColor.BLACK, 3, 6);
        pieces[28] = transform.Find("blackpawn5").gameObject; pieces[28].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP5, PieceType.P, PieceColor.BLACK, 4, 6);
        pieces[29] = transform.Find("blackpawn6").gameObject; pieces[29].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP6, PieceType.P, PieceColor.BLACK, 5, 6);
        pieces[30] = transform.Find("blackpawn7").gameObject; pieces[30].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP7, PieceType.P, PieceColor.BLACK, 6, 6);
        pieces[31] = transform.Find("blackpawn8").gameObject; pieces[31].GetComponent<PieceInterface>().pieceInfo = new PieceInfo(PID.BP8, PieceType.P, PieceColor.BLACK, 7, 6);
    }

    public void Start()
    {
    }

    public void Update() {

    }

    public GameObject[] getPieces()
    {
        return pieces;
    }

}
