using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneDirector : MonoBehaviour
{
    //UI�֘A
    [SerializeField] Text textTurnInfo;
    [SerializeField] Text textResultInfo;
    [SerializeField] Button buttonTitle;
    [SerializeField] Button buttonRematch;
    [SerializeField] Button buttonEvolutionApply;
    [SerializeField] Button buttonEvolutionCancel;

    //�Q�[���ݒ�
    const int PlayerMax = 4;
    int boardWidth;
    int boardHeight;

    //�^�C���̃v���n�u
    [SerializeField] GameObject prefabTile;

    //���j�b�g�̃v���n�u
    [SerializeField] List<GameObject> prefabUnits;

    //�����z�u
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

    //�t�B�[���h�f�[�^
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //���ݑI�𒆂̃��j�b�g
    UnitController selectUnit;

    //�ړ��\�͈�
    Dictionary<GameObject, Vector2Int> movableTiles;

    //�J�[�\���̃v���n�u
    [SerializeField] GameObject prefabCursor;

    //�J�[�\���I�u�W�F�N�g
    List<GameObject> cursors;

    //�v���C���[�ƃ^�[��
    int nowPlayer;
    int turnCount;
    bool isCpu;

    //���[�h
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

    //������^�C���̃v���n�u
    [SerializeField] GameObject prefabUnitTile;

    //�������u���ꏊ
    List<GameObject>[] unitTiles;

    //�L���v�`�����ꂽ���j�b�g
    List<UnitController> captureUnits;

    // Start is called before the first frame update
    void Start()
    {
        //UI�֘A�����ݒ�
        buttonTitle.gameObject.SetActive(false);
        buttonRematch.gameObject.SetActive(false);
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);
        textResultInfo.text = "";

        //�{�[�h�T�C�Y
        boardWidth = boardSetting.GetLength(0);
        boardHeight = boardSetting.GetLength(1);

        //�t�B�[���h������
        tiles = new Dictionary<Vector2Int, GameObject>();
        units = new UnitController[boardWidth, boardHeight];

        //�ړ��\�͈�
        movableTiles = new Dictionary<GameObject, Vector2Int>();
        cursors = new List<GameObject>();

        //�������u���ꏊ
        unitTiles = new List<GameObject>[PlayerMax];

        //�L���v�`�����ꂽ���j�b�g
        captureUnits = new List<UnitController>();

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                //�^�C���ƃ��j�b�g�̃|�W�V����
                float x = i - boardWidth / 2;
                float y = j - boardHeight / 2;

                //�|�W�V����
                Vector3 pos = new Vector3(x, 0, y);

                //�^�C���̃C���f�b�N�X
                Vector2Int tileindex = new Vector2Int(i, j);

                //�^�C���쐬
                GameObject tile = Instantiate(prefabTile, pos, Quaternion.identity);
                tiles.Add(tileindex, tile);

                //���j�b�g�쐬
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                if (0 == type) continue;

                //������
                pos.y = 0.7f;

                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 90, 0));
                unit.AddComponent<Rigidbody>();

                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, tileindex);

                //���j�b�g�f�[�^�Z�b�g
                units[i, j] = unitctrl;
            }
        }

        //�������u���ꏊ�̍쐬
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

        //TurnChange����n�߂�ꍇ-1
        nowPlayer = -1;

        //���񃂁[�h
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

        //���[�h�ύX
        if (Mode.None != nextMode)
        {
            nowMode = nextMode;
            nextMode = Mode.None;
        }
    }

    //�I����
    void setSelectCursors(UnitController unit = null, bool playerunit = true)
    {
        //�J�[�\���폜
        foreach (var item in cursors)
        {
            Destroy(item);
        }
        cursors.Clear();

        //�I�����j�b�g�̔�I�����
        if (selectUnit)
        {
            selectUnit.Select(false);
            selectUnit = null;
        }

        //���j�b�g��񂪂Ȃ���ΏI��
        if (!unit) return;

        //�ړ��\�͈͎擾
        List<Vector2Int> movabletiles = getMovableTiles(unit);
        movableTiles.Clear();

        foreach (var item in movabletiles)
        {
            movableTiles.Add(tiles[item], item);

            //�J�[�\������
            Vector3 pos = tiles[item].transform.position;
            pos.y += 0.51f;
            GameObject cursor = Instantiate(prefabCursor, pos, Quaternion.identity);
            cursors.Add(cursor);
        }

        //�I�����
        if (playerunit)
        {
            unit.Select();
            selectUnit = unit;
        }
    }

    //���j�b�g�ړ�
    Mode moveUnit(UnitController unit, Vector2Int tileindex)
    {
        //�ړ����I�������̃��[�h
        Mode ret = Mode.TurnChange;

        //���ݒn
        Vector2Int oldpos = unit.Pos;

        //�ړ���ɒN����������
        captureUnit(nowPlayer, tileindex);

        //���j�b�g�ړ�
        unit.Move(tiles[tileindex], tileindex);

        //�����f�[�^�X�V(�V�����ꏊ)
        units[tileindex.x, tileindex.y] = unit;

        //�{�[�h��̋���X�V
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            //�����f�[�^�X�V
            units[oldpos.x, oldpos.y] = null;
        }
        //������̍X�V
        else
        {
            //������̍X�V
            captureUnits.Remove(unit);
        }

        //���j�b�g�̏�Ԃ��X�V
        unit.FieldStatus = FieldStatus.OnBoard;

        //������\�����X�V
        alignCaptureUnits(nowPlayer);

        return ret;
    }

    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        //���肳��Ă��܂����`�F�b�N

        return ret;
    }

    //�^�[���J�n
    void startMode()
    {
        //���s�����Ă��Ȃ���Βʏ탂�[�h
        nextMode = Mode.Select;

        //Info�X�V
        textTurnInfo.text = "" + (nowPlayer + 1) + "P�̔Ԃł�";
        textResultInfo.text = "";

        //���s�`�F�b�N

    }

    //���j�b�g�ƃ^�C���I��
    void selectMode()
    {
        GameObject tile = null;
        UnitController unit = null;

        //�v���C���[����
        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //��O�̃��j�b�g�ɂ������蔻�肪����̂Ńq�b�g�������ׂẴI�u�W�F�N�g�����擾
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                UnitController hitunit = hit.transform.GetComponent<UnitController>();

                //������
                if (hitunit && FieldStatus.Captured == hitunit.FieldStatus)
                {
                    unit = hitunit;
                }
                //�^�C���I���Ə�ɏ���Ă��郆�j�b�g
                else if (tiles.ContainsValue(hit.transform.gameObject))
                {
                    tile = hit.transform.gameObject;
                    //�^�C�����烆�j�b�g��T��
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

        //�����I������Ă��Ȃ���Ώ��������Ȃ�
        if (null == tile && null == unit) return;

        //�ړ���I��
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            nextMode = moveUnit(selectUnit, movableTiles[tile]);
        }

        //���j�b�g�I��
        if (unit)
        {
            bool isPlayer = nowPlayer == unit.Player;
            setSelectCursors(unit, isPlayer);
        }
    }

    //�^�[���ύX
    void turnChangeMode()
    {
        //�{�^���ƃJ�[�\���̃��Z�b�g
        setSelectCursors();
        buttonEvolutionApply.gameObject.SetActive(false);
        buttonEvolutionCancel.gameObject.SetActive(false);

        //CPU��ԉ���
        isCpu = false;

        //���̃v���C���[��
        nowPlayer = GetNextPlayer(nowPlayer);


        //�o�߃^�[��
        if (0 == nowPlayer)
        {
            turnCount++;
        }

        nextMode = Mode.Start;
    }

    //���̃v���C���[�ԍ���Ԃ�
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

    //���������ׂ�
    void alignCaptureUnits(int player)
    {
        //�����������������\��
        foreach (var item in unitTiles[player])
        {
            item.SetActive(false);
        }

        //���j�b�g���Ƃɕ�����
        Dictionary<UnitType, List<UnitController>> typeunits = new Dictionary<UnitType, List<UnitController>>();

        foreach (var item in captureUnits)
        {
            if (player != item.Player) continue;
            typeunits.TryAdd(item.UnitType, new List<UnitController>());
            typeunits[item.UnitType].Add(item);
        }

        //�^�C�v���Ƃɕ��ׂĈ�ԏゾ���\������
        int tilecount = 0;
        foreach (var item in typeunits)
        {
            if (1 > item.Value.Count) continue;

            //�u���ꏊ
            GameObject tile = unitTiles[player][tilecount++];

            //��\���ɂ��Ă����^�C����\������
            tile.SetActive(true);

            //�������̕\��
            tile.transform.GetChild(0).gameObject.GetComponent<TextMeshPro>().text = "" + item.Value.Count;

            //������ނ̎��������ׂ�
            for (int i = 0; i < item.Value.Count; i++)
            {
                //���X�g���̃��j�b�g��\��
                GameObject unit = item.Value[i].gameObject;
                //�u���ꏊ
                Vector3 pos = tile.transform.position;
                //��U���j�b�g���ړ����ĕ\������
                unit.SetActive(true);
                unit.transform.position = pos;
                //1�ڈȊO�͔�\��
                if (0 < i) unit.SetActive(false);
            }
        }
    }
}
