using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiCardsDirector : MonoBehaviourPunCallbacks
{
    [SerializeField] public List<GameObject> prefabCards;

    public Transform canvas;

    [SerializeField] public Button buttonUseCard;

    [SerializeField] MultiGameSceneDirector multiGameSceneDirector;

    //�v���C���[�������Ă���J�[�h
    public List<CardController> playerCards;

    //���ݑI�𒆂̃J�[�h
    public CardController selectCard;

    //�I�𒆂ɉE���ɕ\�������J�[�h
    GameObject sampleCard;

    //�J�[�h���g�p�������ǂ���
    public bool usedFlag;

    //��������
    public int cardMaxNums = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //�J�[�h���w�薇��������֐�
    public void AddCards(int player, int num, bool hoju = false)
    {
        DestroyCards();

        for (int i = 0; i < num; i++)
        {
            int type = Random.Range(0, prefabCards.Count);

            CardController cardctrl = gameObject.AddComponent<CardController>();
            cardctrl.Init(player, type);

            playerCards.Add(cardctrl);
        }

        InstantiateCards(player);

        if (!hoju)
        {
            if (cardMaxNums + num > 5)
            {
                cardMaxNums = 5;
                return;
            }
            cardMaxNums += num;
        }
    }

    //�J�[�h���g�����������z��֐�
    public void DealCards(int player)
    {
        //player�̌��݂̃J�[�h����
        int cardcount = playerCards.Count;

        AddCards(player, cardMaxNums - cardcount, true);
    }

    //�莝���̃J�[�h�����̉�����֐�(������l�߂Ă����A�����ł��傤�ǂ����j
    public void InstantiateCards(int player)
    {
        //�����ʒu
        float x = -CardController.Width * 3;

        for (int i = 0; i < playerCards.Count; i++)
        {
            CardController cardctrl = playerCards[0];
            playerCards.Remove(cardctrl);
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
                //selectCard���������g��������false�A�ʂ̃J�[�h��������true
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
                    rectTransform.anchoredPosition = new Vector2(300, 200); // ���W��ݒ�
                    rectTransform.sizeDelta = new Vector2(150, 225);//�T�C�Y��ݒ�

                    if (multiGameSceneDirector.myturn && !usedFlag)
                    {
                        buttonUseCard.gameObject.SetActive(true);
                    }
                }
            }
            button.onClick.AddListener(OnCardClick);

            Cardctrl.Init(player, type);
            playerCards.Add(Cardctrl);

            x += CardController.Width;
        }
    }

    //�莝���̃J�[�h���폜
    public void DestroyCards()
    {
        for (int i = 0; i < playerCards.Count; i++)
        {
            GameObject card = playerCards[i].gameObject;
            Destroy(card);
        }
    }

    public void OnClickButtonUseCard()
    {
        usedFlag = true;
        buttonUseCard.gameObject.SetActive(false);
        Destroy(selectCard.gameObject);
        Destroy(sampleCard);
        bool isRemove = playerCards.Remove(selectCard);
        print(isRemove);
        multiGameSceneDirector.UseCard(selectCard.CardType);
        selectCard = null;
    }
}
