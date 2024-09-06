using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class TitleSceneDirector : MonoBehaviourPunCallbacks
{
    [Header("PanelSelectMode")]
    public GameObject selectModePanel;

    [Header("PanelLogin")]
    public GameObject loginPanel;
    public InputField pNameInputField;
    public Button loginBtn;

    [Header("PanelLobby")]
    public GameObject lobbyPanel;

    [Header("PanelCreateRoom")]
    public GameObject createPanel;
    public InputField rNameInputField;
    public Button createBtn;

    [Header("Room List UI Panel")]
    public GameObject roomPrefab;
    public GameObject parentObj;

    [Header("PanelJoiningRoom")]
    public GameObject joiningPanel;

    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListGameObjects;
    private Dictionary<int, GameObject> playerListGameObjects;

    // Start is called before the first frame update
    void Start()
    {
        ActivatePanel(selectModePanel.name);
        cachedRoomList = new Dictionary<string, RoomInfo>(); //Keyをstring型、ValueをRoomInfo型の配列を宣言
        roomListGameObjects = new Dictionary<string, GameObject>(); //Keyをstring型、ValueをGameObject型の配列を宣言
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SinglePlay()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void MultiPlay()
    {
        ActivatePanel(loginPanel.name);
    }

    public void PlayerNameInputValueChanged() //PlayerNameの入力で呼ぶ関数
    {
        loginBtn.interactable = IsValidPName(); //bool関数から判定を受ける、falseならButtonが半透明になって押せない
    }

    private bool IsValidPName()
    {
        //名前は3文字以上12文字以下である条件にしている
        return !string.IsNullOrWhiteSpace(pNameInputField.text)
            && 3 <= pNameInputField.text.Length
            && pNameInputField.text.Length <= 12;
    }

    public void RoomNameInputValueChanged() //同じくRoomNameの入力で呼ぶ関数
    {
        createBtn.interactable = IsValidRName();
    }

    private bool IsValidRName()
    {
        //名前は3文字以上12文字以下である条件にしている
        return !string.IsNullOrWhiteSpace(rNameInputField.text)
            && 3 <= pNameInputField.text.Length
            && pNameInputField.text.Length <= 12;
    }

    public void LoginClicked()
    {
        string playerName = pNameInputField.text;
        if (!string.IsNullOrEmpty(playerName)) //名前がしっかり入力されていれば
        {
            PhotonNetwork.LocalPlayer.NickName = playerName; //Photon上の名前を設定
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void CreateRoomClicked() //LobbyPanelでのCreateRoomButton
    {
        ActivatePanel(createPanel.name);
    }

    public void CreateEnterClicked() //CreatePanelでのCreateEnter
    {
        string roomName = rNameInputField.text;

        if (string.IsNullOrEmpty(roomName)) //名前がnullの場合
        {
            roomName = "Room " + Random.Range(1000, 10000); //自動で名前をつける
        }

        RoomOptions roomOptions = new RoomOptions(); //RoomOptionsクラスの宣言
        roomOptions.MaxPlayers = 2; //ルーム定員2名に設定

        PhotonNetwork.CreateRoom(roomName, roomOptions); //PhotonNetworkクラスにルーム作成を送信
    }

    public void LoginCancelClicked() //LoginPanelでのCancelButton
    {
        ActivatePanel(selectModePanel.name);
    }

    public void CreateCancelClicked() //LobbyPanelでのCancelButton
    {
        ActivatePanel(lobbyPanel.name);
    }

    public void LogoutClicked()
    {
        PhotonNetwork.Disconnect();
        ActivatePanel(loginPanel.name);
    }

    public void JoinRandomRoomClicked()
    {
        ActivatePanel(joiningPanel.name);
        PhotonNetwork.JoinRandomRoom();
    }

    #region Photon Callbacks
    public override void OnConnectedToMaster() //サーバーにつながり、ロビー機能が使えるようになったら呼ばれる関数
    {
        ActivatePanel(lobbyPanel.name);
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedRoom() //ルームに入ったら呼ばれる関数
    {
        ActivatePanel(joiningPanel.name);
        PhotonNetwork.LoadLevel("GameScene"); //オンラインのゲームシーンへ遷移 
        print("Enter!");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)//RoomListに変更があれば呼ばれる関数
    {
        ClearRoomListView(); //ルームプレファブのリストを削除する関数

        foreach (RoomInfo room in roomList) //既存の全ルームに対して処理
        {
            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList) //鍵がかかっていたり、非表示な（有効でない）ルームの場合
            {
                if (cachedRoomList.ContainsKey(room.Name)) //有効なルームとしてリストに格納されている場合
                {
                    cachedRoomList.Remove(room.Name); //有効なルームのリストから削除
                }
            }
            else //有効なルームの場合
            {
                if (cachedRoomList.ContainsKey(room.Name)) //すでに有効なルームのリストに格納されている場合
                {
                    cachedRoomList[room.Name] = room; //RoomInfo型の情報を渡す
                }
                else //新規に見つかった場合
                {
                    cachedRoomList.Add(room.Name, room); //有効なルームとしてリストに追加
                }
            }
        }

        foreach (RoomInfo room in cachedRoomList.Values) //既存の全ての有効なルームに対して処理
        {
            GameObject roomListEntryGameObject = Instantiate(roomPrefab); //そのルーム用のルームプレファブを生成
            roomListEntryGameObject.transform.SetParent(parentObj.transform); //Content配下に格納
            roomListEntryGameObject.transform.localScale = Vector3.one;

            roomListEntryGameObject.transform.Find("TextRoomName").GetComponent<Text>().text = room.Name; //ルーム名を表示
            roomListEntryGameObject.transform.Find("TextRoomPlayers").GetComponent<Text>().text = room.PlayerCount + " / " + room.MaxPlayers; //プレイヤー数を表示
            roomListEntryGameObject.transform.Find("ButtonJoinRoom").GetComponent<Button>().onClick.AddListener(() => OnJoinRoomButtonClicked(room.Name)); //ボタンを押すとルームに入る関数が呼ばれるように紐付け

            roomListGameObjects.Add(room.Name, roomListEntryGameObject); //ルーム情報を持ったルームプレファブのリストに追加
        }
    }

    public override void OnLeftLobby() //ロビーを出る（≒ログアウト）と呼ばれる関数
    {
        ClearRoomListView(); //ルームプレファブのリストを削除する関数
        cachedRoomList.Clear(); //有効なルームのリストを削除
    }

    public override void OnJoinRandomFailed(short returnCode, string message) //部屋がなくランダム参加できなかったとき、自動でルームを作成する
    {
        string roomName = "Room " + Random.Range(1000, 10000); //部屋名をランダムな番号に

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; //プレイヤー数を2に
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    #endregion

    void OnJoinRoomButtonClicked(string _roomName)
    {
        if (PhotonNetwork.InLobby) //Joinボタンでロビーを離れてルームへ
        {
            PhotonNetwork.LeaveLobby();
        }
        PhotonNetwork.JoinRoom(_roomName);

    }


    void ClearRoomListView() //ルームプレファブのリストを削除する関数
    {
        foreach (var roomListGameObject in roomListGameObjects.Values)
        {
            Destroy(roomListGameObject);
        }
        roomListGameObjects.Clear();
    }

    #region  Public Methods
    //特定のパネルを表示させる関数
    public void ActivatePanel(string panelToBeActivated) //ここに含まれるゲームオブジェクトはいずれか1つだけ表示されるようになる
    {
        selectModePanel.SetActive(panelToBeActivated.Equals(selectModePanel.name));
        loginPanel.SetActive(panelToBeActivated.Equals(loginPanel.name));
        lobbyPanel.SetActive(panelToBeActivated.Equals(lobbyPanel.name));
        createPanel.SetActive(panelToBeActivated.Equals(createPanel.name));
        joiningPanel.SetActive(panelToBeActivated.Equals(joiningPanel.name));
    }
    #endregion
}
