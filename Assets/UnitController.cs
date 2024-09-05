using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

//駒のタイプ
public enum UnitType
{
    None=-1,
    Hu= 1,
    Kaku,
    Hisha,
    Kyousha,
    Keima,
    Gin,
    Kin,
    Gyoku,
    //成り
    Tokin,
    Uma,
    Ryu,
    NariKyo,
    Narikei,
    Narigin,
}

//駒の場所
public enum FieldStatus
{
    OnBoard,
    Captured,
}



public class UnitController : MonoBehaviour
{
    //ユニットのプレイヤー番号 1P: 0, 2P: 1, 3P: 2, 4P: 3
    public int Player;
    //ユニットの種類
    public UnitType UnitType, OldUnitType;
    //ユニットの場所
    public FieldStatus FieldStatus;

    //成りテーブル
    Dictionary<UnitType, UnitType> evolutionTable = new Dictionary<UnitType, UnitType>()
    {
        {UnitType.Hu, UnitType.Tokin },
        {UnitType.Kaku, UnitType.Uma },
        {UnitType.Hisha, UnitType.Ryu },
        {UnitType.Keima, UnitType.Narikei },
        {UnitType.Kyousha, UnitType.NariKyo },
        {UnitType.Gin, UnitType.Narigin },
        {UnitType.Kin, UnitType.None},
        {UnitType.Gyoku, UnitType.None },

    };

    //成り済みかどうか
    public bool isEvolution;

    //ユニットの選択/非選択のy座標
    public const float SelectUnitY = 1.5f;
    public const float UnSelectUnitY = 0.7f;

    //おいている場所
    public Vector2Int Pos;

    //選択される前のy座標
    float oldPosY;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //初期設定
    public void Init(int player,int unittype,GameObject tile, Vector2Int pos)
    {
        Player = player;
        UnitType = (UnitType)unittype;
        //獲られたときもとにもどるよう
        OldUnitType = (UnitType)unittype;
        //場所の初期化
        FieldStatus = FieldStatus.OnBoard;
        //確度と場所
        transform.eulerAngles = getDefaultAngles(player);
        Move(tile, pos);

    }
    //指定されたプレイヤー番号の向きを返す
    Vector3 getDefaultAngles(int player)
    {
        return new Vector3(90, player * 90, 0);
    }

    //移動する処理
    public void Move(GameObject tile, Vector2Int tileindex)
    {
        //新しい場所に非選択状態で移動する
        Vector3 pos = tile.transform.position;
        pos.y = UnSelectUnitY;
        transform.position = pos;

        Pos = tileindex;
    }

    public void Select(bool select = true)
    {
        Vector3 pos = transform.position;
        bool iskinematic = select;

        if (select)
        {
            oldPosY = pos.y;
            pos.y = SelectUnitY;
        }
        else
        {
            pos.y = UnSelectUnitY;

            //持ちゴマの位置は特別
            if (FieldStatus.Captured == FieldStatus)
            {
                pos.y = oldPosY;
                iskinematic = true;
            }
        }

        GetComponent<Rigidbody>().isKinematic = iskinematic;
        transform.position = pos;
    }

    //移動可能範囲取得
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkotherunit = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        ret = GetMovableTiles(units, UnitType.Hu);

        return ret;
    }

    List<Vector2Int> GetMovableTiles(UnitController[,] units, UnitType unittype)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //歩
        if (UnitType.Hu == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            Vector2Int dir = new Vector2Int(0, 1);

            if (Player == 1) dir = new Vector2Int(1, 0);
            if (Player == 2) dir = new Vector2Int(0, -1);
            if (Player == 3) dir = new Vector2Int(-1, 0);

            //前方1マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                dir
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }
        }

        return ret;
    }

    //配列オーバーかどうか
    bool isCheckable(UnitController[,] ary, Vector2Int idx)
    {
        //配列オーバーの状態
        if (idx.x < 0 || ary.GetLength(0) <= idx.x || idx.y < 0 || ary.GetLength(1) <= idx.y)
        {
            return false;
        }
        return true;
    }

    //仲間のユニットかどうか
    bool isFriendlyUnit(UnitController unit)
    {
        if (unit && Player == unit.Player) return true;
        return false;
    }
}
