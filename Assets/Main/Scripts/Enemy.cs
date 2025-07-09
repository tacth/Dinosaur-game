using UnityEngine;
using Photon.Pun;

public class Enemy : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // マスタークライアントから速さを受け取る
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        speed = (float)data[0];
    }

    // Update is called once per frame
    void Update()
    {
        // 左に移動する
        gameObject.transform.Translate(Vector2.left * speed * Time.deltaTime);
    }
}
