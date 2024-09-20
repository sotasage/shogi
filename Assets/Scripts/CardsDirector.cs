using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class CardsDirector : MonoBehaviour
{
    [SerializeField] public List<GameObject> prefabCards;

    public Transform canvas;

    //プレイヤーが持っているカード
    public List<CardController>[] playerCards;

    //現在選択中のカード
    CardController selectCard;

    //選択中に右側に表示されるカード
    GameObject sampleCard;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //カードを5枚まで配る関数
    public void DealCards(int player)
    {
        //playerの現在のカード枚数
        int cardcount = playerCards[player].Count;

        for (int i = 0; i < 5 - cardcount; i++)
        {
            int type = Random.Range(0, prefabCards.Count);

            CardController cardctrl = gameObject.AddComponent<CardController>();
            cardctrl.Init(player, type);

            playerCards[player].Add(cardctrl);
        }
    }

    //手持ちのカードを実体化する関数
    public void InstantiateCards(int player)
    {
        //初期位置
        float x = -CardController.Width * 2;

        for (int i = 0; i < 5; i++)
        {
            CardController cardctrl = playerCards[player][i];
            int type = (int)cardctrl.CardType;
            GameObject card = Instantiate(prefabCards[type], canvas);
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(x, -40); // 座標を設定

            //cardにCardControllerをアタッチ
            CardController Cardctrl = card.AddComponent<CardController>();

            //Buttonコンポーネントをアタッチ
            Button button = card.AddComponent<Button>();
            void OnCardClick()
            {
                //selectCardが自分自身だったらfalse別のカードだったらtrue
                bool selectFlg = selectCard != Cardctrl;

                if (selectCard)
                {
                    selectCard.Select(false);
                    selectCard = null;
                    if (sampleCard)
                    {
                        Destroy(sampleCard);
                    }
                }
                if (selectFlg)
                {
                    Cardctrl.Select(selectFlg);
                    selectCard = Cardctrl;
                    int type = (int)selectCard.CardType;
                    sampleCard = Instantiate(prefabCards[type], canvas);
                    RectTransform rectTransform = sampleCard.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(300, 200); // 座標を設定
                    rectTransform.sizeDelta = new Vector2(150, 225);
                }
            }
            button.onClick.AddListener(OnCardClick);

            Cardctrl.Init(player, type);

            x += CardController.Width;
        }
    }
}
