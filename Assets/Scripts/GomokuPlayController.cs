using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using UnityEngine.UI;

public enum DirectionPiece
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
    LeftTop = 4,
    RightDown = 5,
    LeftDown = 6,
    RightTop = 7
}

public class GomokuPlayController : MonoBehaviour
{
    public Vector3 zeroPointPosition;
    public float cellWidth;
    public float cellHeight;
    public PieceColor pieceColor = PieceColor.Black;

    private PhotonView pv;

    private int row;
    private int column;

    public GameObject blackPiece;
    public GameObject whitePiece;

    public List<GomokuPiecesController> currentPieceList = new List<GomokuPiecesController>();

    // Start is called before the first frame update
    void Start()
    {
        pv = this.GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!pv.IsMine) return;

        // The first turn is black piece
        if (GameObject.FindObjectOfType<GomokuNetworkManager>().playerTurn != pieceColor) return;

        // After gamestate is ready, and then start game
        if (GameObject.FindObjectOfType<GomokuNetworkManager>().gameState != GameState.Ready) return;

        // Left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Calculate positions of pieces
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 offsetPos = mousePos - zeroPointPosition;
            row = (int)Mathf.Round(offsetPos.y / cellHeight);
            column = (int)Mathf.Round(offsetPos.x / cellWidth);

            int[] rowColumnValue = { row, column };

            // If click posotion is not in the chessboard
            if (row < 0 || row > 14 || column < 0 || column > 14) return;

            // Does current position have a piece
            currentPieceList = GameObject.FindObjectsOfType<GomokuPiecesController>().ToList();
            foreach (var item in currentPieceList)
            {
                if (item.row == row && item.column == column)
                    return;
            }

            Vector3 piecePos = new Vector3(column * cellWidth, row * cellHeight, zeroPointPosition.z) + zeroPointPosition;

            // Generate pieces
            GameObject newPiece;
            GomokuPiecesController currentPiece = new GomokuPiecesController();
            if (pieceColor == PieceColor.Black)
            {
                if (blackPiece != null)
                {
                    newPiece = PhotonNetwork.Instantiate(blackPiece.name, piecePos, blackPiece.transform.rotation);

                    // Data synchronization for two clients
                    newPiece.GetComponent<PhotonView>().RPC("SetRowColumnValue", RpcTarget.All, rowColumnValue);

                    currentPiece = newPiece.GetComponent<GomokuPiecesController>();
                }
            }
            else
            {
                if (whitePiece != null)
                {
                    newPiece = PhotonNetwork.Instantiate(whitePiece.name, piecePos, whitePiece.transform.rotation);

                    // Data synchronization for two clients
                    newPiece.GetComponent<PhotonView>().RPC("SetRowColumnValue", RpcTarget.All, rowColumnValue);

                    currentPiece = newPiece.GetComponent<GomokuPiecesController>();
                }
            }

            // Sound Effect
            GameObject.FindObjectOfType<GomokuNetworkManager>().PlaySoundEffect();

            // Is five same color pieces on one line
            currentPieceList = GameObject.FindObjectsOfType<GomokuPiecesController>().ToList();
            bool isFive = FivePiecesOnOneLine(currentPieceList, currentPiece);
            if (isFive)
            {
                // Game Over
                GameObject.FindObjectOfType<GomokuNetworkManager>().gameObject.GetComponent<PhotonView>().RPC("GameOver", RpcTarget.All);
            }

            // Change player turn
            GameObject.FindObjectOfType<GomokuNetworkManager>().gameObject.GetComponent<PhotonView>().RPC("ChangeTurn", RpcTarget.All);
        }
    }

    bool FivePiecesOnOneLine(List<GomokuPiecesController> currentList, GomokuPiecesController currentPiece)
    {
        bool result = false;

        List<GomokuPiecesController> currentColorList = currentList.Where(en => en.pieceColor == pieceColor).ToList();

        var upList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.Up);
        var downList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.Down);
        var leftList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.Left);
        var rightList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.Right);
        var leftTopList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.LeftTop);
        var rightDownList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.RightDown);
        var leftDownList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.LeftDown);
        var rightTopList = GetSamePieceByDirection(currentColorList, currentPiece, DirectionPiece.RightTop);

        if(upList.Count + downList.Count + 1 >= 5 ||
           leftList.Count + rightList.Count + 1 >= 5 ||
           leftTopList.Count + rightDownList.Count + 1 >= 5 ||
           leftDownList.Count + rightTopList.Count + 1 >= 5)
        {
            result = true;
        }

        return result;
    }

    List<GomokuPiecesController> GetSamePieceByDirection(List<GomokuPiecesController> currentColorList, GomokuPiecesController currentPiece, DirectionPiece direction)
    {
        List<GomokuPiecesController> result = new List<GomokuPiecesController>();

        switch(direction)
        {
            case DirectionPiece.Up:
                foreach(var item in currentColorList)
                {
                    if(item.column == currentPiece.column && item.row == currentPiece.row + 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.Up);
                        result.AddRange(resultList);
                    }
                }    
                break;
            case DirectionPiece.Down:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column && item.row == currentPiece.row - 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.Down);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.Left:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column - 1 && item.row == currentPiece.row)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.Left);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.Right:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column + 1 && item.row == currentPiece.row)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.Right);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.LeftTop:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column - 1 && item.row == currentPiece.row + 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.LeftTop);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.RightDown:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column + 1 && item.row == currentPiece.row - 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.RightDown);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.LeftDown:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column - 1 && item.row == currentPiece.row - 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.LeftDown);
                        result.AddRange(resultList);
                    }
                }
                break;
            case DirectionPiece.RightTop:
                foreach (var item in currentColorList)
                {
                    if (item.column == currentPiece.column + 1 && item.row == currentPiece.row + 1)
                    {
                        result.Add(item);
                        var resultList = GetSamePieceByDirection(currentColorList, item, DirectionPiece.RightTop);
                        result.AddRange(resultList);
                    }
                }
                break;
        }

        return result;
    }
}
