using Photon.Pun.UtilityScripts;
using Photon.Pun;
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
using Photon.Realtime;
using Random = UnityEngine.Random;

public class MultiGameSceneDirector : MonoBehaviourPunCallbacks, IPunTurnManagerCallbacks
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
    UnitController selectUnit;

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
    public bool myturn;

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

    //敵陣設定
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    //カードのフラグ初期化
    bool zyunbantobashi = false;
    bool nikaikoudou = false;
    bool huninare = false;
    bool ikusei = false;
    bool uragiri = false;
    bool henshin = false;

    //一斉強化カードの管理
    public List<UnitController>[] isseikyoukatyu = new List<UnitController>[4];
    public int isseikyoukaTurn = 0;

    //サウンド制御
    [SerializeField] SoundController sound;

    //プレイヤーの詰み状態
    bool[] istumi;

    int tumicount;

    //プレイヤー名
    string[] Players;

    //PUN
    [Header("PUN")]
    PunTurnManager punTurnManager = default;

    //カメラ
    [SerializeField]
    Camera Scenecamera;

    [SerializeField] MultiCardsDirector multiCardsDirector;

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

    void SetupTurnManager()
    {
        punTurnManager = GetComponent<PunTurnManager>();
        punTurnManager.enabled = true;
        punTurnManager.TurnManagerListener = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetupTurnManager(); //PunTurnManagerが機能するようにする

        //カメラ
        Vector3 cameraPos;

        if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            cameraPos.x = -5.0f;
            cameraPos.y = 11.0f;
            cameraPos.z = 0.0f;
            Scenecamera.transform.position = cameraPos;

            Scenecamera.gameObject.transform.rotation = Quaternion.Euler(70, 90, 0);
        }
        if (PhotonNetwork.LocalPlayer.ActorNumber == 3)
        {
            cameraPos.x = 0.0f;
            cameraPos.y = 11.0f;
            cameraPos.z = 5.0f;
            Scenecamera.transform.position = cameraPos;

            Scenecamera.gameObject.transform.rotation = Quaternion.Euler(70, -180, 0);
        }
        if (PhotonNetwork.LocalPlayer.ActorNumber == 4)
        {
            cameraPos.x = 5.0f;
            cameraPos.y = 11.0f;
            cameraPos.z = 0.0f;
            Scenecamera.transform.position = cameraPos;

            Scenecamera.gameObject.transform.rotation = Quaternion.Euler(70, -90, 0);
        }

        //BGM再生　うるさいので消しておく
        //sound.PlayBGM(0);

        //UI関連初期設定
        buttonTitle.gameObject.SetActive(false);
        buttonRematch.gameObject.SetActive(false);
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        multiCardsDirector.buttonUseCard.gameObject.SetActive(false);
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

        //プレイヤー
        Players = new string[4];

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
        multiCardsDirector.playerCards = new List<CardController>();

        multiCardsDirector.AddCards(PhotonNetwork.LocalPlayer.ActorNumber - 1, 3);//初期枚数の設定

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

        //isseikyoukatyuのリストの初期化
        for (int i = 0; i < isseikyoukatyu.Length; i++)
        {
            isseikyoukatyu[i] = new List<UnitController>();
        }

        //初回モード
        nowMode = Mode.None;
        nextMode = Mode.None;
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

        if (tiles[tileindex] == null)
        {
            print("tileindexがnull");
            return ret;
        }

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
                        if (myturn)
                        {
                            //成った状態を表示
                            unit.Evolution();
                            setSelectCursors(unit);
                            print(unit.UnitType);

                            //ナビゲーション
                            textResultInfo.text = "成りますか？";
                            buttonEvolutionApply.gameObject.SetActive(true);
                            buttonEvolutionCancel.gameObject.SetActive(true);
                        }

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
                        if (myturn)
                        {
                            //成った状態を表示
                            unit.Evolution();
                            setSelectCursors(unit);
                            print(unit.UnitType);

                            //ナビゲーション
                            textResultInfo.text = "成りますか？";
                            buttonEvolutionApply.gameObject.SetActive(true);
                            buttonEvolutionCancel.gameObject.SetActive(true);
                        }

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
        print("現在のプレイヤー" + (nowPlayer + 1) + "P");

        //勝敗がついていなければ通常モード
        nextMode = Mode.Select;

        //順番飛ばし処理
        if (zyunbantobashi)
        {
            photonView.RPC(nameof(SkipFalse), RpcTarget.All, 1);
            StartCoroutine(FinishPlaying());
            return;
        }

        //脱落してたら即次の人へ
        if (istumi[nowPlayer])
        {
            StartCoroutine(FinishPlaying());
            return;
        }
        else
        {
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
                print(nowPlayer + "P脱落");
                istumi[nowPlayer] = true;
                tumicount++;
                StartCoroutine(FinishPlaying());
                return;
            }
        }

        //プレイヤーのターンかつカードを選択しているならカード選択ボタンを表示
        if (myturn && multiCardsDirector.selectCard)
        {
            //カードの効果を使えない場合ボタンを押せない状態にして表示
            multiCardsDirector.buttonUseCard.interactable = isCanUseCard(multiCardsDirector.selectCard.CardType);
            multiCardsDirector.buttonUseCard.gameObject.SetActive(true);
        }

        //Info更新
        textTurnInfo.text = "" + Players[nowPlayer] + "さんの番です";
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
                    textResultInfo.text = "詰み\n" + Players[i] + "さんの勝ち";
                    break;
                }
            }

            nextMode = Mode.Result;
        }

        //次が結果表示画面なら
        if (Mode.Result == nextMode)
        {
            textTurnInfo.text = "";
            buttonTitle.gameObject.SetActive(true);
        }

        print("startMode終了");
        print(nextMode);
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

        //何も選択されていなければ処理をしない
        if (null == tile && null == unit) return;

        //移動先選択
        if (myturn && tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            if (selectUnit.FieldStatus == FieldStatus.Captured)
            {
                int capturedunitpos = CapturedUnitPosition(selectUnit);
                photonView.RPC(nameof(CapturedSetSelectUnit), RpcTarget.All, capturedunitpos);
            }
            else
            {
                int[] unitpos = UnitPosition(selectUnit);
                photonView.RPC(nameof(SetSelectUnit), RpcTarget.All, unitpos);
            }
            object[] data = new object[] { movableTiles[tile].x, movableTiles[tile].y };
            punTurnManager.SendMove(data, false);
        }

        //ユニット選択
        else if (unit)
        {
            //歩になれ！
            if (huninare)
            {
                //選択した駒が玉か歩または持ち駒だった場合は終了
                if (UnitType.Gyoku == unit.UnitType || UnitType.Hu == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }

                //歩に変える駒の盤上の位置を取得
                int[] unitpos = UnitPosition(unit);

                //歩に変える処理
                photonView.RPC(nameof(ChangeHu), RpcTarget.All, unitpos);

                unit = null;
                huninare = false;
                textResultInfo.text = "";

                return;
            }

            //育成
            else if (ikusei)
            {
                //選択した駒が現在のプレイヤーの駒じゃなかったら終了
                if (nowPlayer != unit.Player) return;
                //選択した駒が進化できないまたは持ち駒だった場合終了
                if (!unit.isEvolution() || unit.FieldStatus == FieldStatus.Captured) return;

                //育成する駒の盤上の位置を取得
                int[] unitpos = UnitPosition(unit);

                //駒を成らせる処理
                photonView.RPC(nameof(Ikusei), RpcTarget.All, unitpos);

                unit = null;
                ikusei = false;
                textResultInfo.text = "";

                return;
            }

            //裏切り
            else if (uragiri)
            {
                //選択した駒が現在のプレイヤーの駒だったら終了
                if (nowPlayer == unit.Player) return;
                //選択した駒が玉または持ち駒だった場合終了
                if (UnitType.Gyoku == unit.UnitType || unit.FieldStatus == FieldStatus.Captured) return;

                //選択した駒の盤上の位置を取得
                int[] unitpos = UnitPosition(unit);

                //裏切りの処理
                photonView.RPC(nameof(Uragiri), RpcTarget.All, unitpos);

                unit = null;
                uragiri = false;
                textResultInfo.text = "";

                StartCoroutine(FinishPlaying());

                return;
            }

            //変身
            else if (henshin)
            {
                //指定した駒が玉または持ち駒だった場合終了
                if (UnitType.Gyoku == unit.UnitType || unit.FieldStatus == FieldStatus.Captured)
                {
                    return;
                }

                //選択した駒の盤上の位置を取得
                int[] unitpos = UnitPosition(unit);

                //ランダムで駒のタイプを取得
                int randomNum = Random.Range(0, 7);

                //変身の処理
                photonView.RPC(nameof(Henshin), RpcTarget.All, unitpos, randomNum);

                unit = null;
                henshin = false;
                textResultInfo.text = "";

                return;
            }

            bool isPlayer = false;
            if (myturn && nowPlayer == unit.Player) isPlayer = true;
            setSelectCursors(unit, isPlayer);
        }
    }

    //ターン変更
    void turnChangeMode()
    {
        print("turnChangeModeコール");
        //ボタンとカーソルのリセット
        setSelectCursors();
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);

        //次のプレイヤーへ
        nowPlayer = GetNextPlayer(nowPlayer);

        nextMode = Mode.Start;
    }

    //次のプレイヤー番号を返す
    public static int GetNextPlayer(int player)
    {
        int next = player + 1;
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

    void captureUnit(int player, Vector2Int tileindex)
    {
        UnitController unit = units[tileindex.x, tileindex.y];
        if (!unit) return;
        unit.Caputure(player);
        captureUnits.Add(unit);
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

    //ユニットの位置を返す関数
    int[] UnitPosition(UnitController unit)
    {
        int[] ret = new int[2];

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                if (units[i, j] == unit)
                {
                    ret[0] = i;
                    ret[1] = j;
                    return ret;
                }
            }
        }

        return ret;
    }

    //持ち駒の位置を返す関数
    int CapturedUnitPosition(UnitController unit)
    {
        int ret = 0;

        for (int i = 0; i < captureUnits.Count; i++)
        {
            if (captureUnits[i] == unit)
            {
                ret = i;
                return ret;
            }
        }

        return ret;
    }

    //成るボタン
    public void OnClickEvolutionApply()
    {
        textResultInfo.text = "";
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        photonView.RPC(nameof(Evolution), RpcTarget.Others, 1);
        StartCoroutine(FinishPlaying());
    }

    //成らないボタン
    public void OnClickEvolutionCancel()
    {
        textResultInfo.text = "";
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        photonView.RPC(nameof(EvolutionCancel), RpcTarget.All, 1);
        StartCoroutine(FinishPlaying());
    }

    //成る
    [PunRPC]
    void Evolution(int t)
    {
        selectUnit.Evolution();
    }

    //成りキャンセル
    [PunRPC]
    void EvolutionCancel(int t)
    {
        selectUnit.Evolution(false);
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

    //モードを変更する関数
    [PunRPC]
    void SetMode(int mode)
    {
        print("モード" + mode + "にセット");
        nextMode = (Mode)mode;
    }

    //歩になれ
    [PunRPC]
    void ChangeHu(int[] unitpos)
    {
        if (unitpos[0] < 0 || boardWidth <= unitpos[0] || unitpos[1] < 0 || boardHeight <= unitpos[1]) return;

        UnitController unit = units[unitpos[0], unitpos[1]];
        //歩に変える処理(内部データもオブジェクトも
        Destroy(unit.gameObject);
        Vector3 pos = unit.gameObject.transform.position;

        GameObject prefabHu = prefabUnits[0];
        GameObject hu = Instantiate(prefabHu, pos, Quaternion.Euler(90, unit.Player * 90, 0));
        Rigidbody rigidbody =  hu.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        UnitController unitctrl = hu.AddComponent<UnitController>();
        unitctrl.Player = unit.Player;
        unitctrl.UnitType = UnitType.Hu;
        //獲られたときもとにもどるよう
        unitctrl.OldUnitType = UnitType.Hu;
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
    }

    //育成
    [PunRPC]
    void Ikusei(int[] unitpos)
    {
        if (unitpos[0] < 0 || boardWidth <= unitpos[0] || unitpos[1] < 0 || boardHeight <= unitpos[1]) return;

        UnitController unit = units[unitpos[0], unitpos[1]];

        //選択した駒を成らせる
        unit.Evolution();
    }

    //裏切り
    [PunRPC]
    public void Uragiri(int[] unitpos)
    {
        if (unitpos[0] < 0 || boardWidth <= unitpos[0] || unitpos[1] < 0 || boardHeight <= unitpos[1]) return;

        UnitController unit = units[unitpos[0], unitpos[1]];

        //裏切りの処理
        unit.Player = nowPlayer;
        unit.transform.eulerAngles = unit.getDefaultAngles(nowPlayer);
    }

    //変身
    [PunRPC]
    public void Henshin(int[] unitpos, int randomNum)
    {
        if (unitpos[0] < 0 || boardWidth <= unitpos[0] || unitpos[1] < 0 || boardHeight <= unitpos[1]) return;

        UnitController unit = units[unitpos[0], unitpos[1]];

        //変身の処理
        Destroy(unit.gameObject);
        Vector3 pos = unit.gameObject.transform.position;

        GameObject prefabRandom = prefabUnits[randomNum];
        GameObject randomUnit = Instantiate(prefabRandom, pos, Quaternion.Euler(90, unit.Player * 90, 0));
        Rigidbody rigidbody = randomUnit.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        UnitController unitctrl = randomUnit.AddComponent<UnitController>();
        unitctrl.Player = unit.Player;
        unitctrl.UnitType = (UnitType)(randomNum + 1);
        //獲られたときもとにもどるよう
        unitctrl.OldUnitType = (UnitType)(randomNum + 1);
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
    }

    //ターンを終了する関数 一瞬時間を空ける
    IEnumerator FinishPlaying()
    {
        print("FinishPlayingコール");
        if (myturn)
        {
            if (nikaikoudou)
            {
                setSelectCursors();
                multiCardsDirector.buttonUseCard.gameObject.SetActive(false);
                photonView.RPC(nameof(NikaikoudouFalse), RpcTarget.All, 1);
            }
            else
            {
                myturn = false;
                yield return new WaitForSeconds(0.2f);
                multiCardsDirector.buttonUseCard.gameObject.SetActive(false);

                //カード使用フラグを元に戻す
                multiCardsDirector.usedFlag = false;

                //使用した枚数返す
                multiCardsDirector.DealCards(nowPlayer);

                //一斉強化カード処理
                if (isseikyoukaTurn > 0)
                {
                    isseikyoukaTurn--;
                    if (isseikyoukaTurn == 0)
                    {
                        photonView.RPC(nameof(IsseikyoukaCancel), RpcTarget.All, nowPlayer);
                    }
                }

                punTurnManager.SendMove(null, true); //trueで手番終了を送信
            }
        }
    }

    //selectUnitをセットする関数
    [PunRPC]
    void SetSelectUnit(int[] unitpos)
    {
        if (unitpos[0] < 0 || boardWidth <= unitpos[0] || unitpos[1] < 0 || boardHeight <= unitpos[1]) return;
        selectUnit = units[unitpos[0], unitpos[1]];
    }

    //持ち駒だった場合のselectUnitをセットする関数
    [PunRPC]
    void CapturedSetSelectUnit(int unitpos)
    {
        if (unitpos < 0 || captureUnits.Count <= unitpos) return;
        selectUnit = captureUnits[unitpos];
    }

    //カードを使用
    public void UseCard(CardType cardType, int player)
    {
        string str = Players[player] + "さんが" + CardName[cardType] + "使用";
        print(str);
        photonView.RPC(nameof(DisplayUsedCardLog), RpcTarget.All, str);

        if (CardType.Zyunbantobashi == cardType)
        {
            photonView.RPC(nameof(SkipTrue), RpcTarget.All, 1);
        }

        else if (CardType.ichimaituika == cardType)
        {
            multiCardsDirector.AddCards(player, 1);
        }

        else if (CardType.komaget == cardType)
        {
            int unittype = Random.Range(1, 8);

            photonView.RPC(nameof(GetKoma), RpcTarget.All, unittype, nowPlayer);
        }

        else if (CardType.nikaikoudou == cardType)
        {
            photonView.RPC(nameof(NikaikoudouTrue), RpcTarget.All, 1);
        }

        else if (CardType.isseikyouka == cardType)
        {
            //一斉強化ターンを更新
            isseikyoukaTurn = 3;
            //対象の駒を成らせてリストに入れる
            photonView.RPC(nameof(Isseikyouka), RpcTarget.All, nowPlayer);
        }

        else if (CardType.huninare == cardType){
            huninare = true;
            textResultInfo.text = "歩に変える駒を選択";
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

        else if (CardType.henshin == cardType)
        {
            henshin = true;
            textResultInfo.text = "変身する駒を選択";
        }
    }

    //リザルトタイトルへ
    public void OnClickTitle()
    {
        PhotonNetwork.LeaveRoom(); //ルームから出る
    }

    [PunRPC]
    void setPlayers(int t)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            Players[p.ActorNumber - 1] = p.NickName;
        }
    }

    //順番飛ばしのフラグをfalseにする関数
    [PunRPC]
    public void SkipFalse(int t)
    {
        zyunbantobashi = false;
    }

    //順番飛ばしのフラグをtrueにする関数
    [PunRPC]
    public void SkipTrue(int t)
    {
        zyunbantobashi = true;
    }

    //二回行動のフラグをfalseにする関数
    [PunRPC]
    public void NikaikoudouFalse(int t)
    {
        nikaikoudou = false;
    }

    //二回行動のフラグをtrueにする関数
    [PunRPC]
    public void NikaikoudouTrue(int t)
    {
        nikaikoudou = true;
    }

    //一斉強化カード使用時に対象の駒を成らせてリストに入れる関数
    [PunRPC]
    public void Isseikyouka(int player)
    {
        foreach (var item in getUnits(player))
        {
            if (item.isEvolution() && FieldStatus.OnBoard == item.FieldStatus)
            {
                item.Evolution();
                isseikyoukatyu[player].Add(item);
            }
        }
    }

    //一斉強化された駒の成りを解除しリストを空にする関数
    [PunRPC]
    public void IsseikyoukaCancel(int player)
    {
        foreach (var item in isseikyoukatyu[player])
        {
            item.Evolution(false);
        }
        isseikyoukatyu[player].Clear();
    }

    //駒を獲得する関数
    [PunRPC]
    public void GetKoma(int unittype, int player)
    {
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
            else if (CardType.uragiri == cardType || CardType.saiminjutu == cardType)
            {
                if (!item || nowPlayer == item.Player || UnitType.Gyoku == item.UnitType) continue;
            }
            //敵駒かつ玉、雑魚駒以外
            else if (CardType.huninare == cardType)
            {
                if (!item || nowPlayer == item.Player || UnitType.Gyoku == item.UnitType || UnitType.Hu == item.UnitType || UnitType.Keima == item.UnitType || UnitType.Kyousha == item.UnitType) continue;
            }

            //自駒かつ玉,強い駒以外
            else if (CardType.kakuninare == cardType || CardType.hishaninare == cardType || CardType.henshin == cardType)
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

    //カード使用ログに表示する関数
    [PunRPC]
    public void DisplayUsedCardLog(string str)
    {
        GameObject obj = Instantiate(textUsedCardLog, parentObj.transform);
        obj.GetComponent<Text>().text = str;
    }

    //ルームを出たときに呼ばれる関数
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("TitleScene"); //ルームを出たらTitleSceneへ
    }

    //プレイヤーが入室したら、そのプレイヤー以外で読まれる関数
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        print("Enter!!");
        Debug.Log("OnPlayerEnteredRoom: " + newPlayer.NickName);

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 4) //定員4に達したら
        {
            Debug.Log("start!");

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(setPlayers), RpcTarget.All, 1);
                punTurnManager.BeginTurn();//ターン開始
            }
        }
    }

    void IPunTurnManagerCallbacks.OnTurnBegins(int turn) //ターンの始まりに読まれる関数
    {
        print(turn + "ターン目開始！");

        // MasterClientが先手とする
        if (PhotonNetwork.IsMasterClient)
        {
            myturn = true;
            photonView.RPC(nameof(SetMode), RpcTarget.All, (int)Mode.TurnChange);
        }
    }

    //あるプレイヤーがターンを終了したとき全員に呼ばれる関数
    void IPunTurnManagerCallbacks.OnPlayerFinished(Photon.Realtime.Player player, int turn, object move)
    {
        print(player.NickName + "終了");
        // 自分が MasterClient ではない、かつ、現在のプレイヤーの次のプレイヤーの場合
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber + 1)
        {
            myturn = true;
            photonView.RPC(nameof(SetMode), RpcTarget.All, (int)Mode.TurnChange);
        }
    }

    void IPunTurnManagerCallbacks.OnPlayerMove(Photon.Realtime.Player player, int turn, object move) //SendMove関数を受けて読まれる関数
    {
        print("sendMove");

        object[] data = (object[])move;
        int nextposx = (int)data[0];
        int nextposy = (int)data[1];

        Vector2Int nextpos = new Vector2Int();
        nextpos.x = nextposx;
        nextpos.y = nextposy;

        Mode mode = moveUnit(selectUnit, nextpos);
        if (mode == Mode.TurnChange)
        {
            StartCoroutine(FinishPlaying());
        }
        else
        {
            if (myturn) nextMode = mode;
        }
    }

    void IPunTurnManagerCallbacks.OnTurnCompleted(int turn) //全員の手番が終わったら読まれる関数
    {
        print("全員のターン終了！");
        if (PhotonNetwork.IsMasterClient)
        {
            punTurnManager.BeginTurn();
        }
    }

    void IPunTurnManagerCallbacks.OnTurnTimeEnds(int turn) //ターンの制限時間が0になったら読まれる関数
    {
        punTurnManager.BeginTurn();
    }
}
