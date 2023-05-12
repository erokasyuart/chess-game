using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minmax : MonoBehaviour
{
    BoardManager board;
    GameManager gameManager;
    MoveData bestMove; //each better move than the last will overwrite
    int myScore = 0; //evaluation function
    int opponentScore = 0;
    int maxDepth; //tells recursive function when to stop

    List<TileData> myPieces = new List<TileData>();
    List<TileData> opponentPieces = new List<TileData>();
    Stack<MoveData> moveStack = new Stack<MoveData>(); //last in first out(LIFO) for ordered nodes from when they were added
    MoveHeuristic weight = new MoveHeuristic(); //piece weightings

    public static Minimax instance;
    public static Minimax Instance
    {
        get { return instance; }
    }


    private void Awake()
    {
        if (instance == null)
        {   
            instance = this;
        }        
        else if (instance != this)
        {
            Destroy(this);
        }      
    }

    //Hypothetical move
    MoveData CreateMove(TileData from, TileData to)
    {
        MoveData tempMove = new MoveData
        {
            firstPosition = from, //where the piece is coming from
            pieceMoved = from.CurrentPiece, //where the piece is going to
            secondPosition = to
        };

        if (to.CurrentPiece != null)
        {
            tempMove.pieceKilled = to.CurrentPiece;
        }
        return tempMove;
    }

    //iterates through all available player pieces and gets a list of movedata
    //for all legal moves
    List<MoveData> GetMoves(PlayerTeam team)
    {
        List<MoveData> turnMove = new List<MoveData>();
        List<TileData> pieces = (team == gameManager.playerTurn) ? myPieces : opponentPieces;

        foreach(TileData tile in pieces)
        {
            MoveFunction movement = new MoveFunction(board);
            List<MoveData> pieceMoves = movement.GetMoves(tile.CurrentPiece, tile.Position);

            foreach(MoveData move in pieceMoves)
            {
                MoveData newMove = CreateMove(move.firstPosition, move.secondPosition);
                turnMove.Add(newMove);
            }
        }
        return turnMove;
    }

    void DoFakeMove(TileData currentTile, TileData targetTile)
    {
        targetTile.SwapFakePieces(currentTile.CurrentPiece);
        currentTile.currentPiece = null;
    }

    //Undoes the fake moves by moving back up the tree
    void UndoFakeMove()
    {
        MoveData tempMove = moveStack.Pop();
        TileData movedTo = tempMove.secondPosition;
        TileData movedFrom = tempMove.firstPosition;
        ChessPiece pieceKilled = tempMove.pieceKilled;
        ChessPiece pieceMoved = tempMove.pieceMoved;

        movedFrom.currentPiece = movedTo.currentPiece;
        movedTo.currentPiece = (pieceKilled != null) ? pieceKilled : null;
    }

    //Evaluation function
    int Evaluate()
    {
        int pieceDifference = myScore - opponentScore;
        return pieceDifference;
    }

    //assigns appropriate board pieces to team
    void GetBoardState()
    {
        myPieces.Clear();
        opponentPieces.Clear();
        myScore = 0;
        opponentScore = 0;

        for (int y = 0; y < 8; y++) 
        {       
            for (int x = 0; x < 8; x++)
            {
                TileData tile = board.GetTileFromBoard(new Vector2(x, y));
                if(tile.CurrentPiece != null && tile.CurrentPiece.Type != ChessPiece.PieceType.NONE)
                {
                    if (tile.CurrentPiece.Team == gameManager.playerTurn)
                    {
                        myScore += weight.GetPieceWeight(tile.CurrentPiece.Type);
                        myPieces.Add(tile);
                    }
                    else
                    {
                        opponentScore += weight.GetPieceWeight(tile.CurrentPiece.Type);
                        opponentPieces.Add(tile);
                    }
                }
            }     
        }
    }
}
