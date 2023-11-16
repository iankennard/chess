//for selected piece
//	for each normal move for that piece
//		move the piece there temporarily, storing the piece already on that square, if necessary
//		check whether the king is in check by tracing all opponent pieces with the current temp setup
//		if not, mark square as legal

/*

public class stopit
{
    public void CreateHighlights()
    {
        foreach (Piece p in pieces)
        {
            foreach (Square m in p.Moves())
            {
                Piece storage = pieces[m];
                pieces[m] = p;
                bool legal = true;
                foreach (Piece a in pieces)
                {
                    if (a.CanReach(p.king.cur))
                    {
                        legal = false;
                        break;
                    }
                }
                if (legal)
                    addHighlight(m);
            }
        }
    }
}

public class Piece
{
    public Square cur;
    public Piece king;

    public List<Square> Moves()
    {
        List<Square> list = new List<Square>();
        foreach (Square m in basicMoves(type))
        {
            if(onBoard(m+cur) && p.CanReach(m+cur))
                list.Add(m);
        }
        return list;
    }

    public bool CanReach(Square s)
    {
        foreach (Square m in basicMoves(type))
        {
            if (s == m + cur && PathTo(s))
                return true;
        }
        return false;
    }

    public bool PathTo(Square s)
    {
        if (type == N) return true;

    }
}

*/