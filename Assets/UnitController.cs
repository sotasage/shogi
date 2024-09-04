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
    //ユニットのプレイヤー番号
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
}
