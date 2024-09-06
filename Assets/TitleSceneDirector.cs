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
        cachedRoomList = new Dictionary<string, RoomInfo>(); //Key��string�^�AValue��RoomInfo�^�̔z���錾
        roomListGameObjects = new Dictionary<string, GameObject>(); //Key��string�^�AValue��GameObject�^�̔z���錾
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

    public void PlayerNameInputValueChanged() //PlayerName�̓��͂ŌĂԊ֐�
    {
        loginBtn.interactable = IsValidPName(); //bool�֐����画����󂯂�Afalse�Ȃ�Button���������ɂȂ��ĉ����Ȃ�
    }

    private bool IsValidPName()
    {
        //���O��3�����ȏ�12�����ȉ��ł�������ɂ��Ă���
        return !string.IsNullOrWhiteSpace(pNameInputField.text)
            && 3 <= pNameInputField.text.Length
            && pNameInputField.text.Length <= 12;
    }

    public void RoomNameInputValueChanged() //������RoomName�̓��͂ŌĂԊ֐�
    {
        createBtn.interactable = IsValidRName();
    }

    private bool IsValidRName()
    {
        //���O��3�����ȏ�12�����ȉ��ł�������ɂ��Ă���
        return !string.IsNullOrWhiteSpace(rNameInputField.text)
            && 3 <= pNameInputField.text.Length
            && pNameInputField.text.Length <= 12;
    }

    public void LoginClicked()
    {
        string playerName = pNameInputField.text;
        if (!string.IsNullOrEmpty(playerName)) //���O������������͂���Ă����
        {
            PhotonNetwork.LocalPlayer.NickName = playerName; //Photon��̖��O��ݒ�
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void CreateRoomClicked() //LobbyPanel�ł�CreateRoomButton
    {
        ActivatePanel(createPanel.name);
    }

    public void CreateEnterClicked() //CreatePanel�ł�CreateEnter
    {
        string roomName = rNameInputField.text;

        if (string.IsNullOrEmpty(roomName)) //���O��null�̏ꍇ
        {
            roomName = "Room " + Random.Range(1000, 10000); //�����Ŗ��O������
        }

        RoomOptions roomOptions = new RoomOptions(); //RoomOptions�N���X�̐錾
        roomOptions.MaxPlayers = 2; //���[�����2���ɐݒ�

        PhotonNetwork.CreateRoom(roomName, roomOptions); //PhotonNetwork�N���X�Ƀ��[���쐬�𑗐M
    }

    public void LoginCancelClicked() //LoginPanel�ł�CancelButton
    {
        ActivatePanel(selectModePanel.name);
    }

    public void CreateCancelClicked() //LobbyPanel�ł�CancelButton
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
    public override void OnConnectedToMaster() //�T�[�o�[�ɂȂ���A���r�[�@�\���g����悤�ɂȂ�����Ă΂��֐�
    {
        ActivatePanel(lobbyPanel.name);
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedRoom() //���[���ɓ�������Ă΂��֐�
    {
        ActivatePanel(joiningPanel.name);
        PhotonNetwork.LoadLevel("GameScene"); //�I�����C���̃Q�[���V�[���֑J�� 
        print("Enter!");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)//RoomList�ɕύX������ΌĂ΂��֐�
    {
        ClearRoomListView(); //���[���v���t�@�u�̃��X�g���폜����֐�

        foreach (RoomInfo room in roomList) //�����̑S���[���ɑ΂��ď���
        {
            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList) //�����������Ă�����A��\���ȁi�L���łȂ��j���[���̏ꍇ
            {
                if (cachedRoomList.ContainsKey(room.Name)) //�L���ȃ��[���Ƃ��ă��X�g�Ɋi�[����Ă���ꍇ
                {
                    cachedRoomList.Remove(room.Name); //�L���ȃ��[���̃��X�g����폜
                }
            }
            else //�L���ȃ��[���̏ꍇ
            {
                if (cachedRoomList.ContainsKey(room.Name)) //���łɗL���ȃ��[���̃��X�g�Ɋi�[����Ă���ꍇ
                {
                    cachedRoomList[room.Name] = room; //RoomInfo�^�̏���n��
                }
                else //�V�K�Ɍ��������ꍇ
                {
                    cachedRoomList.Add(room.Name, room); //�L���ȃ��[���Ƃ��ă��X�g�ɒǉ�
                }
            }
        }

        foreach (RoomInfo room in cachedRoomList.Values) //�����̑S�Ă̗L���ȃ��[���ɑ΂��ď���
        {
            GameObject roomListEntryGameObject = Instantiate(roomPrefab); //���̃��[���p�̃��[���v���t�@�u�𐶐�
            roomListEntryGameObject.transform.SetParent(parentObj.transform); //Content�z���Ɋi�[
            roomListEntryGameObject.transform.localScale = Vector3.one;

            roomListEntryGameObject.transform.Find("TextRoomName").GetComponent<Text>().text = room.Name; //���[������\��
            roomListEntryGameObject.transform.Find("TextRoomPlayers").GetComponent<Text>().text = room.PlayerCount + " / " + room.MaxPlayers; //�v���C���[����\��
            roomListEntryGameObject.transform.Find("ButtonJoinRoom").GetComponent<Button>().onClick.AddListener(() => OnJoinRoomButtonClicked(room.Name)); //�{�^���������ƃ��[���ɓ���֐����Ă΂��悤�ɕR�t��

            roomListGameObjects.Add(room.Name, roomListEntryGameObject); //���[���������������[���v���t�@�u�̃��X�g�ɒǉ�
        }
    }

    public override void OnLeftLobby() //���r�[���o��i�����O�A�E�g�j�ƌĂ΂��֐�
    {
        ClearRoomListView(); //���[���v���t�@�u�̃��X�g���폜����֐�
        cachedRoomList.Clear(); //�L���ȃ��[���̃��X�g���폜
    }

    public override void OnJoinRandomFailed(short returnCode, string message) //�������Ȃ������_���Q���ł��Ȃ������Ƃ��A�����Ń��[�����쐬����
    {
        string roomName = "Room " + Random.Range(1000, 10000); //�������������_���Ȕԍ���

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; //�v���C���[����2��
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    #endregion

    void OnJoinRoomButtonClicked(string _roomName)
    {
        if (PhotonNetwork.InLobby) //Join�{�^���Ń��r�[�𗣂�ă��[����
        {
            PhotonNetwork.LeaveLobby();
        }
        PhotonNetwork.JoinRoom(_roomName);

    }


    void ClearRoomListView() //���[���v���t�@�u�̃��X�g���폜����֐�
    {
        foreach (var roomListGameObject in roomListGameObjects.Values)
        {
            Destroy(roomListGameObject);
        }
        roomListGameObjects.Clear();
    }

    #region  Public Methods
    //����̃p�l����\��������֐�
    public void ActivatePanel(string panelToBeActivated) //�����Ɋ܂܂��Q�[���I�u�W�F�N�g�͂����ꂩ1�����\�������悤�ɂȂ�
    {
        selectModePanel.SetActive(panelToBeActivated.Equals(selectModePanel.name));
        loginPanel.SetActive(panelToBeActivated.Equals(loginPanel.name));
        lobbyPanel.SetActive(panelToBeActivated.Equals(lobbyPanel.name));
        createPanel.SetActive(panelToBeActivated.Equals(createPanel.name));
        joiningPanel.SetActive(panelToBeActivated.Equals(joiningPanel.name));
    }
    #endregion
}
