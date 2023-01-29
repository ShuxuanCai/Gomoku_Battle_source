using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public enum GameState
{
    Ready = 1,
    GameOver = 3
}

public class GomokuNetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject player;
    public PieceColor playerTurn = PieceColor.Black;
    public GameState gameState = GameState.Ready;
    public Text GameOverText;
    public Text WinText;
    public AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        // Connect Server
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // Create or join room
        base.OnConnectedToMaster();
        print("OnConnectedToMaster Successfully!");

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        PhotonNetwork.JoinOrCreateRoom("GomokuRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        // Create online player
        base.OnJoinedRoom();
        print("OnJoinedRoom Successfully!");

        if (player == null) return;
        GameObject newPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, player.transform.rotation);

        // Instantiate attributes of player
        if(PhotonNetwork.IsMasterClient)
        {
            newPlayer.GetComponent<GomokuPlayController>().pieceColor = PieceColor.Black;
        }
        else
        {
            newPlayer.GetComponent<GomokuPlayController>().pieceColor = PieceColor.White;
        }
    }

    [PunRPC]
    public void ChangeTurn()
    {
        if (playerTurn == PieceColor.Black)
            playerTurn = PieceColor.White;
        else
            playerTurn = PieceColor.Black;
    }

    [PunRPC]
    public void GameOver()
    {
        if(GameOverText)
            GameOverText.gameObject.SetActive(true);
        else if(WinText)
            WinText.gameObject.SetActive(true);
        gameState = GameState.GameOver;
    }

    public void PlaySoundEffect()
    {
        if (audioSource == null) return;
        audioSource.Play();
    }
}
