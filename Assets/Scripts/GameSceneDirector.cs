﻿using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;
using Random = UnityEngine.Random;

public class GameSceneDirector : MonoBehaviour
{
    //UI関連
    [SerializeField] Text textTurnInfo;
    [SerializeField] Text textResultInfo;
    [SerializeField] Button buttonTitle;
    [SerializeField] Button buttonRematch;
    [SerializeField] Button buttonEvolutionApply;
    [SerializeField] Button buttonEvolutionCancel;
    [SerializeField] GameObject parentObj;
    [SerializeField] GameObject textUsedCardLog;

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

    //フィールドデータ
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //現在選択中のユニット
    public UnitController selectUnit;

    //移動可能範囲
    Dictionary<GameObject, Vector2Int> movableTiles;

    //カーソルのプレハブ
    [SerializeField] GameObject prefabCursor;

    //カーソルオブジェクト
    List<GameObject> cursors;

    //プレイヤーとターン
    public int nowPlayer;
    int turnCount;
    bool isCpu;

    //モード
    enum Mode
    {
        None,
        Start,
        Select,
        WaitEvolution,
        TurnChange,
        Result
    }

    Mode nowMode, nextMode;

    //持ち駒タイルのプレハブ
    [SerializeField] GameObject prefabUnitTile;

    //持ち駒を置く場所
    List<GameObject>[] unitTiles;

    //キャプチャされたユニット
    List<UnitController> captureUnits;

    //カードのフラグ初期化
    bool zyunbantobashi = false;
    public static bool reverse = false;
    bool nikaikoudou = false;
    bool ikusei = false;
    bool uragiri = false;
    bool huninare = false;
    bool henshin = false;
    bool irekae = false;
    bool hishaninare = false;
    bool kakuninare = false;
    bool saiminjutu = false;

    //一斉強化カードの管理
    public List<UnitController>[] isseikyoukatyu = new List<UnitController>[4];
    public int[] isseikyoukaTurn = new int[] {0,0,0,0};
    UnitType checkSameUnit;

    //入れ替えカードの変数
    UnitController unit1 = null;
    bool firstSelected = false;

    //敵陣設定
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    //CPU
    const float EnemyWaitTimerMax = 3;
    float enemyWaitTimer;
    public static int PlayerCount = 1;

    //サウンド制御
    [SerializeField] SoundController sound;

    //プレイヤーの詰み状態
    bool[] istumi;
    int tumicount;

    //CardsDirector
    [SerializeField] CardsDirector cardsDirector;

    //カード名
    Dictionary<CardType, string> CardName = new Dictionary<CardType, string>()
    {
        { CardType.reverse, "リバース" },
        { CardType.Zyunbantobashi, "順番飛ばし" },
        { CardType.ichimaituika, "一枚追加" },
        { CardType.komaget, "駒ゲット" },
        { CardType.nikaikoudou, "二回行動" },
        { CardType.isseikyouka, "一斉強化" },
        { CardType.huninare, "歩になれ！" },
        { CardType.ikusei, "育成" },
        { CardType.uragiri, "裏切り" },
        { CardType.henshin, "変身" },
        { CardType.irekae, "入れ替え" },
        { CardType.hishaninare, "飛車になれ！" },
        { CardType.kakuninare, "角になれ！" },
        { CardType.saiminjutu, "催眠術" },
        { CardType.cardReset, "カードリセット" },
    };

