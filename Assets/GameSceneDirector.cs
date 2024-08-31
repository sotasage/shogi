using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneDirector : MonoBehaviour
{
    //UI関連
    [SerializeField] Text textTurnInfo;
    [SerializeField] Text textResultInfo;
    [SerializeField] Button buttonTitle;
    [SerializeField] Button buttonRematch;
    [SerializeField] Button buttonEvolutionApply;
    [SerializeField] Button buttonEvolutionCancel;

    //ゲーム設定
    const int PlayerMax = 4;
    int boardWidth;
    int boardHeight;

    //タイルのプレハブ
    [SerializeField] GameObject prefabTile;

    //ユニットのプレハブ
    [SerializeField] List<GameObject> prefabUnits;

    //初期配置
    int[,] boardSetting =
    {
        { 0, 0, 16, 17, 18, 17, 16, 0, 0 },
        { 0, 0, 0, 11, 13, 11, 0, 0, 0 },
        { 6, 0, 0, 0, 11, 0, 0, 0, 26 },
        { 7, 1, 0, 0, 0, 0, 0, 21, 27 },
        { 8, 3, 1, 0, 0, 0, 21, 23, 28 },
        { 7, 1, 0, 0, 0, 0, 0, 21, 27 },
        { 6, 0, 0, 0, 31, 0, 0, 0, 26 },
        { 0, 0, 0, 31, 33, 31, 0, 0, 0 },
        { 0, 0, 36, 37, 38, 37, 36, 0, 0 },
    };

    // Start is called before the first frame update
    void Start()
    {
        //UI関連初期設定
        buttonTitle.gameObject.SetActive(false);
        buttonRematch.gameObject.SetActive(false);
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        textResultInfo.text = "";

        //ボードサイズ
        boardWidth = boardSetting.GetLength(0);
        boardHeight = boardSetting.GetLength(1);

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                //タイルとユニットのポジション
                float x = i - boardWidth / 2;
                float y = j - boardHeight / 2;

                //ポジション
                Vector3 pos = new Vector3(x, 0, y);

                //タイル作成
                GameObject tile = Instantiate(prefabTile, pos, Quaternion.identity);

                //ユニット作成
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                if (0 == type) continue;

                //初期化
                pos.y = 0.7f;

                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 90, 0));
                unit.AddComponent<Rigidbody>();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
