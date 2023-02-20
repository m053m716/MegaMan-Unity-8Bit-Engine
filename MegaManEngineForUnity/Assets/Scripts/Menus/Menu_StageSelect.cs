using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class Menu_StageSelect : Menu
{

    private Vector2 inputVec;
    public float inputCooldown;
    private float lastAngle;

    public Camera cmr;
    private Vector3 cmrPos;
    private bool cameraInRoom = true;
    private Vector2 prevScreenXY;
    private bool cmrSnapToRoom;
    private float _transitionTime = 0f;
    private float _transitionComplete = 1f;
    private static readonly float _TRANSITION_PERIOD = 0.35f; // seconds

    private GUISkin font;

    public enum Rooms { MainStage, FortressStage, Shop, Data }
    public Rooms activeRoom;
    public bool stageSelected;
    public float stageCooldown;

    [Header("Main Stage Select Elements")]

    public Vector2 stageSelectorOrigin;
    public Vector2Int stageIndex;

    public AudioClip stageAudioMove;
    public AudioClip stageAudioConfirm;
    public AudioClip stageAudioCancel;

    public SpriteRenderer selectMainSprite;
    public Sprite selectFlashStage1;
    public Sprite selectFlashStage2;
    public Sprite selectFlashStage3;
    public Sprite selectFlashStage4;

    public SpriteRenderer pharaohIcon;
    public SpriteRenderer geminiIcon;
    public SpriteRenderer metalIcon;
    public SpriteRenderer starIcon;
    public SpriteRenderer bombIcon;
    public SpriteRenderer windIcon;
    public SpriteRenderer galaxyIcon;
    public SpriteRenderer commandoIcon;

    [Header("Fortress Stage Select Elements")]

    public AudioClip fortressAudioMove;
    public AudioClip fortressAudioConfirm;
    public AudioClip fortressPathSound;
    public AudioClip fortressTrack;

    public Vector2 fortressSelectorOrigin;
    public Vector2Int fortressIndex;
    private bool fortressPlayingAnimation;

    public Sprite[] fortressStageIcons;

    public Sprite selectFlashFort1;
    public Sprite selectFlashFort2;
    public Sprite selectFlashFort3;
    public Sprite selectFlashFort4;
    public SpriteRenderer fortIconReference;
    public SpriteRenderer[] fortIcons;

    public MeshFilter fortPathFilter;

    [System.Serializable]
    public class FortressPathPoints
    {
        public Vector3[] points;
        public Vector3 this[int i]
        {
            get
            {
                return points[i];
            }
        }
        public int Count
        {
            get { return points.Length; }
        }
    }
    public FortressPathPoints[] fortPathPoints;

    [Header("Shop Elements")]

    public AudioClip shopAudioMove;

    public Vector2 shopSelectorOrigin;
    public Vector3Int shopIndex = new Vector3Int(0, 0, 0);

    public SpriteRenderer shopSelect;
    public Sprite selectFlashShop1;
    public Sprite selectFlashShop2;

    public Item.Items[] itemCatalog;
    public SpriteRenderer[] itemSlots;

    public SpriteRenderer itemDisplay;

    public float purchaseCooldown;

    [Header("Data Elements")]

    public AudioClip dataAudioMove;
    public AudioClip dataAudioChange;

    public Vector2 dataSelectorOrigin;
    public Vector2Int dataIndex;

    public SpriteRenderer SLButton;
    public Sprite SaveSprite;
    public Sprite LoadSprite;

    public string dataDesc;

    public override void Start()
    {
        Time.timeScale = 1.0f;
        stageIndex = GameManager.lastStageSelected;
        inputVec = Vector2.zero;
        inputCooldown = 0.0f;
        lastAngle = 0;
        itemCatalog = new Item.Items[] { Item.Items.ETank, Item.Items.WTank, Item.Items.MTank, Item.Items.LTank, Item.Items.empty, Item.Items.OneUp, Item.Items.RedBullTank, Item.Items.Yashichi, Item.Items.empty, Item.Items.empty, Item.Items.empty, Item.Items.empty, Item.Items.boltDev };
        cmrPos = cmr.transform.position;

        string s = GameManager.LoadData(dataIndex.y, false);
        SetDataDescription(s);

        ChangeRoom(Rooms.MainStage);
        stageSelected = false;
        stageCooldown = 1.0f;

        RefreshFortressStages();
        
        Helper.SetAspectRatio(cmr);
        prevScreenXY = new Vector2(Screen.width, Screen.height);

        font = (GUISkin)Resources.Load("GUI/8BitFont", typeof(GUISkin));

        GameManager.checkpointActive = false;
        GameManager.stageItems.Clear();
    }
    public override void Update()
    {
        if (!stageSelected)
        {
            inputVec = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (inputVec.sqrMagnitude > 1f)
                inputVec.Normalize();

            if (cmrSnapToRoom)
            {
                cmr.transform.position = new Vector3(cmrPos.x, cmrPos.y, cmr.transform.position.z);
            }
            else
            {
                if (!cameraInRoom)
                {
                    _transitionTime += Time.deltaTime;
                    _transitionComplete = Mathf.Min(_transitionTime / _TRANSITION_PERIOD, 1f);
                    cameraInRoom = _transitionComplete >= 1f;
                }
                Vector3 pNew = new Vector3();
                pNew = Vector3.Lerp(cmr.transform.position, cmrPos, _transitionComplete);
                cmr.transform.position = pNew;
            }

            if (inputVec.magnitude < 0.5f)
            {
                inputCooldown = 0.0f;
            }
            else
            {
                float moveAngle = Mathf.Round(Vector2.SignedAngle(Vector2.right, inputVec) / 90) * 90;
                if (moveAngle != lastAngle)
                {
                    lastAngle = moveAngle;
                    inputCooldown = 0.0f;
                }

                if (inputCooldown <= 0.0f && cameraInRoom)
                {
                    inputCooldown = 0.2f;

                    switch (activeRoom)
                    {
                        case Rooms.MainStage:
                            InputMainStageSelect();
                            break;
                        case Rooms.FortressStage:
                            InputFortressStageSelect();
                            break;
                        case Rooms.Shop:
                            InputShopStageSelect();
                            break;
                        case Rooms.Data:
                            InputDataStageSelect();
                            break;
                    }
                }
                else
                {
                    inputCooldown -= Time.deltaTime;
                }
            }
        }
        else
        {
            stageCooldown -= Time.deltaTime;
            if (stageCooldown <= 0)
            {
                switch (activeRoom)
                {
                    case Rooms.MainStage:
                        if (!GoToSelectedScene())
                        {
                            stageCooldown = 0.5f;
                            stageSelected = false;
                        }
                        break;
                    case Rooms.FortressStage:
                        if (!GoToSelectedScene())
                        {
                            stageCooldown = 0.5f;
                            stageSelected = false;
                        }
                        break;
                }
            }
        }

        if (Screen.width != prevScreenXY.x || Screen.height != prevScreenXY.y)
        {
            Helper.SetAspectRatio(cmr);
            prevScreenXY = new Vector2(Screen.width, Screen.height);
        }

        switch (activeRoom)
        {
            case Rooms.MainStage:
                UpdateMainStageSelect();
                break;
            case Rooms.FortressStage:
                UpdateFortressStageSelect();
                break;
            case Rooms.Shop:
                UpdateShopStageSelect();
                break;
            case Rooms.Data:
                UpdateDataStageSelect();
                break;
        }
    }
    public override void DrawGUI()
    {
        switch (activeRoom)
        {
            case Rooms.Shop:
                GUIShopStageSelect();
                break;
            case Rooms.Data:
                GUIDataStageSelect();
                break;
        }
    }
    public void OnDrawGizmos()
    {
        FortressGizmos();
    }
    public void ChangeRoom(Rooms newRoom)
    {
        cameraInRoom = false;
        _transitionTime = 0f;
        _transitionComplete = 0f;
        cmrSnapToRoom = false;

        activeRoom = newRoom;
        switch (newRoom)
        {
            case Rooms.MainStage:
                cmrPos = new Vector3(0f, 8f, -100f);

                if (GameManager.bossDead_PharaohMan)
                    pharaohIcon.enabled = false;
                else
                    pharaohIcon.enabled = true;
                if (GameManager.bossDead_GeminiMan)
                    geminiIcon.enabled = false;
                else
                    geminiIcon.enabled = true;
                if (GameManager.bossDead_MetalMan)
                    metalIcon.enabled = false;
                else
                    metalIcon.enabled = true;
                if (GameManager.bossDead_StarMan)
                    starIcon.enabled = false;
                else
                    starIcon.enabled = true;
                if (GameManager.bossDead_BombMan)
                    bombIcon.enabled = false;
                else
                    bombIcon.enabled = true;
                if (GameManager.bossDead_WindMan)
                    windIcon.enabled = false;
                else
                    windIcon.enabled = true;
                if (GameManager.bossDead_GalaxyMan)
                    galaxyIcon.enabled = false;
                else
                    galaxyIcon.enabled = true;
                if (GameManager.bossDead_CommandoMan)
                    commandoIcon.enabled = false;
                else
                    commandoIcon.enabled = true;
                break;
            case Rooms.FortressStage:
                cmrPos = new Vector3(0f, 248f, -100f);
                break;
            case Rooms.Shop:
                cmrPos = new Vector3(-272f, 8f, -100f);
                break;
            case Rooms.Data:
                cmrPos = new Vector3(272f, 8f, -100f);
                break;
        }
    }
    public bool GoToSelectedScene(bool checkOnly = false, bool snapToRoom = false)
    {
        cmrSnapToRoom = snapToRoom;
        if (activeRoom == Rooms.MainStage)
        {
            if (stageIndex.x == 0 && stageIndex.y == 0)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_PharaohMan");
                return true;
            }
            if (stageIndex.x == 2 && stageIndex.y == 0)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_GeminiMan");
                return true;
            }
            if (stageIndex.x == 2 && stageIndex.y == 1)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_MetalMan");
                return true;
            }
            if (stageIndex.x == 0 && stageIndex.y == 2)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_StarMan");
                return true;
            }
            if (stageIndex.x == 1 && stageIndex.y == 0)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_BombMan");
                return true;
            }
            if (stageIndex.x == 0 && stageIndex.y == 1)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_WindMan");
                return true;
            }
            if (stageIndex.x == 1 && stageIndex.y == 2)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_GalaxyMan");
                return true;
            }
            if (stageIndex.x == 2 && stageIndex.y == 2)
            {
                if (!checkOnly)
                    Helper.GoToStage("Stage_CommandoMan");
                return true;
            }
        }
        else if (activeRoom == Rooms.FortressStage)
        {
            if (fortressIndex.x == 0)
            {
                if (!checkOnly)
                    Helper.GoToStage("Fortress_Devil");
                return true;
            }
            if (fortressIndex.x == 1)
            {
                if (!checkOnly)
                    Helper.GoToStage("Fortress_MegaBot");
                return true;
            }
            if (fortressIndex.x == 2)
            {
                if (!checkOnly)
                    Helper.GoToStage("Fortress_Roll");
                return true;
            }
            if (fortressIndex.x == 3)
            {
                if (!checkOnly)
                    Helper.GoToStage("Fortress_Roll3D");
                return true;
            }
        }

        return false;
    }
    public IEnumerator PlayFortressAnimation(int stage)
    {
        while (!cameraInRoom)
            yield return null;

        fortressPlayingAnimation = true;
        foreach (SpriteRenderer rend in fortIcons)
            rend.gameObject.SetActive(false);
        fortIconReference.enabled = false;

        yield return new WaitForSeconds(8f);

        //  Preparing the mesh.
        Mesh mesh = new Mesh();

        List<Vector3> v = new List<Vector3>();
        List<int> t = new List<int>();
        List<Vector2> u = new List<Vector2>();



        // Deciding the place of each path segment.

        if (stage == 0)
            yield break;
        else if (stage >= fortPathPoints.Length)
            stage = fortPathPoints.Length;


        for (int j = 0; j < stage; j++)
        {
            for (int i = 0; i < fortPathPoints[j].Count - 1; i++)
            {
                Vector2 direction = (fortPathPoints[j][i + 1] - fortPathPoints[j][i]);
                int steps    = (int)(direction.magnitude / 8);
                float distance = direction.magnitude / (steps + 0.75f);
                direction.Normalize();

                for (int s =  0; s <= steps; s++)
                {
                    int vc = v.Count;
                    v.Add((Vector2)fortPathPoints[j][i] + Vector2.Perpendicular(direction) * 4f + direction * distance * (s + 0));
                    v.Add((Vector2)fortPathPoints[j][i] - Vector2.Perpendicular(direction) * 4f + direction * distance * (s + 0));
                    v.Add((Vector2)fortPathPoints[j][i] - Vector2.Perpendicular(direction) * 4f + direction * distance * (s + 1));
                    v.Add((Vector2)fortPathPoints[j][i] + Vector2.Perpendicular(direction) * 4f + direction * distance * (s + 1));

                    t.Add(vc + 0);
                    t.Add(vc + 3);
                    t.Add(vc + 2);
                    t.Add(vc + 0);
                    t.Add(vc + 2);
                    t.Add(vc + 1);

                    u.Add(new Vector2(0, 1));
                    u.Add(new Vector2(0, 0));
                    u.Add(new Vector2(1, 0));
                    u.Add(new Vector2(1, 1));

                    if (j == stage - 1)
                    {
                        mesh = new Mesh();
                        mesh.vertices = v.ToArray();
                        mesh.triangles = t.ToArray();
                        mesh.uv = u.ToArray();

                        fortPathFilter.mesh = mesh;

                        cmr.GetComponent<AudioSource>().PlaySound(fortressPathSound, true);
                        yield return new WaitForSeconds(0.075f);
                    }
                    
                }

            }
        }

        foreach (SpriteRenderer rend in fortIcons)
            rend.gameObject.SetActive(true);
        fortIconReference.enabled = true;

        fortressPlayingAnimation = false;
        GameManager.playFortressStageUnlockAnimation = false;

    }
   


    private void InputMainStageSelect()
    {
        // Normally the moveAngle should be one of [-180, -90, 0, 90, 180].
        if (lastAngle == 0)
            stageIndex.x += 1;
        else if (lastAngle == 180 || lastAngle == -180)
            stageIndex.x -= 1;
        else if (lastAngle == 90)
            stageIndex.y -= 1;
        else
            stageIndex.y += 1;

        if (stageIndex.y < 0)
            ChangeRoom(Rooms.FortressStage);
        else if (stageIndex.x < 0)
            ChangeRoom(Rooms.Shop);
        else if (stageIndex.x > 2)
            ChangeRoom(Rooms.Data);
        else if (stageIndex.y <= 2)
            Helper.PlaySound(stageAudioMove);

        stageIndex.x = Mathf.Clamp(stageIndex.x, 0, 2);
        stageIndex.y = Mathf.Clamp(stageIndex.y, 0, 2);
    }
    private void UpdateMainStageSelect()
    {
        selectMainSprite.transform.position = new Vector3(stageSelectorOrigin.x + stageIndex.x * 64,
                                              stageSelectorOrigin.y - stageIndex.y * 64,
                                              selectMainSprite.transform.position.z);
        if (Input.GetButtonDown("Start"))
        {
            stageSelected = true;
            if (GoToSelectedScene(true))
            {
                Helper.PlaySound(stageAudioConfirm);
                cmr.GetComponent<AudioSource>().Stop();
            }
            else
                Helper.PlaySound(stageAudioCancel);
        }

        if (stageSelected)
            selectMainSprite.sprite = Time.time % 0.1f < 0.05f ? selectFlashStage3 : selectFlashStage4;
        else
            selectMainSprite.sprite = Time.time % 0.5f < 0.25f ? selectFlashStage1 : selectFlashStage2;
    }

    private void InputFortressStageSelect()
    {
        if (fortressPlayingAnimation)
            return;

        // Normally the moveAngle should be one of [-180, -90, 0, 90, 180].
        if (lastAngle == 0)
            fortressIndex.x += 1;
        else if (lastAngle == 180 || lastAngle == -180)
            fortressIndex.x -= 1;
        else if (lastAngle == 90)
            fortressIndex.y -= 1;
        else
            fortressIndex.y += 1;

        if (fortressIndex.y > 0)
            ChangeRoom(Rooms.MainStage);
        else if (fortressIndex.y == 0 && fortressIndex.x >= 0 && fortressIndex.x <= GameManager.maxFortressStage - 1)
            Helper.PlaySound(fortressAudioMove);

        fortressIndex.x = Mathf.Clamp(fortressIndex.x, 0, GameManager.maxFortressStage - 1);
        fortressIndex.y = 0;
    }
    private void UpdateFortressStageSelect()
    {
        if (Input.GetButtonDown("Start"))
        {
            stageSelected = true;
            if (GoToSelectedScene(true))
            {
                Helper.PlaySound(stageAudioConfirm);
                cmr.GetComponent<AudioSource>().Stop();
            }
            else
                Helper.PlaySound(stageAudioCancel);
        }

        for (int i = 0; i < fortIcons.Length; i++)
        {
            if (fortressIndex.x == i)
            {
                if (stageSelected)
                    fortIcons[i].sprite = Time.time % 0.1f < 0.05f ? selectFlashFort3 : selectFlashFort4;
                else
                    fortIcons[i].sprite = Time.time % 0.5f < 0.25f ? selectFlashFort1 : selectFlashFort2;
            }
            else
                fortIcons[i].sprite = selectFlashFort1;
        }
    }
    public void RefreshFortressStages()
    {
        for (int i = 1; i < fortIcons.Length; i++)
        {
            if (fortIcons[i] != null)
                UnityEngine.Object.Destroy(fortIcons[i].gameObject);
        }
        if (GameManager.maxFortressStage > 0)
        {
            fortIcons = new SpriteRenderer[GameManager.maxFortressStage];
            fortIcons[0] = fortIconReference;
            for (int i = 0; i < fortIcons.Length; i++)
            {
                if (i > 0)
                {
                    fortIcons[i] = UnityEngine.Object.Instantiate(fortIcons[0]);
                }
                fortIcons[i].transform.position = new Vector3(272f * (1 + i) / (1 + GameManager.maxFortressStage) - 136f,
                                                              fortIcons[0].transform.position.y,
                                                              fortIcons[0].transform.position.z);
                fortIcons[i].transform.parent = fortIcons[0].transform.parent;

                SpriteRenderer icon = fortIcons[i].transform.GetChild(0).GetComponent<SpriteRenderer>();
                if (fortressStageIcons.Length > i)
                    icon.sprite = fortressStageIcons[i];
            }
        }

        if (GameManager.playFortressStageUnlockAnimation)
        {
            ChangeRoom(Rooms.FortressStage);
            cmrSnapToRoom = true;
            UnityEngine.Object.FindObjectOfType<Menu_Controller>().StartCoroutine(PlayFortressAnimation(GameManager.maxFortressStage));
            cmr.GetComponent<AudioSource>().PlaySound(fortressTrack, true);
            cmr.GetComponent<AudioSource>().loop = false;
            UnityEngine.Object.Destroy(cmr.GetComponent<Misc_PlayRandomTrack>());
        }
    }
    public void FortressGizmos()
    {
        int stage = 6;

        if (stage == 0)
            return;
        else if (stage >= fortPathPoints.Length)
            stage = fortPathPoints.Length;


        for (int j = 0; j < stage; j++)
        {
            for (int i = 0; i < fortPathPoints[j].Count - 1; i++)
            {
                Vector2 direction = (fortPathPoints[j][i + 1] - fortPathPoints[j][i]);
                int steps = (int)(direction.magnitude / 8);
                float distance = direction.magnitude / steps;
                direction.Normalize();

                for (int s = 0; s <= steps; s++)
                {
                    Gizmos.color = Color.cyan * ((float)s / steps) + Color.blue * (1f - (float)s / steps);
                    Gizmos.DrawSphere((Vector2)fortPathPoints[j][i] + direction * distance * s, 4);
                }

            }
        }
    }

    private void InputShopStageSelect()
    {
        _HandleShopStageSelectorMovement();
        _HandleUpdatingShopCatalog();
        _HandleUpdatingShopSelectedItemDisplay();
    }
    private void _HandleShopStageSelectorMovement()
    {
        int maxZ = Mathf.FloorToInt(itemCatalog.Length / 6) + 1;

        // Normally the moveAngle should be one of [-180, -90, 0, 90, 180].
        if (lastAngle == 0)
        {
            if (shopIndex.x == 5)
            {
                ChangeRoom(Rooms.MainStage);
            }
            else
            {
                shopIndex.x += 1; // selector moves right 1 place.
                Helper.PlaySound(shopAudioMove);
            }
        }
        else if (lastAngle == 180 || lastAngle == -180)
        {
            if (shopIndex.x > 0)
            {
                shopIndex.x -= 1; // selector moves left 1 place.
                Helper.PlaySound(shopAudioMove);
            }
        }
        else if (lastAngle == 90)
        {
            if (shopIndex.y == 1)
            {
                shopIndex.y = 0; // selector moves to top row.
            }
            if (shopIndex.z > 0)
            {
                shopIndex.z -= 1; // this tracks "scrolling" of items, "upward"
                Helper.PlaySound(shopAudioMove);
            }
        }
        else if (lastAngle == -90)
        {
            if (shopIndex.y == 0)
            {
                shopIndex.y = 1; // selector moves to lower row.
            }
            if (shopIndex.z < maxZ)
            {
                shopIndex.z += 1; // track "scrolling" of items, "downward"
                Helper.PlaySound(shopAudioMove);
            }
        }
    }

    private void _HandleUpdatingShopCatalog()
    {
        GameObject item;
        // Draw the image of each item.
        for (int x = 0; x < 6; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                if ((shopIndex.z - shopIndex.y + z) * 6 + x >= itemCatalog.Length)
                {
                    itemSlots[z * 6 + x].sprite = null;
                }
                else
                {
                    item = Item.GetObjectFromItem(itemCatalog[(shopIndex.z - shopIndex.y + z) * 6 + x]);
                    if (item != null)
                    {
                        itemSlots[z * 6 + x].sprite = item.GetComponentInChildren<SpriteRenderer>().sprite; 
                    }
                    else
                    {
                        itemSlots[z * 6 + x].sprite = null;
                    }
                }
            }
        }
    }

    private Boolean _HandleUpdatingShopSelectedItemDisplay()
    {
        GameObject item;
        if ((shopIndex.z * 6 + shopIndex.x) < itemCatalog.Length)
        {
            item = Item.GetObjectFromItem(itemCatalog[(shopIndex.z) * 6 + shopIndex.x]); 
        } 
        else
        {
            item = null;
        }
            
        if (item != null)
        {
            itemDisplay.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
        }
        else
        {
            itemDisplay.sprite = null;
        }
        return item != null;
            
    }

    private void UpdateShopStageSelect()
    {
        shopSelect.sprite = Time.time % 0.5f < 0.25f ? selectFlashShop1 : selectFlashShop2;

        shopSelect.transform.position = new Vector3(shopSelectorOrigin.x + shopIndex.x * 32,
                                                    shopSelectorOrigin.y - shopIndex.y * 48,
                                                    shopSelect.transform.position.z);

        if (purchaseCooldown > 0.0f)
        {
            purchaseCooldown -= Time.deltaTime;
        } else if (Input.GetButtonDown("Start")) {

            if ((shopIndex.z) * 6 + shopIndex.x >= Item.itemList.Length)
                return;

            Item.Items item = itemCatalog[(shopIndex.z) * 6 + shopIndex.x];
            if (GameManager.bolts >= Item.itemList[(int)item].boltCost)
            {
                Item.AddItemQuantity(item, 1);
                GameManager.bolts -= Item.itemList[(int)item].boltCost;
                Helper.PlaySound(stageAudioConfirm);
            }
            else
            {
                Helper.PlaySound(stageAudioCancel);
            }
            purchaseCooldown = 0.25f;
        }
    }
    private void GUIShopStageSelect()
    {
        if (!cameraInRoom)
        {
            return;
        }
        
        Vector2 cmrBase = new Vector2(Camera.main.rect.x * Screen.width, Camera.main.rect.y * Screen.height);
        int blockSize = (int)(Camera.main.pixelWidth / 16);
        font.label.fontSize = (int)(blockSize * 0.5625f);

        // Draw sprite images of each object in the catalog.
        _HandleUpdatingShopCatalog();

        // Indicate the currently-selected item (and how many user owns).
        Boolean isValidItem = _HandleUpdatingShopSelectedItemDisplay();
        if (isValidItem)
        {
            // If the item is in the catalog, then list how many are in our inventory.
            GUI.Label(new Rect(cmrBase.x + 3.5f * blockSize,
                                   cmrBase.y + 12.5f * blockSize,
                                   2.125f * blockSize,
                                   1.0f * blockSize),
                                   Item.GetItemQuantity(itemCatalog[shopIndex.z * 6 + shopIndex.x]).ToString("000"),
                                   font.label);
        } 
        else
        {
            GUI.Label(new Rect(cmrBase.x + 3.5f * blockSize,
                                   cmrBase.y + 12.5f * blockSize,
                                   2.125f * blockSize,
                                   1.0f * blockSize),
                                   0.ToString("000"),
                                   font.label);
        }

        for (int i = 0; i < itemSlots.Length; i++)
        { 
            if (itemSlots[i].sprite != null)
            {
                int x = i % 6;
                int y = Mathf.FloorToInt(i / 6);
                GUI.Label(new Rect(cmrBase.x + blockSize * (2.425f + 2.00f * x),
                                               cmrBase.y + blockSize * (3.5f + 2.75f * y),
                                               blockSize * 2f,
                                               blockSize * 2f),
                                               Item.itemList[(int)itemCatalog[(y + shopIndex.z - shopIndex.y) * 6 + x]].boltCost.ToString("000"),
                                               font.label);
            }

        }

        // Draw the number of bolts left.
        GUI.Label(new Rect(cmrBase.x + 11.5f * blockSize,
                           cmrBase.y + 12.5f * blockSize,
                           2.125f * blockSize,
                           1.0f * blockSize),
                           GameManager.bolts.ToString("0000"),
                           font.label);

    }

    private void InputDataStageSelect()
    {
        // Normally the moveAngle should be one of [-180, -90, 0, 90, 180].
        if (lastAngle == 0)
            dataIndex.x += 1;
        else if (lastAngle == 180 || lastAngle == -180)
            dataIndex.x -= 1;
        else if (lastAngle == 90)
            dataIndex.y -= 1;
        else
            dataIndex.y += 1;

        string s = GameManager.LoadData(dataIndex.y, false);
        SetDataDescription(s);

        if (dataIndex.x < 0)
            ChangeRoom(Rooms.MainStage);
        else if (lastAngle == 0 || lastAngle == 180 || lastAngle == -180)
            Helper.PlaySound(dataAudioChange);
        else
            Helper.PlaySound(dataAudioMove);


        dataIndex.x = Mathf.Clamp(dataIndex.x, 0, 1);
        dataIndex.y = Mathf.Clamp(dataIndex.y, 0, 9);
    }
    private void UpdateDataStageSelect()
    {
        SLButton.sprite = dataIndex.x == 0 ? SaveSprite : LoadSprite;

        if (Input.GetButtonDown("Start"))
        {
            if (dataIndex.x == 0)
            {
                GameManager.SaveData(dataIndex.y);
                Helper.PlaySound(dataAudioChange);
            } else {
                GameManager.LoadData(dataIndex.y, true);
                Helper.PlaySound(dataAudioChange);
                RefreshFortressStages();
            }
        }
    }
    private void SetDataDescription(string s)
    {
        dataDesc = "";

        if (s == null || s.Length < 26)
            return;

        int output = 0;
        int.TryParse(s[0].ToString(), out output);
        dataDesc += "BombMan:         " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[1].ToString(), out output);
        dataDesc += "MetalMan:        " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[2].ToString(), out output);
        dataDesc += "GeminiMan:       " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[3].ToString(), out output);
        dataDesc += "PharaohMan:   " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[4].ToString(), out output);
        dataDesc += "StarMan:          " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[5].ToString(), out output);
        dataDesc += "WindMan:          " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[6].ToString(), out output);
        dataDesc += "GalaxyMan:      " + (output == 1 ? "Dead" : "Alive") + "\n";
        int.TryParse(s[7].ToString(), out output);
        dataDesc += "CommandoMan: " + (output == 1 ? "Dead" : "Alive") + "\n";

        dataDesc += "\n";
        int.TryParse(s.Substring(8, 2), out output);
        dataDesc += "Fortress Stages: " + output.ToString() + "\n";

        dataDesc += "\n";
        int.TryParse(s.Substring(10, 2), out output);
        dataDesc += "E Tanks:  " + output.ToString() + "\n";
        int.TryParse(s.Substring(12, 2), out output);
        dataDesc += "W Tanks:  " + output.ToString() + "\n";
        int.TryParse(s.Substring(14, 2), out output);
        dataDesc += "M Tanks:  " + output.ToString() + "\n";
        int.TryParse(s.Substring(16, 2), out output);
        dataDesc += "L Tanks:  " + output.ToString() + "\n";
        int.TryParse(s.Substring(18, 2), out output);
        dataDesc += "RedBulls: " + output.ToString() + "\n";
        int.TryParse(s.Substring(20, 2), out output);
        dataDesc += "Yashichi: " + output.ToString() + "\n";

        dataDesc += "\n";
        int.TryParse(s.Substring(22, 4), out output);
        dataDesc += "Bolts: " + output;

    }
    private void GUIDataStageSelect()
    {
        if (!cameraInRoom)
            return;

        Vector2 cmrBase = new Vector2(Camera.main.rect.x * Screen.width, Camera.main.rect.y * Screen.height);
        float pixelSize = (Camera.main.pixelWidth / 272f);
        font.label.fontSize = (int)(pixelSize * 8);

        for (int i = 0; i < 10; i++)
        {
            string text = "Slot " + (i + 1).ToString();
            if (i == dataIndex.y)
                text = "> " + text;
            GUI.Label(new Rect(cmrBase.x + dataSelectorOrigin.x * pixelSize,
                               cmrBase.y + dataSelectorOrigin.y * pixelSize + i * 16f * pixelSize,
                               64f * pixelSize,
                               16 * pixelSize),
                      text,
                      font.label);
        }

        GUI.Label(new Rect(cmrBase.x + dataSelectorOrigin.x * pixelSize + 94f * pixelSize,
                   cmrBase.y + dataSelectorOrigin.y * pixelSize,
                   128f * pixelSize,
                   240f * pixelSize),
                   dataDesc,
                   font.label);
    }

}
