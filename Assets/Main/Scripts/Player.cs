using UnityEngine;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks
{
    private float playerSpeed;
    private SpriteRenderer spriteRenderer;
    private new Rigidbody2D rigidbody2D;

    public float moveSpeed = 5.0f; // 移動速度
    public float jumpForce = 5.0f; // ジャンプ力
    private bool isJumping = false; // 接地判定
    public float minX = -8.0f; // 移動できる最小のX座標
    public float maxX = 8.0f; // 移動できる最大のX座標

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // 自身が生成したオブジェクトだけに移動処理を行う
        if (photonView.IsMine)
        {
            // スペースキーを押したらジャンプする
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                Jump();
            }
            // 左キーかAキーを押したら
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                playerSpeed = -moveSpeed; // 左に移動
                spriteRenderer.flipX = false; // スプライトを元に戻す
            }
            // 右キーかDキーを押したら
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                playerSpeed = moveSpeed; // 右に移動
                spriteRenderer.flipX = true; // スプライトを反転
            }
            // 何も押さなかったら
            else
            {
                playerSpeed = 0.0f; // 止まる
            }

            rigidbody2D.linearVelocity = new Vector2(playerSpeed, rigidbody2D.linearVelocity.y);

            // 移動範囲を制限
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            transform.position = position;
        }
    }

    // ジャンプを実行
    private void Jump()
    {
        if (photonView.IsMine)
        {
            rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, jumpForce);
            isJumping = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (photonView.IsMine)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isJumping = false;
            }
        }
    }
}
