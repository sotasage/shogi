using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardsDirector : MonoBehaviour
{
    [SerializeField] List<GameObject> prefabCards;

    public Canvas canvas;

    //�v���C���[�������Ă���J�[�h
    public List<CardController>[] playerCards;

    //���ݑI�𒆂̃J�[�h
    public CardController selectCard;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //�J�[�h��5���܂Ŕz��֐�
    public void DealCards(int player)
    {
        //player�̌��݂̃J�[�h����
        int cardcount = playerCards[player].Count;

        for (int i = 0; i < 5 - cardcount; i++)
        {
            int type = Random.Range(0, prefabCards.Count);

            CardController cardctrl = gameObject.AddComponent<CardController>();
            cardctrl.Init(player, type);

            playerCards[player].Add(cardctrl);
        }
    }

    //�莝���̃J�[�h�����̉�����֐�
    public void InstantiateCards(int player)
    {
        //�����ʒu
        float x = -CardController.Width * 2;

        for (int i = 0; i < 5; i++)
        {
            CardController cardctrl = playerCards[player][i];
            int type = (int)cardctrl.CardType;
            Vector3 pos = new Vector3(x, -40, 0);
            GameObject card = Instantiate(prefabCards[type], pos, Quaternion.identity);
            card.transform.SetParent(canvas.transform, false);

            //card��CardController���A�^�b�`
            CardController Cardctrl = card.AddComponent<CardController>();

            //Button�R���|�[�l���g���A�^�b�`
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
