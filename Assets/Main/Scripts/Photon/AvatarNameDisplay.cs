using UnityEngine;
using Photon.Pun;
using TMPro;

public class AvatarNameDisplay : MonoBehaviourPunCallbacks
{
    private TextMeshPro nameLabel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nameLabel = GetComponent<TextMeshPro>();
        // プレイヤー名を表示する
        nameLabel.text = $"{photonView.Owner.NickName}";
    }

    // Update is called once per frame
    void Update()
    {

    }
}
