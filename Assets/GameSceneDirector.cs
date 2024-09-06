using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
    int nowPlayer;
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
                    posEven.x = (posEven.x + 1) * dir;
                    posEven.z = posEven.z * dir;

                    resultpos = posEven;
                }
                else
                {
                    Vector3 posOdd = startposOdd;
                    posOdd.x = posOdd.x * dir;
                    posOdd.z = (posOdd.z - 1) * dir;

                    resultpos = posOdd;
                }

                GameObject obj = Instantiate(prefabUnitTile, resultpos, Quaternion.Euler(0, 90 * i, 0));
                unitTiles[i].Add(obj);

                obj.SetActive(false);
            }
        }

        //TurnChangeから始める場合-1
        nowPlayer = -1;

        //初回モード
        nowMode = Mode.None;
        nextMode = Mode.TurnChange;
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
    void setSelectCursors(UnitController unit = null, bool playerunit = true)
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

        return ret;
    }

    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        //王手されてしまうかチェック

        return ret;
    }

    //ターン開始
    void startMode()
    {
        //勝敗がついていなければ通常モード
        nextMode = Mode.Select;

        //Info更新
        textTurnInfo.text = "" + (nowPlayer + 1) + "Pの番です";
        textResultInfo.text = "";

        //勝敗チェック

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
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            nextMode = moveUnit(selectUnit, movableTiles[tile]);
        }

        //ユニット選択
        if (unit)
        {
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
        int next = player + 1;
        if (PlayerMax <= next) next = 0;

        return next;
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
}
