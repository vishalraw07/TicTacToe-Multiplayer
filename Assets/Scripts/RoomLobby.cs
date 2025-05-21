using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RoomLobby : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private TMP_Text lobbyStatusText;
    [SerializeField] private AudioSource lobbyAudioSource; // For lobby BGM
    [SerializeField] private AudioSource clickAudioSource; // For button clicks
    [SerializeField] private AudioClip lobbyBGM; // Background music for lobby
    [SerializeField] private AudioClip clickSound; // Click sound for buttons

    private bool isConnectingToServer;
    private Dictionary<string, GameObject> roomItems = new Dictionary<string, GameObject>();

    void Start()
    {
        ValidateComponents();
        InitializeUI();
        ConnectToPhoton();
        PlayLobbyBGM();
    }

    private void ValidateComponents()
    {
        if (roomItemPrefab == null) Debug.LogError("RoomLobby: Room item prefab is not assigned.");
        if (roomListContainer == null) Debug.LogError("RoomLobby: Room list container is not assigned.");
        if (roomNameInput == null) Debug.LogError("RoomLobby: Room name input is not assigned.");
        if (createRoomButton == null) Debug.LogError("RoomLobby: Create room button is not assigned.");
        if (lobbyStatusText == null) Debug.LogError("RoomLobby: Lobby status text is not assigned.");
        if (lobbyAudioSource == null) Debug.LogError("RoomLobby: Lobby audio source is not assigned.");
        if (clickAudioSource == null) Debug.LogError("RoomLobby: Click audio source is not assigned.");
        if (lobbyBGM == null) Debug.LogError("RoomLobby: Lobby BGM clip is not assigned.");
        if (clickSound == null) Debug.LogError("RoomLobby: Click sound clip is not assigned.");
    }

    private void InitializeUI()
    {
        createRoomButton.interactable = false;
        createRoomButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            CreateNewRoom();
        });
    }

    private void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            isConnectingToServer = true;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon server...");
        }
        else
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("Already connected, joining lobby.");
        }
    }

    private void PlayLobbyBGM()
    {
        if (lobbyAudioSource != null && lobbyBGM != null)
        {
            lobbyAudioSource.clip = lobbyBGM;
            lobbyAudioSource.loop = true;
            lobbyAudioSource.Play();
            Debug.Log("Playing lobby background music.");
        }
    }

    private void PlayClickSound()
    {
        if (clickAudioSource != null && clickSound != null)
        {
            clickAudioSource.PlayOneShot(clickSound);
            Debug.Log("Playing button click sound.");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon server.");
        if (isConnectingToServer)
        {
            PhotonNetwork.JoinLobby();
            isConnectingToServer = false;
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Entered lobby.");
        createRoomButton.interactable = true;
        lobbyStatusText.text = "In Lobby";
        ClearRoomList();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Room list updated: {roomList.Count} rooms.");
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                RemoveRoomItem(room.Name);
            }
            else if (room.PlayerCount < 2 && room.IsOpen && room.IsVisible)
            {
                AddRoomItem(room.Name);
            }
        }
    }

    private void AddRoomItem(string roomName)
    {
        if (roomItems.ContainsKey(roomName)) return;

        GameObject item = Instantiate(roomItemPrefab, roomListContainer);
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();
        if (text == null)
        {
            Debug.LogError($"Room item prefab '{roomItemPrefab.name}' lacks TMP_Text.");
            Destroy(item);
            return;
        }
        text.text = roomName;

        Button button = item.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"Room item prefab '{roomItemPrefab.name}' lacks Button.");
            Destroy(item);
            return;
        }
        button.onClick.AddListener(() =>
        {
            PlayClickSound();
            JoinRoomByName(roomName);
        });
        roomItems[roomName] = item;
    }

    private void RemoveRoomItem(string roomName)
    {
        if (roomItems.ContainsKey(roomName))
        {
            Destroy(roomItems[roomName]);
            roomItems.Remove(roomName);
        }
    }

    private void CreateNewRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            Debug.LogWarning("Cannot create room: Not in lobby or not connected.");
            return;
        }

        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? $"Room_{Random.Range(1000, 9999)}" : roomNameInput.text;
        RoomOptions options = new RoomOptions { MaxPlayers = 2, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log($"Creating room: {roomName}");
        DisableLobby();
        lobbyStatusText.text = $"Creating room {roomName}...";
    }

    private void JoinRoomByName(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            Debug.LogWarning("Cannot join room: Not in lobby or not connected.");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"Joining room: {roomName}");
        DisableLobby();
        lobbyStatusText.text = $"Joining room {roomName}...";
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        if (lobbyAudioSource != null)
        {
            lobbyAudioSource.Stop();
            Debug.Log("Stopped lobby background music.");
        }
        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnJoinRoomFailed(short code, string message)
    {
        Debug.LogError($"Failed to join room: {message} (Code: {code})");
        EnableLobby();
        lobbyStatusText.text = $"Join failed: {message}";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected: {cause}");
        createRoomButton.interactable = false;
        isConnectingToServer = false;
        ClearRoomList();
        lobbyStatusText.text = "Disconnected. Reconnecting...";
        if (lobbyAudioSource != null)
        {
            lobbyAudioSource.Stop();
            Debug.Log("Stopped lobby background music on disconnect.");
        }
    }

    private void DisableLobby()
    {
        roomNameInput.interactable = false;
        createRoomButton.interactable = false;
        roomListContainer.gameObject.SetActive(false);
    }

    private void EnableLobby()
    {
        roomNameInput.interactable = true;
        createRoomButton.interactable = true;
        roomListContainer.gameObject.SetActive(true);
        lobbyStatusText.text = "In Lobby";
        PlayLobbyBGM();
    }

    private void ClearRoomList()
    {
        foreach (var item in roomItems)
        {
            Destroy(item.Value);
        }
        roomItems.Clear();
    }
}