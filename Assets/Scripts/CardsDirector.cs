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

    //�ŏ��ɃJ�[�h��5���z��֐�
    public List<CardController> DealCards()
    {
        List<CardController> ret = new List<CardController>();

        //�����ʒu
        float x = -CardController.Width * 2;

        int player = 0;

        for (int i = 0; i < 5; i++)
        {
            int type = Random.Range(0, prefabCards.Count);
            Vector3 pos = new Vector3(x, -40, 0);
            GameObject card = Instantiate(prefabCards[type], pos, Quaternion.identity);
            card.transform.SetParent(canvas.transform, false);

            CardController cardctrl = card.AddComponent<CardController>();
            //�����蔻��ǉ�
            BoxCollider bc = card.AddComponent<BoxCollider>();
            //�����蔻�茟�m�p
            Rigidbody rb = card.AddComponent<Rigidbody>();
            //�������Z�ƃJ�[�h���m�̓����蔻����g��Ȃ�
            bc.isTrigger = true;
            rb.isKinematic = true;
            cardctrl.Init(player, type);

            ret.Add(cardctrl);

            x += CardController.Width;
        }

        return ret;
    }
}
