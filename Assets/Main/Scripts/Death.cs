using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter2D(Collision2D collision) // 衝突を検知する
    {
        // Enemyタグを持つオブジェクトと衝突したら
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 自身を削除する
            Destroy(gameObject);

            // ルームから退出する
            PhotonNetwork.LeaveRoom();

            // サーバーから切断する
            PhotonNetwork.Disconnect();

            // ロビーに戻る
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