    // Start is called before the first frame update
    void Start()
    {
        //BGM再生　うるさいので消しておく
        //sound.PlayBGM(0);

        //UI関連初期設定
        buttonTitle.gameObject.SetActive(true);
        buttonRematch.gameObject.SetActive(false);
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        cardsDirector.buttonUseCard.gameObject.SetActive(false);
        textResultInfo.text = "";

        //ボードサイズ
        boardWidth = boardSetting.GetLength(0);
        boardHeight = boardSetting.GetLength(1);

        //フィールド初期化
        tiles = new Dictionary<Vector2Int, GameObject>();
        units = new UnitController[boardWidth, boardHeight];

        //移動可能範囲
        movableTiles = new Dictionary<GameObject, Vector2Int>();
        cursors = new List<GameObject>();

        //持ち駒を置く場所
        unitTiles = new List<GameObject>[PlayerMax];

        //キャプチャされたユニット
        captureUnits = new List<UnitController>();

        //プレイヤーの詰み状態
        istumi = new bool[4];
        tumicount = 0;

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                //タイルとユニットのポジション
                float x = i - boardWidth / 2;
                float y = j - boardHeight / 2;

                //ポジション
                Vector3 pos = new Vector3(x, 0, y);

                //タイルのインデックス
                Vector2Int tileindex = new Vector2Int(i, j);

                //タイル作成
                GameObject tile = Instantiate(prefabTile, pos, Quaternion.identity);
                tiles.Add(tileindex, tile);

                //ユニット作成
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                if (0 == type) continue;

                //初期化
                pos.y = 0.7f;

                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 90, 0));
                unit.AddComponent<Rigidbody>();

                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, tileindex);

                //ユニットデータセット
                units[i, j] = unitctrl;
            }
        }

        //持ち駒を置く場所の作成
        Vector3 startposEven = new Vector3(-4, 0.5f, -5);
        Vector3 startposOdd = new Vector3(-5, 0.5f, 4);

        for (int i = 0; i < PlayerMax; i++)
        {
            unitTiles[i] = new List<GameObject>();
            int dir = 1;
            if (i == 2 || i == 3) dir = -1;

            for (int j = 0; j < 9; j++)
            {
                Vector3 resultpos = new Vector3();

                if (i == 0 || i == 2)
                {
                    Vector3 posEven = startposEven;
                    posEven.x = (posEven.x + j) * dir;
                    posEven.z = posEven.z * dir;

                    resultpos = posEven;
                }
                else
                {
                    Vector3 posOdd = startposOdd;
                    posOdd.x = posOdd.x * dir;
                    posOdd.z = (posOdd.z - j) * dir;

                    resultpos = posOdd;
                }

                GameObject obj = Instantiate(prefabUnitTile, resultpos, Quaternion.Euler(0, 90 * i, 0));
                unitTiles[i].Add(obj);

                obj.SetActive(false);
            }
        }

        //カードの作成
        cardsDirector.playerCards = new List<CardController>[4];

        for (int i = 0; i < 4; i++)
        {
            cardsDirector.playerCards[i] = new List<CardController>();
        }

        for (int i = 0; i < 4; i++)
        {
            cardsDirector.AddCards(i, 3);//初期枚数の設定

        }


        //TurnChangeから始める場合-1
        nowPlayer = -1;

        //敵陣設定
        enemyLines = new List<int>[PlayerMax];
        for (int i = 0; i < PlayerMax; i++)
        {
            enemyLines[i] = new List<int>();
            int rangemin = 0;
            if (0 == i || 1 == i)
            {
                rangemin = boardHeight - EnemyLine;
            }

            for (int j = 0; j < EnemyLine; j++)
            {
                enemyLines[i].Add(rangemin + j);
            }
        }

        //初回モード
        nowMode = Mode.None;
        nextMode = Mode.TurnChange;

        //isseikyoukatyuのリストの初期化
        for (int i = 0; i < isseikyoukatyu.Length; i++)
        {
            isseikyoukatyu[i] = new List<UnitController>();
        }
    }

        // Update is called once per frame
        void Update()
    {
        if (Mode.Start == nowMode)
        {
            startMode();
        }
        else if (Mode.Select == nowMode)
        {
            selectMode();
        }
        else if (Mode.TurnChange == nowMode)
        {
            turnChangeMode();
        }

        //モード変更
        if (Mode.None != nextMode)
        {
            nowMode = nextMode;
            nextMode = Mode.None;
        }
    }

    //選択時
    public void setSelectCursors(UnitController unit = null, bool playerunit = true)
    {
        //カーソル削除
        foreach (var item in cursors)
        {
            Destroy(item);
        }
        cursors.Clear();

        //選択ユニットの非選択状態
        if (selectUnit)
        {
            selectUnit.Select(false);
            selectUnit = null;
        }

        //ユニット情報がなければ終了
        if (!unit) return;

        //移動可能範囲取得
        List<Vector2Int> movabletiles = getMovableTiles(unit);
        movableTiles.Clear();

        foreach (var item in movabletiles)
        {
            movableTiles.Add(tiles[item], item);

            //カーソル生成
            Vector3 pos = tiles[item].transform.position;
            pos.y += 0.51f;
            GameObject cursor = Instantiate(prefabCursor, pos, Quaternion.identity);
            cursors.Add(cursor);
        }

        //選択状態
        if (playerunit)
        {
            unit.Select();
            selectUnit = unit;
        }
    }

    //ユニット移動
    Mode moveUnit(UnitController unit, Vector2Int tileindex)
    {
        //移動し終わった後のモード
        Mode ret = Mode.TurnChange;

        //現在地
        Vector2Int oldpos = unit.Pos;

        //移動先に誰かいたら取る
        captureUnit(nowPlayer, tileindex);

        //ユニット移動
        unit.Move(tiles[tileindex], tileindex);

        //内部データ更新(新しい場所)
        units[tileindex.x, tileindex.y] = unit;

        //ボード上の駒を更新
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            //内部データ更新
            units[oldpos.x, oldpos.y] = null;

            //成
            if (unit.Player == 0 || unit.Player == 2)
            {
                if (unit.isEvolution() && (enemyLines[unit.Player].Contains(tileindex.y) || enemyLines[unit.Player].Contains(oldpos.y)))
                {
                    //次のターンに移動可能かどうか
                    UnitController[,] copyunits = new UnitController[boardWidth, boardHeight];
                    //自分以外いないフィールドを作る
                    copyunits[unit.Pos.x, unit.Pos.y] = unit;

                    //CPUもしくは次移動できないなら強制成
                    if (isCpu || 1 > unit.GetMovableTiles(copyunits).Count)
                    {
                        unit.Evolution();
                    }
                    //なるか確認
                    else
                    {
                        //成った状態を表示
                        unit.Evolution();
                        setSelectCursors(unit);
                        print(unit.UnitType);

                        //ナビゲーション
                        textResultInfo.text = "成りますか？";
                        buttonEvolutionApply.gameObject.SetActive(true);
                        buttonEvolutionCancel.gameObject.SetActive(true);

                        ret = Mode.WaitEvolution;
                    }
                }
            }
            else
            {
                if (unit.isEvolution() && (enemyLines[unit.Player].Contains(tileindex.x) || enemyLines[unit.Player].Contains(oldpos.x)))
                {
                    //次のターンに移動可能かどうか
                    UnitController[,] copyunits = new UnitController[boardWidth, boardHeight];
                    //自分以外いないフィールドを作る
                    copyunits[unit.Pos.x, unit.Pos.y] = unit;

                    //CPUもしくは次移動できないなら強制成
                    if (isCpu || 1 > unit.GetMovableTiles(copyunits).Count)
                    {
                        unit.Evolution();
                    }
                    //なるか確認
                    else
                    {
                        //成った状態を表示
                        unit.Evolution();
                        setSelectCursors(unit);
                        print(unit.UnitType);

                        //ナビゲーション
                        textResultInfo.text = "成りますか？";
                        buttonEvolutionApply.gameObject.SetActive(true);
                        buttonEvolutionCancel.gameObject.SetActive(true);

                        ret = Mode.WaitEvolution;
                    }
                }
            }
        }
        //持ち駒の更新
        else
        {
            //持ち駒の更新
            captureUnits.Remove(unit);
        }

        //ユニットの状態を更新
        unit.FieldStatus = FieldStatus.OnBoard;

        //持ち駒表示を更新
        alignCaptureUnits(nowPlayer);

        //SE再生
        sound.PlaySE(0);

        return ret;
    }

    //移動可能範囲取得
    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        return ret;
    }

    //ターン開始
    void startMode()
    {
        //脱落してたら即次の人へ
        if (istumi[nowPlayer])
        {
            nextMode = Mode.TurnChange;
            return;
        }

        //玉がとられているかどうか
        bool gyoku_survive = false;
        foreach (var item in getUnits(nowPlayer))
        {
            if (UnitType.Gyoku == item.UnitType && FieldStatus.OnBoard == item.FieldStatus)
            {
                gyoku_survive = true;
            };
        }

        //王がとられていたら脱落
        if (!gyoku_survive)
        {
            istumi[nowPlayer] = true;
            tumicount++;
            nextMode = Mode.TurnChange;
            //脱落したプレイヤーの駒を消去
            foreach (var item in getUnits(nowPlayer))
            {
                Destroy(item.gameObject);
                if (item.FieldStatus == FieldStatus.OnBoard)
                {
                    units[item.Pos.x, item.Pos.y]=null;
                }
                else
                {
                    captureUnits.Remove(item);
                }
            }
            //脱落したプレイヤーの持ち駒タイルを削除
            if (unitTiles[nowPlayer].Count > 0)
            {
                foreach (var item in unitTiles[nowPlayer])
                {
                    Destroy(item);
                }
            }

            return ;
        }

        //プレイヤーのターンかつカードを選択しているならカード選択ボタンを表示
        if (nowPlayer == 0 && cardsDirector.selectCard)
        {
            //カードの効果を使えない場合ボタンを押せない状態にして表示
            cardsDirector.buttonUseCard.interactable = isCanUseCard(cardsDirector.selectCard.CardType);
            cardsDirector.buttonUseCard.gameObject.SetActive(true);
        }

        //勝敗がついていなければ通常モード
        nextMode = Mode.Select;

        //順番飛ばし処理
        if (zyunbantobashi)
        {
            nextMode = Mode.TurnChange;
            zyunbantobashi = false;
            return;
        }

        //Info更新
        textTurnInfo.text = "" + (nowPlayer + 1) + "Pの番です";
        textResultInfo.text = "";


        //勝敗チェック

        //王手しているユニット
        List<UnitController> outeunits = GetOuteUnitsUke(units, nowPlayer);
        bool isoute = 0 < outeunits.Count;
        if (isoute)
        {
            textResultInfo.text = "王手";
        }

        if (tumicount >= 3)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!istumi[i])
                {
                    textResultInfo.text = "詰み\n" + (i + 1) + "Pの勝ち";
                    break;
                }
            }

            nextMode = Mode.Result;
        }

        //CPU判定
        if (PlayerCount <= nowPlayer)
        {
            isCpu = true;
            enemyWaitTimer = Random.Range(1, EnemyWaitTimerMax);
        }

        //次が結果表示画面なら
        if (Mode.Result == nextMode)
        {
            textTurnInfo.text = "";
            buttonRematch.gameObject.SetActive(true);
            buttonTitle.gameObject.SetActive(true);
        }
    }


    //ユニットとタイル選択
    void selectMode()
    {
        GameObject tile = null;
        UnitController unit = null;

        //プレイヤー処理
        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //手前のユニットにも当たり判定があるのでヒットしたすべてのオブジェクト情報を取得
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                UnitController hitunit = hit.transform.GetComponent<UnitController>();

                //持ち駒
                if (hitunit && FieldStatus.Captured == hitunit.FieldStatus)
                {
                    unit = hitunit;
                }
                //タイル選択と上に乗っているユニット
                else if (tiles.ContainsValue(hit.transform.gameObject))
                {
                    tile = hit.transform.gameObject;
                    //タイルからユニットを探す
                    foreach (var item in tiles)
                    {
                        if (item.Value == tile)
                        {
                            unit = units[item.Key.x, item.Key.y];
                        }
                    }
                    break;
                }
            }
        }

        //CPU処理
        if (isCpu)
        {
            //タイマー消化
            if (0 < enemyWaitTimer)
            {
                enemyWaitTimer -= Time.deltaTime;
                return;
            }

            //カード使用
            if (!cardsDirector.usedFlag)
            {
                cardsDirector.usedFlag = true;
                List<CardController> cards = new List<CardController>();
                foreach (var item in cardsDirector.playerCards[nowPlayer])
                {
                    if (isCanUseCard(item.CardType)) cards.Add(item);
                }
                if (cards.Count > 0)
                { 
                    int cardnum = Random.Range(0, cards.Count);
                    CardController card = cards[cardnum];
                    UseCard(card.CardType, nowPlayer);
                    bool isRemove = cardsDirector.playerCards[nowPlayer].Remove(card);
                }
            }

            //ユニット選択
            if (!selectUnit)
            {
                if (ikusei)
                {
                    //成らせる自駒をランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.ikusei);
                    unit = ret[Random.Range(0, ret.Count)];
                }
                else if (uragiri)
                {
                    //自駒にする敵駒をランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.uragiri);
                    unit = ret[Random.Range(0, ret.Count)];
                }
                else if (huninare)
                {
                    //歩にする駒を敵駒からランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.huninare);
                    unit = ret[Random.Range(0, ret.Count)];

                }
                else if (hishaninare)
                {
                    //飛車にする駒を自駒からランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.hishaninare);
                    unit = ret[Random.Range(0, ret.Count)];

                }
                else if (kakuninare)
                {
                    //角にする駒を自駒からランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.kakuninare);
                    unit = ret[Random.Range(0, ret.Count)];

                }
                else if (henshin)
                {
                    //ランダムな駒にする駒を自駒からランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.henshin);
                    unit = ret[Random.Range(0, ret.Count)];

                }
                else if (saiminjutu)
                {
                    //動かす敵の駒を選択
                    List<UnitController> ret = GetUnitsForCard(CardType.saiminjutu);
                    unit = ret[Random.Range(0, ret.Count)];
                    //移動できないならやり直し
                    if (1 > getMovableTiles(unit).Count)
                    {
                        unit = null;
                    }
                }
                else if (irekae)
                {
                    //場所を入れ替える駒を自駒からランダムで選択
                    List<UnitController> ret = GetUnitsForCard(CardType.irekae);
                    unit = ret[Random.Range(0, ret.Count)];
                }

                else
                {
                    //全ユニット取得してランダムで選択
                    List<UnitController> allunits;
                    allunits = getUnits(nowPlayer);
                    unit = allunits[Random.Range(0, allunits.Count)];
                    //移動できないならやり直し
                    if (1 > getMovableTiles(unit).Count)
                    {
                        unit = null;
                    }
                }
            }
            //タイル選択
            else
            {
                //今回移動可能なタイルをランダムで選択
                List<GameObject> tiles = new List<GameObject>(movableTiles.Keys);
                tile = tiles[Random.Range(0, tiles.Count)];
                //持ち駒は非表示になっている可能性があるので表示する
                selectUnit.gameObject.SetActive(true);
            }
        }

        //何も選択されていなければ処理をしない
        if (null == tile && null == unit) return;

        /*if (selectUnit && unit)
        {
            if (selectUnit == unit)
            {
                setSelectCursors();
                unit = null;
            }
        }*/

        //移動先選択
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            //カードの使用を制限
            cardsDirector.usedFlag = true;
            nextMode = moveUnit(selectUnit, movableTiles[tile]);
            //催眠術カードフラグ
            saiminjutu = false;
        }

        //ユニット選択
        else if (unit)
        {
            //育成
            if (ikusei)
            {
                //指定したユニットを成らせる                
                if (nowPlayer == unit.Player)
                {
                    if (!unit.isEvolution() || unit.FieldStatus == FieldStatus.Captured)
                    {
                        return;
                    }
                    //強化中リストから除外
                    if (isseikyoukatyu[unit.Player].Contains(unit))
                    {
                        isseikyoukatyu[unit.Player].Remove(unit);
                    }

                    unit.Evolution();
                    textResultInfo.text = "";
                    ikusei = false;
                }
                unit = null;
                return;
            }
            //裏切り
            else if (uragiri)
            {
                //敵駒を自駒に変える
                if (nowPlayer != unit.Player)
                {
                    if (UnitType.Gyoku == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                    {
                        return;
                    }
                    unit.Player = nowPlayer;
                    unit.transform.eulerAngles = unit.getDefaultAngles(nowPlayer);

                    //成り駒の場合
                    if ((int)unit.UnitType >= 9)
                    {
                        Vector3 angle = unit.transform.eulerAngles;

                        angle.x = 270;
                        angle.y = 90 * (unit.Player - 2);
                        angle.z = 0;
                        unit.transform.eulerAngles = angle;
                    }

                    textResultInfo.text = "";
                    uragiri = false;
                    nextMode = Mode.TurnChange;
                }
                unit = null;
                return;
            }
            //歩になれ
            else if (huninare) 
            {
                //指定した駒を歩にする
                if (UnitType.Gyoku == unit.UnitType || UnitType.Hu == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }
                //歩に変える処理(内部データもオブジェクトも
                Destroy(unit.gameObject);
                Vector3 pos = unit.gameObject.transform.position;

                GameObject prefabHu = prefabUnits[0];
                GameObject hu = Instantiate(prefabHu, pos, Quaternion.Euler(90, unit.Player * 90, 0));
                hu.AddComponent<Rigidbody>();

                UnitController unitctrl = hu.AddComponent<UnitController>();
                unitctrl.Player = unit.Player;
                unitctrl.UnitType = UnitType.Hu;
                //獲られたときもとにもどるよう
                unitctrl.OldUnitType = UnitType.Hu;
                //場所の初期化
                unitctrl.FieldStatus = FieldStatus.OnBoard;
                //角度と場所
                unitctrl.transform.eulerAngles = unitctrl. getDefaultAngles(unit.Player);
                unitctrl.Pos = unit.Pos;


                //ユニットデータセット
                units[unit.Pos.x, unit.Pos.y] = unitctrl;

                //強化中リストから除外
                if (isseikyoukatyu[unit.Player].Contains(unit))
                {
                    isseikyoukatyu[unit.Player].Remove(unit);
                }

                unit =null;
                huninare= false;
                textResultInfo.text = "";

                return;
            }
            //飛車になれ
            else if (hishaninare)
            {
                //指定した駒を飛車にする
                if (UnitType.Gyoku == unit.UnitType || UnitType.Hisha == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }
                //飛車に変える処理(内部データもオブジェクトも
                Destroy(unit.gameObject);
                Vector3 pos = unit.gameObject.transform.position;

                GameObject prefabHisha = prefabUnits[2];
                GameObject hisha = Instantiate(prefabHisha, pos, Quaternion.Euler(90, unit.Player * 90, 0));
                hisha.AddComponent<Rigidbody>();

                UnitController unitctrl = hisha.AddComponent<UnitController>();
                unitctrl.Player = unit.Player;
                unitctrl.UnitType = UnitType.Hisha;
                //獲られたときもとにもどるよう
                unitctrl.OldUnitType = UnitType.Hisha;
                //場所の初期化
                unitctrl.FieldStatus = FieldStatus.OnBoard;
                //角度と場所
                unitctrl.transform.eulerAngles = unitctrl.getDefaultAngles(unit.Player);
                unitctrl.Pos = unit.Pos;


                //ユニットデータセット
                units[unit.Pos.x, unit.Pos.y] = unitctrl;

                //強化中リストから除外
                if (isseikyoukatyu[unit.Player].Contains(unit))
                {
                    isseikyoukatyu[unit.Player].Remove(unit);
                }

                unit = null;
                hishaninare = false;
                textResultInfo.text = "";


                return;
            }
            //角になれ
            else if (kakuninare)
            {
                //指定した駒を角にする
                if (UnitType.Gyoku == unit.UnitType || UnitType.Kaku == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }
                //角に変える処理(内部データもオブジェクトも
                Destroy(unit.gameObject);
                Vector3 pos = unit.gameObject.transform.position;

                GameObject prefabKaku = prefabUnits[1];
                GameObject kaku = Instantiate(prefabKaku, pos, Quaternion.Euler(90, unit.Player * 90, 0));
                kaku.AddComponent<Rigidbody>();

                UnitController unitctrl = kaku.AddComponent<UnitController>();
                unitctrl.Player = unit.Player;
                unitctrl.UnitType = UnitType.Kaku;
                //獲られたときもとにもどるよう
                unitctrl.OldUnitType = UnitType.Kaku;
                //場所の初期化
                unitctrl.FieldStatus = FieldStatus.OnBoard;
                //角度と場所
                unitctrl.transform.eulerAngles = unitctrl.getDefaultAngles(unit.Player);
                unitctrl.Pos = unit.Pos;


                //ユニットデータセット
                units[unit.Pos.x, unit.Pos.y] = unitctrl;

                //強化中リストから除外
                if (isseikyoukatyu[unit.Player].Contains(unit))
                {
                    isseikyoukatyu[unit.Player].Remove(unit);
                }

                unit = null;
                kakuninare = false;
                textResultInfo.text = "";

                return;
            }


            //変身
            else if (henshin)
            {
                //指定した駒をランダムな駒にする
                if (UnitType.Gyoku == unit.UnitType  || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }
                //駒を変える処理(内部データもオブジェクトも
                Destroy(unit.gameObject);
                Vector3 pos = unit.gameObject.transform.position;
                int randomNum = Random.Range(0,7);
                GameObject prefabRandom = prefabUnits[randomNum];
                GameObject randomUnit = Instantiate(prefabRandom, pos, Quaternion.Euler(90, unit.Player * 90, 0));
                randomUnit.AddComponent<Rigidbody>();

                UnitController unitctrl = randomUnit.AddComponent<UnitController>();
                unitctrl.Player = unit.Player;
                unitctrl.UnitType = (UnitType)randomNum+1;
                //獲られたときもとにもどるよう
                unitctrl.OldUnitType = unit.UnitType;
                //場所の初期化
                unitctrl.FieldStatus = FieldStatus.OnBoard;
                //角度と場所
                unitctrl.transform.eulerAngles = unitctrl.getDefaultAngles(unit.Player);
                unitctrl.Pos = unit.Pos;

                //ユニットデータセット
                units[unit.Pos.x, unit.Pos.y] = unitctrl;

                //強化中リストから除外
                if (isseikyoukatyu[unit.Player].Contains(unit))
                {
                    isseikyoukatyu[unit.Player].Remove(unit);
                }

                unit = null;
                henshin = false;
                textResultInfo.text = "";


                return;
            }
            //入れ替え
            else if (irekae)
            {
                if (!firstSelected)
                {
                    if (unit.Player != nowPlayer || unit.FieldStatus == FieldStatus.Captured)
                    {
                        return;
                    }
                    unit1 = unit;

                    firstSelected = true;
                    textResultInfo.text = "入れ替える駒を選択(1/2)";
                }

                else
                {
                    if (unit.Player != nowPlayer || unit.FieldStatus == FieldStatus.Captured)
                    {
                        return;
                    }

                    UnitController unit2 = unit;

                    if ( unit2 != null )
                    {
                        //位置交換
                        units[unit1.Pos.x,unit1.Pos.y] = unit2;
                        units[unit2.Pos.x, unit2.Pos.y] = unit1;

                        Vector2Int tempPos1 = unit1.Pos;

                        units[unit2.Pos.x, unit2.Pos.y].Pos = unit2.Pos;
                        units[tempPos1.x, tempPos1.y].Pos = tempPos1;

                        //ゲームオブジェクトの交換
                        Vector3 tempObjectPos = unit1.gameObject.transform.position;
                        unit1.gameObject.transform.position = unit2.gameObject.transform.position;
                        unit2.gameObject.transform.position = tempObjectPos;

                        irekae = false;
                        textResultInfo.text = "";
                        unit1 = null;
                        firstSelected = false;
                    }

                }

                return;
            }
            //催眠術
            else if (saiminjutu)
            {
                setSelectCursors(unit, true);

                return;
            }

            bool isPlayer = nowPlayer == unit.Player;
            setSelectCursors(unit, isPlayer);
        }
    }

    //ターン変更
    void turnChangeMode()
    {
        //ボタンとカーソルのリセット
        setSelectCursors();
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        cardsDirector.buttonUseCard.gameObject.SetActive(false);

        //2回行動
        if (nikaikoudou)
        {
            //カード使用フラグは元に戻さない
            nextMode = Mode.Select;
            nikaikoudou = false;
            //コンピューターの思考時間をリセット
            if (isCpu)
            {
                enemyWaitTimer = Random.Range(1, EnemyWaitTimerMax);
            }
            return;
        }

        //カード使用フラグを元に戻す
        cardsDirector.usedFlag = false;

        //使用した枚数返す
        if (nowPlayer >= 0)
        {
            cardsDirector.DealCards(nowPlayer);
        }

        //一斉強化カード処理
        if (nowPlayer >= 0 && isseikyoukaTurn[nowPlayer] > 0)
        {
            isseikyoukaTurn[nowPlayer]--;
            if (isseikyoukaTurn[nowPlayer] == 0)
            {
                foreach (var item in isseikyoukatyu[nowPlayer])
                {
                    item.Evolution(false);
                }
                isseikyoukatyu[nowPlayer].Clear();
            }
        }

        //CPU状態解除
        isCpu = false;

        //次のプレイヤーへ
        nowPlayer = GetNextPlayer(nowPlayer);


        //経過ターン
        if (0 == nowPlayer)
        {
            turnCount++;
        }
        nextMode = Mode.Start;
    }

    //次のプレイヤー番号を返す
    public static int GetNextPlayer(int player)
    {
        int next;
        if (reverse)
        {
            next = player - 1;
            if (next < 0) next = 3;
            return next;
        }
        next = player + 1;
        if (PlayerMax <= next) next = 0;

        return next;
    }

    //自分以外のプレイヤー番号を返す
    public static List<int> GetOtherPlayer(int player)
    {
        List<int> ret = new List<int>();
        int nextplayer = player;
        for (int i = 0; i < 3; i++)
        {
            nextplayer = GetNextPlayer(nextplayer);
            ret.Add(nextplayer);
        }

        return ret;
    }

    //敵の駒を獲る
    void captureUnit(int player, Vector2Int tileindex)
    {
        UnitController unit = units[tileindex.x, tileindex.y];
        if (!unit) return;
        unit.Caputure(player);
        if (unit.UnitType != UnitType.Gyoku) 
        {
            captureUnits.Add(unit); 
        }
        else//玉を獲ったら
        {
            Destroy(unit.gameObject);//玉のオブジェクトを削除

            //手札を二枚追加
            cardsDirector.AddCards(player, 2);

            print((player+1) + "Pが" + (unit.Player+1) + "Pを撃破！");

        }
        units[tileindex.x, tileindex.y] = null;
    }

    //持ち駒を並べる
    void alignCaptureUnits(int player)
    {
        //所持個数をいったん非表示
        foreach (var item in unitTiles[player])
        {
            item.SetActive(false);
        }

        //ユニットごとに分ける
        Dictionary<UnitType, List<UnitController>> typeunits = new Dictionary<UnitType, List<UnitController>>();

        foreach (var item in captureUnits)
        {
            if (player != item.Player) continue;
            typeunits.TryAdd(item.UnitType, new List<UnitController>());
            typeunits[item.UnitType].Add(item);
        }

        //タイプごとに並べて一番上だけ表示する
        int tilecount = 0;
        foreach (var item in typeunits)
        {
            if (1 > item.Value.Count) continue;

            //置く場所
            GameObject tile = unitTiles[player][tilecount++];

            //非表示にしていたタイルを表示する
            tile.SetActive(true);

            //所持個数の表示
            tile.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "" + item.Value.Count;

            //同じ種類の持ち駒を並べる
            for (int i = 0; i < item.Value.Count; i++)
            {
                //リスト内のユニットを表示
                GameObject unit = item.Value[i].gameObject;
                //置く場所
                Vector3 pos = tile.transform.position;
                //一旦ユニットを移動して表示する
                unit.SetActive(true);
                unit.transform.position = pos;
                //1個目以外は非表示
                if (0 < i) unit.SetActive(false);
            }
        }
    }

    //指定された配列をコピーして返す
    public static UnitController[,] GetCopyArray(UnitController[,] ary)
    {
        UnitController[,] ret = new UnitController[ary.GetLength(0), ary.GetLength(1)];
        Array.Copy(ary, ret, ary.Length);
        return ret;
    }

    //指定された配置で王手しているユニットを返す 引数のplayerは王手されているプレイヤー番号
    public static List<UnitController> GetOuteUnitsUke(UnitController[,] units, int player, bool checkotherunit = true)
    {
        List<UnitController> ret = new List<UnitController>();

        foreach (var unit in units)
        {
            //仲間のユニットだったら
            if (!unit || player == unit.Player) continue;

            //ユニットの移動可能範囲
            List<Vector2Int> movabletiles = unit.GetMovableTiles(units, checkotherunit);

            foreach (var tile in movabletiles)
            {
                //ユニットがいなければ
                if (!units[tile.x, tile.y]) continue;

                if (UnitType.Gyoku == units[tile.x, tile.y].UnitType && units[tile.x, tile.y].Player == player)
                {
                    ret.Add(unit);
                }
            }
        }

        return ret;
    }

    //指定された配置で王手されているプレイヤーを返す 引数のplayerは王手しているプレイヤー番号
    public static List<int> GetOuteUnitsSeme(UnitController[,] units, int player, bool checkotherunit = true)
    {
        List<int> ret = new List<int>();

        foreach (var unit in units)
        {
            //敵のユニットだったら
            if (!unit || player != unit.Player) continue;

            //ユニットの移動可能範囲
            List<Vector2Int> movabletiles = unit.GetMovableTiles(units, checkotherunit);

            foreach (var tile in movabletiles)
            {
                //ユニットがいなければ
                if (!units[tile.x, tile.y]) continue;

                if (UnitType.Gyoku == units[tile.x, tile.y].UnitType && units[tile.x, tile.y].Player != player)
                {
                    ret.Add(units[tile.x, tile.y].Player);
                }
            }
        }

        return ret;
    }

    //成るボタン
    public void OnClickEvolutionApply()
    {
        textResultInfo.text = "";
        nextMode = Mode.TurnChange;
    }

    //成らないボタン
    public void OnClickEvolutionCancel()
    {
        selectUnit.Evolution(false);
        OnClickEvolutionApply();
    }

    //指定されたプレイヤー番号の全ユニットを取得する
    List<UnitController> getUnits(int player)
    {
        List<UnitController> ret = new List<UnitController>();

        //全ユニットのリストを作成する
        List<UnitController> allunits = new List<UnitController>(captureUnits);
        allunits.AddRange(units);
        foreach (var item in allunits)
        {
            if (!item || player != item.Player) continue;
            ret.Add(item);
        }

        return ret;
    }

    //カードの対象として選択できるユニットを取得する
    public List<UnitController> GetUnitsForCard(CardType cardType)
    {
        List<UnitController> ret = new List<UnitController>();
        foreach (var item in units)
        {
            //自駒かつ成り可能駒
            if (CardType.ikusei == cardType) 
            {
                if (!item || nowPlayer != item.Player || !item.isEvolution()) continue;
            }
            //敵駒かつ玉以外
            else if (CardType.uragiri == cardType || CardType.saiminjutu== cardType)
            {
                if (!item || nowPlayer == item.Player || UnitType.Gyoku == item.UnitType) continue;
            }
            //敵駒かつ玉、雑魚駒以外
            else if (CardType.huninare == cardType)
            {
                if (!item || nowPlayer == item.Player || UnitType.Gyoku == item.UnitType || UnitType.Hu == item.UnitType || UnitType.Keima == item.UnitType || UnitType.Kyousha == item.UnitType ) continue;
            }

            //自駒かつ玉,強い駒以外
            else if (CardType.kakuninare == cardType || CardType.hishaninare == cardType || CardType.henshin == cardType )
            {
                if (!item || nowPlayer != item.Player || UnitType.Gyoku == item.UnitType || UnitType.Kaku == item.UnitType || UnitType.Hisha == item.UnitType || UnitType.Uma == item.UnitType || UnitType.Ryu == item.UnitType) continue;
            }

            //自駒(移動できない駒を含む)
            if (CardType.irekae == cardType) 
            {
                if (!item || nowPlayer != item.Player) continue;
            }

            ret.Add(item);
        }
        return ret;
    }

    //カードが使用可能か調べる
    public bool isCanUseCard(CardType cardType)
    {
        if (GetUnitsForCard(cardType).Count == 0) return false;
        return true;
    } 

    //カードを使用
    public void UseCard(CardType cardType, int player)
    {
        string str = (player + 1) + "Pが" + CardName[cardType] + "使用";
        print(str);
        GameObject obj = Instantiate(textUsedCardLog, parentObj.transform);
        obj.GetComponent<Text>().text = str;

        //カード使用時、カーソルをリセット
        setSelectCursors();

        if (CardType.Zyunbantobashi == cardType)
        {
            zyunbantobashi = true;
        }

        else if (CardType.ichimaituika == cardType)
        {
            cardsDirector.AddCards(nowPlayer, 1);
        }

        else if (CardType.reverse == cardType)
        {
            if (!reverse) reverse = true;
            else reverse = false;

        }

        else if (CardType.komaget == cardType)
        {
            int unittype = Random.Range(1, 8);

            GameObject prefab = prefabUnits[unittype - 1];
            GameObject unit = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.Euler(90, player * 90, 0));
            unit.AddComponent<Rigidbody>();
            UnitController unitctrl = unit.AddComponent<UnitController>();
            unitctrl.Player = player;
            unitctrl.UnitType = (UnitType)unittype;
            unitctrl.OldUnitType = (UnitType)unittype;
            unitctrl.FieldStatus = FieldStatus.Captured;
            unitctrl.transform.eulerAngles = unitctrl.getDefaultAngles(player);
            unitctrl.Caputure(player);
            captureUnits.Add(unitctrl);
            alignCaptureUnits(player);
        }

        else if (CardType.nikaikoudou == cardType)
        {
            nikaikoudou = true;
        }
        
        else if (CardType.ikusei == cardType)
        {
            ikusei = true;
            textResultInfo.text = "成らせる自駒を選択";
        }

        else if (CardType.uragiri == cardType)
        {
            uragiri = true;
            textResultInfo.text = "自駒に変える敵駒を選択";
        }

        else if (CardType.isseikyouka == cardType)
        {
            //一斉強化ターンを更新
            isseikyoukaTurn[player] = 3;
            //対象の駒を成らせてリストに入れる
            foreach (var item in getUnits(player))
            {
                if (item.isEvolution() && FieldStatus.OnBoard == item.FieldStatus)
                {
                    item.Evolution();
                    isseikyoukatyu[player].Add(item);
                }
            }
        }

        else if (CardType.huninare == cardType)
        {
            huninare = true;
            textResultInfo.text = "歩に変える駒を選択";
        }

        else if (CardType.henshin == cardType)
        {
            henshin = true;
            textResultInfo.text = "変身する駒を選択";
        }

        else if (CardType.irekae == cardType)
        {
            irekae = true;
            textResultInfo.text = "入れ替える駒を選択(0/2)";
        }

        else if (CardType.hishaninare == cardType)
        {
            hishaninare = true;
            textResultInfo.text = "飛車に変える駒を選択";
        }

        else if (CardType.kakuninare == cardType)
        {
            kakuninare = true;
            textResultInfo.text = "角に変える駒を選択";
        }
        else if (CardType.saiminjutu == cardType)
        {
            saiminjutu = true;
        }
        else if (CardType.cardReset == cardType)
        {
            int cardnum = cardsDirector.playerCards[player].Count;
            if (player == 0) cardsDirector.DestroyCards(player);
            cardsDirector.playerCards[player].Clear();
            cardsDirector.AddCards(player, cardnum, true);
        }
    }

    //リザルト再戦
    public void OnClickRematch()
    {
        SceneManager.LoadScene("GameScene");
    }

    //リザルトタイトルへ
    public void OnClickTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
