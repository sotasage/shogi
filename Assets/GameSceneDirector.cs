using System.Collections;
using System.Collections.Generic;
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
    }

    // Update is called once per frame
    void Update()
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
            moveUnit(selectUnit, movableTiles[tile]);
            selectUnit = null;
        }

        //���j�b�g�I��
        if (unit)
        {
            setSerectCursors(unit);
        }
    }

    //�I����
    void setSerectCursors(UnitController unit = null, bool playerunit = true)
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
    void moveUnit(UnitController unit, Vector2Int tileindex)
    {
        //���ݒn
        Vector2Int oldpos = unit.Pos;

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
        }

        //���j�b�g�̏�Ԃ��X�V
        unit.FieldStatus = FieldStatus.OnBoard;
    }

    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        //���肳��Ă��܂����`�F�b�N

        return ret;
    }
}
