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

    //�v���C���[�������Ă���J�[�h
    public List<CardController>[] playerCards;

    //���ݑI�𒆂̃J�[�h
    CardController selectCard;

    //�I�𒆂ɉE���ɕ\�������J�[�h
    GameObject sampleCard;

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
            GameObject card = Instantiate(prefabCards[type], canvas);
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(x, -40); // ���W��ݒ�

            //card��CardController���A�^�b�`
            CardController Cardctrl = card.AddComponent<CardController>();

            //Button�R���|�[�l���g���A�^�b�`
            Button button = card.AddComponent<Button>();
            void OnCardClick()
            {
                //selectCard���������g��������false�ʂ̃J�[�h��������true
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
                    rectTransform.anchoredPosition = new Vector2(300, 200); // ���W��ݒ�
                    rectTransform.sizeDelta = new Vector2(150, 225);
                }
            }
            button.onClick.AddListener(OnCardClick);

            Cardctrl.Init(player, type);

            x += CardController.Width;
        }
    }
}
