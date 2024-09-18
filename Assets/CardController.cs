using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

//カードのタイプ
public enum CardType
{
    Muhon,
}

public class CardController : MonoBehaviour
{
    //カードのプレイヤー番号 1P: 0, 2P: 1, 3P: 2, 4P: 3
    public int Player;
    //カードの種類
    public CardType CardType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //初期設定
    public void Init(int player,int cardtype)
    {
        Player = player;
        CardType = (CardType)cardtype;
        //カードの表示
        Show();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }
}
