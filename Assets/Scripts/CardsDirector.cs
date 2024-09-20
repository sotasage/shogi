using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardsDirector : MonoBehaviour
{
    [SerializeField] List<GameObject> prefabCards;

    public Canvas canvas;

    //プレイヤーが持っているカード
    public List<CardController>[] playerCards;

    //現在選択中のカード
    public CardController selectCard;

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
            Vector3 pos = new Vector3(x, -40, 0);
            GameObject card = Instantiate(prefabCards[type], pos, Quaternion.identity);
            card.transform.SetParent(canvas.transform, false);

            //cardにCardControllerをアタッチ
            CardController Cardctrl = card.AddComponent<CardController>();

            //Buttonコンポーネントをアタッチ
            Button button = card.AddComponent<Button>();
            void OnCardClick()
            {
                if (selectCard)
                {
                    selectCard.Select(false);
                }
                Cardctrl.Select();
                selectCard = Cardctrl;
            }
            button.onClick.AddListener(OnCardClick);

            Cardctrl.Init(player, type);

            x += CardController.Width;
        }
    }
}
