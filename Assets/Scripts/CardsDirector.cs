using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardsDirector : MonoBehaviour
{
    [SerializeField] List<GameObject> prefabCards;

    public Canvas canvas;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //最初にカードを5枚配る関数
    public List<CardController> DealCards()
    {
        List<CardController> ret = new List<CardController>();

        //初期位置
        float x = -CardController.Width * 2;

        int player = 0;

        for (int i = 0; i < 5; i++)
        {
            int type = Random.Range(0, prefabCards.Count);
            Vector3 pos = new Vector3(x, -40, 0);
            GameObject card = Instantiate(prefabCards[type], pos, Quaternion.identity);
            card.transform.SetParent(canvas.transform, false);

            CardController cardctrl = card.AddComponent<CardController>();
            //当たり判定追加
            BoxCollider bc = card.AddComponent<BoxCollider>();
            //当たり判定検知用
            Rigidbody rb = card.AddComponent<Rigidbody>();
            //物理演算とカード同士の当たり判定を使わない
            bc.isTrigger = true;
            rb.isKinematic = true;
            cardctrl.Init(player, type);

            ret.Add(cardctrl);

            x += CardController.Width;
        }

        return ret;
    }
}
