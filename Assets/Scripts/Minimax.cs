using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minimax : MonoBehaviour
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

    //The algorithm calculates "fake moves"
    void DoFakeMove(TileData currentTile, TileData targetTile)
    {
        targetTile.SwapFakePieces(currentTile.CurrentPiece);
        currentTile.CurrentPiece = null;
    }

    //Undoes the fake moves by moving back up the tree
    void UndoFakeMove()
    {
        MoveData tempMove = moveStack.Pop();
        TileData movedTo = tempMove.secondPosition;
        TileData movedFrom = tempMove.firstPosition;
        ChessPiece pieceKilled = tempMove.pieceKilled;
        ChessPiece pieceMoved = tempMove.pieceMoved;

        movedFrom.CurrentPiece = movedTo.CurrentPiece;
        movedTo.CurrentPiece = (pieceKilled != null) ? pieceKilled : null;
    }

    //Evaluation function that subtracts the computer's score from the player's
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

    //called from gameManager
    public MoveData GetMove()
    {
        board = BoardManager.Instance;
        gameManager = GameManager.Instance;
        bestMove = CreateMove(board.GetTileFromBoard(new Vector2(0, 0)), board.GetTileFromBoard(new Vector2(0, 0))); //chooses the best possible move and default start at 0,0

        maxDepth = 3; //max branch reach - higher will make the game run slower
        CalculateMinMax(maxDepth, true);

        return bestMove;
    }

    //recursive function
    //uses maxDepth for a limit and bool for minimising and maximising move score
    int CalculateMinMax(int depth, bool max)
    {
        GetBoardState();

        if (depth == 0) //if at an end node
        {
            return Evaluate(); //move score
        }

        if (max) //if maximising (true)
        {
            int maxScore = int.MinValue; //sets this value as the lowest for default
            List<MoveData> allMoves = GetMoves(gameManager.playerTurn); //all possible moves for the player
            allMoves = Shuffle(allMoves);

            foreach (MoveData move in allMoves)
            {
                moveStack.Push(move);//go backwards through nodes

                DoFakeMove(move.firstPosition, move.secondPosition); //does a fake move
                int score = CalculateMinMax(depth - 1, false); //call method again for the next level down the tree and swap the bool
                UndoFakeMove(); 

                if(score > maxScore) //compares and overwrites
                {
                    maxScore = score;
                }

                if(score > bestMove.score && depth == maxDepth)
                {
                    move.score = score;
                    bestMove = move; //chooses the best move
                }
            }
            return maxScore;
        }
        else
        {
            PlayerTeam opponent = gameManager.playerTurn == PlayerTeam.WHITE ? PlayerTeam.BLACK : PlayerTeam.WHITE;
            int minScore = int.MaxValue;
            List<MoveData> allMoves = GetMoves(opponent);
            allMoves = Shuffle(allMoves);

            foreach (MoveData move in allMoves)
            {
                moveStack.Push(move);

                DoFakeMove(move.firstPosition, move.secondPosition);
                int score = CalculateMinMax(depth - 1, true);
                UndoFakeMove();

                if(score < minScore)
                {
                    minScore = score;
                }             
            }
            return minScore;
        }
    }

    //Randomises the list of anything (T type is placeholder)
    public List<T> Shuffle<T>(List<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = Random.Range(0,n);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
        return list;
    }

}
