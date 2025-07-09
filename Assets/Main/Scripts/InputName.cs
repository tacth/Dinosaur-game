using UnityEngine;
using TMPro;

public class InputName : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // プレイヤー名を保存する
    public void SaveName()
    {
        PlayerNameHolder.PlayerName = inputField.text;
    }
}
