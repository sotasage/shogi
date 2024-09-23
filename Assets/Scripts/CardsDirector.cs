using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardsDirector : MonoBehaviour
{
    [SerializeField] public List<GameObject> prefabCards;

    public Transform canvas;

    [SerializeField] public Button buttonUseCard;

    [SerializeField] GameSceneDirector gameSceneDirector;

    //プレイヤーが持っているカード
    public List<CardController>[] playerCards = new List<CardController>[4];

    //現在選択中のカード
    public CardController selectCard;

    //選択中に右側に表示されるカード
    GameObject sampleCard;

    //カードを使用したかどうか
    public bool usedFlag;

    //所持枚数
    public int[] cardMaxNums = new int[] {0,0,0,0};

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //カードを指定枚数加える関数
    public void AddCards(int player,int num,bool hoju=false)
    {
        if (player == 0)
        {
            DestroyCards(player);
        }
        for (int i = 0; i < num; i++)
        {
            int type = Random.Range(0,prefabCards.Count);

            CardController cardctrl = gameObject.AddComponent<CardController>();
            cardctrl.Init(player, type);

            playerCards[player].Add(cardctrl);
        }
        if (player == 0)
        {
            InstantiateCards(0);
        }
        if (!hoju)
        {
           if (cardMaxNums[player] + num > 5)
            {
                cardMaxNums[player] = 5;
                return;
            }
            cardMaxNums[player] += num;
        }
    }
    //カードを使った分だけ配る関数
    public void DealCards(int player)
    {
        //playerの現在のカード枚数
        int cardcount = playerCards[player].Count;

        AddCards(player, cardMaxNums[player]-cardcount,true);
    }

    //手持ちのカードを実体化する関数(左から詰めていき、七枚でちょうどいい）
    public void InstantiateCards(int player)
    {
        //初期位置
        float x = -CardController.Width * 3;

        for (int i = 0; i < playerCards[player].Count; i++)
        {
            CardController cardctrl = playerCards[player][0];
            playerCards[player].Remove(cardctrl);
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
                //selectCardが自分自身だったらfalse、別のカードだったらtrue
                bool selectFlg = selectCard != Cardctrl;

                if (selectCard)
                {
                    selectCard.Select(false);
                    selectCard = null;
                    if (sampleCard)
                    {
                        Destroy(sampleCard);
                    }
                    buttonUseCard.gameObject.SetActive(false);
                }
                if (selectFlg)
                {
                    Cardctrl.Select(selectFlg);
                    selectCard = Cardctrl;
                    int type = (int)selectCard.CardType;
                    sampleCard = Instantiate(prefabCards[type], canvas);
                    RectTransform rectTransform = sampleCard.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(300, 200); // 座標を設定
                    rectTransform.sizeDelta = new Vector2(150, 225);//サイズを設定
                    
                    if (gameSceneDirector.nowPlayer == player && !usedFlag)
                    {
                        buttonUseCard.gameObject.SetActive(true);
                    }
                }
            }
            button.onClick.AddListener(OnCardClick);

            Cardctrl.Init(player, type);
            playerCards[player].Add(Cardctrl);

            x += CardController.Width;
        }
    }

    //手持ちのカードのオブジェクトを削除
    public void DestroyCards(int player)
    {
        for (int i = 0; i < playerCards[player].Count; i++)
        {
            GameObject card = playerCards[player][i].gameObject;
            Destroy(card);
        }
    }

    public void OnClickButtonUseCard()
    {
        usedFlag = true;
        buttonUseCard.gameObject.SetActive(false);
        Destroy(selectCard.gameObject);
        Destroy(sampleCard);
        bool isRemove = playerCards[0].Remove(selectCard);
        print(isRemove);
        gameSceneDirector.UseCard(selectCard.CardType, gameSceneDirector.nowPlayer);
        selectCard = null;
    }
}
