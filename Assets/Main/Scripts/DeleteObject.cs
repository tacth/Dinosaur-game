using UnityEngine;

public class DeleteObject : MonoBehaviour
{
    public string objectTag = "Enemy"; // タグを指定

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
        // 特定のタグを持つオブジェクトと衝突したら
        if (collision.gameObject.CompareTag(objectTag))
        {
            Destroy(collision.gameObject); // 衝突したオブジェクトを削除
        }
    }
}
