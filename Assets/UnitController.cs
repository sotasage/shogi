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

        //持ち駒状態
        if(FieldStatus.Captured == FieldStatus)
        {
            //持ち駒状態
        }
        //玉
        else if(UnitType.Gyoku == UnitType)
        {
            //玉の移動範囲
        }
        //金と同じ動き
        else if (UnitType.Tokin == UnitType 
            || UnitType.NariKyo == UnitType
            || UnitType.Narikei == UnitType
            || UnitType.Narigin == UnitType)
        {
            ret = GetMovableTiles(units, UnitType.Kin);

        }
        //馬(玉+角）
        else if( UnitType.Uma == UnitType)
        {
            ret = GetMovableTiles(units, UnitType.Gyoku);
            foreach (var item in GetMovableTiles(units, UnitType.Kaku))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }

        }
        //龍(玉+角）
        else if (UnitType.Ryu == UnitType)
        {
            ret = GetMovableTiles(units, UnitType.Gyoku);
            foreach (var item in GetMovableTiles(units, UnitType.Hisha))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }

        }

        else
        {
            ret = GetMovableTiles(units, UnitType);

        }

        return ret;
    }

    List<Vector2Int> GetMovableTiles(UnitController[,] units, UnitType unittype)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        List<Vector2Int> dirs = new List<Vector2Int>();

        //歩
        if (UnitType.Hu == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            if (Player == 0)
            {
                dirs.Add(new Vector2Int(0, 1));
            }
            else if (Player == 1)
            {
                dirs.Add(new Vector2Int(1, 0));
            }
            else if (Player == 2)
            {
                dirs.Add(new Vector2Int(0, -1));
            }
            else if (Player == 3)
            {
                dirs.Add(new Vector2Int(-1, 0));
            }

            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }
        }
        //桂馬
        else if (UnitType.Keima== unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            if (Player == 0)
            {
                dirs.Add(new Vector2Int(1, 2));
                dirs.Add(new Vector2Int(-1, 2));
            }
            else if (Player == 1)
            {
                dirs.Add(new Vector2Int(2,1));
                dirs.Add(new Vector2Int(2,-1));

            }
            else if (Player == 2)
            {
                dirs.Add(new Vector2Int(1, -2));
                dirs.Add(new Vector2Int(-1, -2));

            }
            else if (Player == 3)
            {
                dirs.Add(new Vector2Int(-2, 1));
                dirs.Add(new Vector2Int(-2, -1));

            }

            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }

        }
        //銀
        else if (UnitType.Gin == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            if (Player == 0)
            {
                dirs.Add(new Vector2Int(-1, 1));
                dirs.Add(new Vector2Int(0, 1));
                dirs.Add(new Vector2Int(1, 1));
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(1, -1));

            }
            else if (Player == 1)
            {
                dirs.Add(new Vector2Int(1, -1));
                dirs.Add(new Vector2Int(1, 0));
                dirs.Add(new Vector2Int(1, 1));
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(-1, 1));

            }
            else if (Player == 2)
            {
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(0, -1));
                dirs.Add(new Vector2Int(1, -1));
                dirs.Add(new Vector2Int(-1, 1));
                dirs.Add(new Vector2Int(1, 1));

            }
            else if (Player == 3)
            {
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(-1, 0));
                dirs.Add(new Vector2Int(-1, 1));
                dirs.Add(new Vector2Int(1, -1));
                dirs.Add(new Vector2Int(1, 1));

            }

            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }

        }
        //金
        else if (UnitType.Kin == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            if (Player == 0)
            {
                dirs.Add(new Vector2Int(-1, 1));
                dirs.Add(new Vector2Int(0, 1));
                dirs.Add(new Vector2Int(1, 1));
                dirs.Add(new Vector2Int(-1, 0));
                dirs.Add(new Vector2Int(1, 0));
                dirs.Add(new Vector2Int(0,-1));

            }
            else if (Player == 1)
            {
                dirs.Add(new Vector2Int(1, -1));
                dirs.Add(new Vector2Int(1, 0));
                dirs.Add(new Vector2Int(1, 1));
                dirs.Add(new Vector2Int(0, -1));
                dirs.Add(new Vector2Int(0, 1));
                dirs.Add(new Vector2Int(-1, 0));

            }
            else if (Player == 2)
            {
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(0, -1));
                dirs.Add(new Vector2Int(1, -1));
                dirs.Add(new Vector2Int(-1, 0));
                dirs.Add(new Vector2Int(1, 0));
                dirs.Add(new Vector2Int(0, 1));

            }
            else if (Player == 3)
            {
                dirs.Add(new Vector2Int(-1, -1));
                dirs.Add(new Vector2Int(-1, 0));
                dirs.Add(new Vector2Int(-1, 1));
                dirs.Add(new Vector2Int(0, -1));
                dirs.Add(new Vector2Int(0, 1));
                dirs.Add(new Vector2Int(1, 0));


            }

            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }

        }
        //玉
        else if (UnitType.Gyoku == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            dirs.Add(new Vector2Int(-1, 1));
            dirs.Add(new Vector2Int(0, 1));
            dirs.Add(new Vector2Int(1, 1));
            dirs.Add(new Vector2Int(-1, 0));
            dirs.Add(new Vector2Int(1, 0));
            dirs.Add(new Vector2Int(0, -1));
            dirs.Add(new Vector2Int(-1, -1));
            dirs.Add(new Vector2Int(1, -1));



            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y])) continue;
                ret.Add(checkpos);
            }

        }
        //角
        else if (UnitType.Kaku == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            dirs.Add(new Vector2Int(-1, 1));
            dirs.Add(new Vector2Int(1, 1));
            dirs.Add(new Vector2Int(-1, -1));
            dirs.Add(new Vector2Int(1, -1));



            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos) && !isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    if (units[checkpos.x, checkpos.y])
                    {
                        ret.Add(checkpos);
                        break;
                    }
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }

        }
        //飛車
        else if (UnitType.Hisha == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            dirs.Add(new Vector2Int(0, 1));
            dirs.Add(new Vector2Int(0, -1));
            dirs.Add(new Vector2Int(-1, 0));
            dirs.Add(new Vector2Int(1, 0));



            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos) && !isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    if (units[checkpos.x,checkpos.y])
                    {
                        ret.Add(checkpos);
                        break;
                    }
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }

        }
        //香車
        else if (UnitType.Kyousha == unittype)
        {
            //4人用なので向きが4方向になる 左回りにターンが進む
            //向き
            if (Player == 0)
            {
                dirs.Add(new Vector2Int(0, 1));
            }
            else if (Player == 1)
            {
                dirs.Add(new Vector2Int(1, 0));
            }
            else if (Player == 2)
            {
                dirs.Add(new Vector2Int(0, -1));
            }
            else if (Player == 3)
            {
                dirs.Add(new Vector2Int(-1, 0));
            }

            foreach (var item in dirs)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos) && !isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    if (units[checkpos.x, checkpos.y])
                    {
                        ret.Add(checkpos);
                        break;
                    }
                    ret.Add(checkpos);
                    checkpos += item;
                }
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

    //キャプチャされたとき
    public void Caputure(int player)
    {
        Player = player;
        FieldStatus = FieldStatus.Captured;
        Evolution(false);
        GetComponent<Rigidbody>().isKinematic = true;
    }

    //成
    public void Evolution(bool evolution = true)
    {
        Vector3 angle = transform.eulerAngles;

        //成
        if (evolution && UnitType.None != evolutionTable[UnitType])
        {
            UnitType = evolutionTable[UnitType];
            angle.x = 270;
            angle.y = 90 * (Player - 2);
            angle.z = 0;
            transform.eulerAngles = angle;
        }
        else
        {
            UnitType = OldUnitType;
            transform.eulerAngles = getDefaultAngles(Player);
        }

        isEvolution = evolution;
    }
}
