using UnityEngine;
using Photon.Pun;

public class Generate : MonoBehaviourPunCallbacks
{
    public GameObject[] prefab;
    private bool isGenerating = false;
    // 移動速度
    public float minSpeed = 1.0f;
    public float maxSpeed = 5.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(WaitForJoinAndStart());
    }

    // Update is called once per frame
    void Update()
    {

    }

    System.Collections.IEnumerator WaitForJoinAndStart()
    {
        // ルーム参加まで待機
        while (!PhotonNetwork.InRoom)
        {
            yield return null;
        }

        // マスタークライアントだけが生成ループを開始
        if (PhotonNetwork.IsMasterClient)
        {
            StartGenerate();
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // マスタークライアントになったら生成ループを開始
        if (PhotonNetwork.IsMasterClient && !isGenerating)
        {
            StartGenerate();
        }
    }

    private void StartGenerate()
    {
        isGenerating = true;
        StartCoroutine(GenerateLoop());
    }

    System.Collections.IEnumerator GenerateLoop()
    {
        while (true)
        {
            // ランダムなPrefabを選択
            int prefebIndex = Random.Range(0, prefab.Length);
            GameObject generateObject = prefab[prefebIndex];

            // 移動する速さを決める
            float speed = Random.Range(minSpeed, maxSpeed);

            // 生成
            PhotonNetwork.Instantiate(generateObject.name, transform.position, Quaternion.identity, 0 , new object[] {speed});

            // 生成間隔をランダムに設定
            float interval = Random.Range(1f, 5f);

            // 次の生成まで待機
            yield return new WaitForSeconds(interval);
        }
    }
}
