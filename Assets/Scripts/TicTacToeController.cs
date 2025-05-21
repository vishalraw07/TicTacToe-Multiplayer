using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class TicTacToeController : MonoBehaviourPunCallbacks
{
    public static TicTacToeController Instance { get; private set; }
    [SerializeField] private TMP_Text gameStatusText;
    [SerializeField] private Button[] cellButtons;
    [SerializeField] private Sprite symbolX, symbolO;
    [SerializeField] private AudioSource gameAudioSource; // For game BGM and match end sound
    [SerializeField] private AudioClip gameBGM; // Background music for game
    [SerializeField] private AudioClip matchEndSound; // Sound for match end

    private string[] gameBoard = new string[9];
    private bool isPlayerTurn;
    private string playerSymbol;
    private bool isGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (photonView == null)
        {
            Debug.LogError("TicTacToeController: PhotonView component is missing.");
        }
    }

    void Start()
    {
        ValidateSetup();
        foreach (var button in cellButtons)
        {
            if (button != null)
            {
                button.interactable = false;
                TicTacToeButton.OnButtonPressed += HandleButtonPress;
            }
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartCoroutine(SetupGame());
        }
        else
        {
            gameStatusText.text = "Awaiting opponent...";
        }

        PlayGameBGM();
    }

    void OnDestroy()
    {
        TicTacToeButton.OnButtonPressed -= HandleButtonPress;
    }

    private void ValidateSetup()
    {
        if (cellButtons == null || cellButtons.Length != 9 || cellButtons.Any(c => c == null))
        {
            Debug.LogError("TicTacToeController: Cell buttons array is not properly configured. Ensure 9 buttons are assigned.");
        }
        if (symbolX == null || symbolO == null)
        {
            Debug.LogError("TicTacToeController: Symbol sprites (X or O) are not assigned.");
        }
        if (gameStatusText == null)
        {
            Debug.LogError("TicTacToeController: Game status text is not assigned.");
        }
        if (gameAudioSource == null)
        {
            Debug.LogError("TicTacToeController: Game audio source is not assigned.");
        }
        if (gameBGM == null)
        {
            Debug.LogError("TicTacToeController: Game BGM clip is not assigned.");
        }
        if (matchEndSound == null)
        {
            Debug.LogError("TicTacToeController: Match end sound clip is not assigned.");
        }
        foreach (var button in cellButtons)
        {
            if (button != null && button.image == null)
            {
                Debug.LogError($"Button {button.name} is missing Image component.");
            }
        }
    }

    private void PlayGameBGM()
    {
        if (gameAudioSource != null && gameBGM != null)
        {
            gameAudioSource.clip = gameBGM;
            gameAudioSource.loop = true;
            gameAudioSource.Play();
            Debug.Log("Playing game background music.");
        }
    }

    private void PlayMatchEndSound()
    {
        if (gameAudioSource != null && matchEndSound != null)
        {
            gameAudioSource.Stop(); // Stop BGM
            gameAudioSource.PlayOneShot(matchEndSound);
            Debug.Log("Playing match end sound.");
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"New player joined: {newPlayer.NickName}");
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartCoroutine(SetupGame());
        }
    }

    private IEnumerator SetupGame()
    {
        while (SceneManager.GetActiveScene().name != "Game" || !SceneManager.GetActiveScene().isLoaded)
        {
            yield return null;
        }
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("BeginGame", RpcTarget.All);
        }
    }

    [PunRPC]
    private void BeginGame()
    {
        SetupBoard();
    }

    private void SetupBoard()
    {
        gameBoard = new string[9];
        Debug.Log($"Game initialized. Board: [{string.Join(",", gameBoard.Select(s => s ?? "empty"))}]");
        Debug.Log($"Player: {PhotonNetwork.NickName}, Master: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient)
        {
            playerSymbol = "X";
            isPlayerTurn = true;
            photonView.RPC("AssignOpponentSymbol", RpcTarget.Others, "O");
        }

        foreach (var button in cellButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
        UpdateGameStatus();
    }

    [PunRPC]
    private void AssignOpponentSymbol(string symbol)
    {
        playerSymbol = symbol;
        isPlayerTurn = false;
        Debug.Log($"Opponent ({PhotonNetwork.NickName}) assigned: Symbol={playerSymbol}, Turn={isPlayerTurn}");
        UpdateGameStatus();
    }

    private void HandleButtonPress(int index)
    {
        if (!isPlayerTurn || isGameOver || !string.IsNullOrEmpty(gameBoard[index]))
        {
            Debug.LogWarning($"Move rejected: Turn={isPlayerTurn}, GameOver={isGameOver}, Cell[{index}]={(gameBoard[index] ?? "empty")}");
            return;
        }

        gameBoard[index] = playerSymbol;
        UpdateButtonUI(index);
        photonView.RPC("BroadcastMove", RpcTarget.All, index, playerSymbol);
        EvaluateGameState();
    }

    private void UpdateButtonUI(int index)
    {
        if (cellButtons[index] == null) return;
        var button = cellButtons[index];
        button.image.sprite = playerSymbol == "X" ? symbolX : symbolO;
        button.interactable = false;
    }

    [PunRPC]
    private void BroadcastMove(int index, string symbol)
    {
        gameBoard[index] = symbol;
        if (cellButtons[index] != null)
        {
            var button = cellButtons[index];
            button.image.sprite = symbol == "X" ? symbolX : symbolO;
            button.interactable = false;
        }
        isPlayerTurn = !isPlayerTurn;
        Debug.Log($"BroadcastMove: Index={index}, Symbol={symbol}, Turn={isPlayerTurn}");
        UpdateGameStatus();
        EvaluateGameState();
    }

    private void EvaluateGameState()
    {
        string winner = FindWinner();
        bool isBoardFull = gameBoard.All(cell => !string.IsNullOrEmpty(cell));

        if (winner != null)
        {
            isGameOver = true;
            photonView.RPC("EndGame", RpcTarget.All, winner);
        }
        else if (isBoardFull)
        {
            isGameOver = true;
            photonView.RPC("EndGame", RpcTarget.All, "Draw");
        }
    }

    private string FindWinner()
    {
        int[,] winningLines = new int[,]
        {
            {0,1,2}, {3,4,5}, {6,7,8}, // Rows
            {0,3,6}, {1,4,7}, {2,5,8}, // Columns
            {0,4,8}, {2,4,6}           // Diagonals
        };

        for (int i = 0; i < winningLines.GetLength(0); i++)
        {
            int a = winningLines[i, 0];
            int b = winningLines[i, 1];
            int c = winningLines[i, 2];
            if (!string.IsNullOrEmpty(gameBoard[a]) && gameBoard[a] == gameBoard[b] && gameBoard[b] == gameBoard[c])
            {
                return gameBoard[a];
            }
        }
        return null;
    }

    [PunRPC]
    private void EndGame(string result)
    {
        isGameOver = true;
        gameStatusText.text = result == "Draw" ? "Game is a Draw!" : $"Player {result} Wins!";
        foreach (var button in cellButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
        PlayMatchEndSound();
        Debug.Log($"Game Ended: Result={result}");
    }

    private void UpdateGameStatus()
    {
        gameStatusText.text = isGameOver ? gameStatusText.text : (isPlayerTurn ? "Your Move" : "Opponent's Move");
    }
}