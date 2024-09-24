using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

//カードのタイプ
public enum CardType
{
    reverse,
    Zyunbantobashi,
    ichimaituika,
    komaget,
    nikaikoudou,
    isseikyouka,
    huninare,
    ikusei,
    uragiri,
    henshin,
    irekae,
    hishaninare,
    kakuninare,
    saiminjutu,
}

public class CardController : MonoBehaviour
{
    //カードサイズ
    public const float Width = 100;

    //カードのプレイヤー番号 1P: 0, 2P: 1, 3P: 2, 4P: 3
    public int Player;
    //カードの種類
    public CardType CardType;

    //ユニットの選択/非選択のy座標
    public const float SelectUnitY = -30f;
    public const float UnSelectUnitY = -40f;

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

    //カードを表示
    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Select(bool select = true)
    {
        Vector3 pos = transform.position;

        if (select)
        {
            pos.y = SelectUnitY;
        }
        else
        {
            pos.y = UnSelectUnitY;
        }
        transform.position = pos;
    }


    void OnClickUseCard()
    {

    }
}
