using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Menu : MonoBehaviourPunCallbacks, ILobbyCallbacks
{
    [Header("Screens")]
    public GameObject mainScreen;
    public GameObject createRoomScreen;
    public GameObject lobbyScreen;
    public GameObject lobbyBrowserScreen;

    [Header("Main Screen")]
    public Button createRoomButton;
    public Button findRoomButton;

    [Header("Lobby")]
    public TextMeshProUGUI playerListText;      // text displaying all players in the lobby
    public TextMeshProUGUI roomInfoText;        // text displaying the room info
    public Button startGameButton;              // button to start the game

    [Header("Lobby Browser")]
    public RectTransform roomListContainer;     // container which holds the room buttons
    public GameObject roomButtonPrefab;

    private List<GameObject> roomButtons = new List<GameObject>();
    private List<RoomInfo> roomList = new List<RoomInfo>();

    void Start()
    {
        // disable the menu buttons at the start to prevent network errors
        createRoomButton.interactable = false;
        findRoomButton.interactable = false;

        // enable the cursor since we hide it when we play the game
        Cursor.lockState = CursorLockMode.None;

        // are we in a game? have we just finished a game in the Game scene?
        if(PhotonNetwork.InRoom)
        {
            // go to the lobby
            SetScreen(lobbyScreen);
            UpdateLobbyUI();

            // make the room visible again
            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.CurrentRoom.IsOpen = true;
        }
    }

    // changes the currently visible screen
    public void SetScreen(GameObject screen)
    {
        // deactivate all the screens
        mainScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        lobbyBrowserScreen.SetActive(false);

        // activate the requested one
        screen.SetActive(true);

        // if the screen is the lobby browser - update the UI
        if(screen == lobbyBrowserScreen)
            UpdateLobbyBrowserUI();
    }

    // called when the "Back" button has been pressed
    // located on the Create Room and Lobby Browser screens
    public void OnBackButton()
    {
        SetScreen(mainScreen);
    }

    // MAIN SCREEN

    // called when the player name input field has been changed
    public void OnPlayerNameValueChanged(TMP_InputField playerNameInput)
    {
        PhotonNetwork.NickName = playerNameInput.text;
    }

    // called when we connect to the master server
    public override void OnConnectedToMaster()
    {
        // enable the menu buttons once we connect to the server
        createRoomButton.interactable = true;
        findRoomButton.interactable = true;
    }

    // called when the "Create Room" button has been pressed
    public void OnCreateRoomButton()
    {
        SetScreen(createRoomScreen);
    }

    // called when the "Find Room" button has been pressed
    public void OnFindRoomButton()
    {
        SetScreen(lobbyBrowserScreen);
    }

    // CREATE ROOM SCREEN

    // called when the "Create" button is pressed
    public void OnCreateButton(TMP_InputField roomNameInput)
    {
        NetworkManager.instance.CreateRoom(roomNameInput.text);
    }

    // LOBBY SCREEN

    // called when we join a room
    // set the screen to be the Lobby and update the UI for all players
    public override void OnJoinedRoom()
    {
        SetScreen(lobbyScreen);
        photonView.RPC("UpdateLobbyUI", RpcTarget.All);
    }

    // called when a player leaves the room - update the lobby UI
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();
    }

    // updates the lobby player list and active buttons
    [PunRPC]
    void UpdateLobbyUI()
    {
        // enable or disable start game button depending on who's the host
        startGameButton.interactable = PhotonNetwork.IsMasterClient;

        // display all players
        playerListText.text = "";

        foreach(Player player in PhotonNetwork.PlayerList)
            playerListText.text += player.NickName + "\n";

        // set the room info text
        roomInfoText.text = "<b>Lobi Adı</b>\n" + PhotonNetwork.CurrentRoom.Name;
    }

    // called when the "Start Game" button has been pressed
    public void OnStartGameButton()
    {
        // hide the room so no one can join anymore
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // tell everyone to load into the Game scene
        NetworkManager.instance.photonView.RPC("ChangeScene", RpcTarget.All, "Game");
    }

    // called when the "Leave Lobby" button has been pressed
    public void OnLeaveLobbyButton()
    {
        PhotonNetwork.LeaveRoom();
        SetScreen(mainScreen);
    }

    // LOBBY BROWSER SCREEN

    // creates a new room button object and returns it
    GameObject CreateRoomButton()
    {
        GameObject buttonObj = Instantiate(roomButtonPrefab, roomListContainer.transform);
        roomButtons.Add(buttonObj);

        return buttonObj;
    }

    // displays all of the rooms in the lobby
    void UpdateLobbyBrowserUI()
    {
        // disable all current room buttons
        foreach(GameObject button in roomButtons)
            button.SetActive(false);

        // display all current rooms in the master server
        for(int x = 0; x < roomList.Count; ++x)
        {
            // get or create the button object
            GameObject button = x >= roomButtons.Count ? CreateRoomButton() : roomButtons[x];

            button.SetActive(true);

            // set the room name and player count texts
            button.transform.Find("RoomNameText").GetComponent<TextMeshProUGUI>().text = roomList[x].Name;
            button.transform.Find("PlayerCountText").GetComponent<TextMeshProUGUI>().text = roomList[x].PlayerCount + " / " + roomList[x].MaxPlayers;

            // set the button OnClick event
            Button butComp = button.GetComponent<Button>();

            string roomName = roomList[x].Name;

            butComp.onClick.RemoveAllListeners();
            butComp.onClick.AddListener(() => { OnJoinRoomButton(roomName); });
        }

        // resize the room list container
        float bottom = roomButtonPrefab.GetComponent<RectTransform>().sizeDelta.y * PhotonNetwork.PlayerList.Length + PhotonNetwork.PlayerList.Length * 5;
        roomListContainer.offsetMin = new Vector2(roomListContainer.offsetMin.x, bottom);
    }

    // called when a lobby browser room button has been pressed
    public void OnJoinRoomButton(string roomName)
    {
        NetworkManager.instance.JoinRoom(roomName);
    }

    // called when the "Refresh" button has been pressed
    public void OnRefreshButton()
    {
        UpdateLobbyBrowserUI();
    }

    // called when the list of rooms is updated - we want to cache it
    public override void OnRoomListUpdate(List<RoomInfo> allRooms)
    {
        roomList = allRooms;
    }
}