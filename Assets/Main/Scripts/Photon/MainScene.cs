using Photon.Pun;
using Photon.Realtime;
using System.Xml;
using UnityEngine;

public class MainScene : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // プレイヤー名を取得する
        string playerName = PlayerNameHolder.PlayerName;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "名前を取得できませんでした。";
        }

        // 名前を設定する
        PhotonNetwork.NickName = playerName;

        // マスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // マスターサーバーへの接続が成功した
    public override void OnConnectedToMaster()
    {
        // "Room"という名前のルームに参加する（ルームが存在しなければ作成して参加する）
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }

    // ゲームサーバーへの接続が成功した
    public override void OnJoinedRoom()
    {
        // アバター（ネットワークオブジェクト）を生成する
        var position = new Vector3(-6, 6);
        PhotonNetwork.Instantiate("Avatar", position, Quaternion.identity);
    }
}
