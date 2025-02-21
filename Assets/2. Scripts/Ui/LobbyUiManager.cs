using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LobbyUiManager : MonoBehaviour
{
    // Manage all UI Components in the Lobby Scene
    // Activate UI Animations and set UI informations containing user data

    #region UI Components
    [Header("main ui===================================")]
    [SerializeField] private RectTransform mainUiContainer;
    [SerializeField] private GameObject partsUpgradeBase;
    [SerializeField] private Image panel;               // For Fade-out effect
    [SerializeField] private Image[] fragmentButtons;   // reference to 3 buttons at the bottom
    [SerializeField] private Text jemCount;
    [SerializeField] private Text highScore;
    [SerializeField] private Text ticket;
    [SerializeField] private Image equipment;
    [SerializeField] private Sprite[] partsIcons;
    [SerializeField] private Image jemIcon;
    [SerializeField] private GameObject achievementFlag;
    [SerializeField] private ParticleSystem bgParticle;

    [Header("parts ui===================================")]
    [SerializeField] private GameObject[] parts;
    [SerializeField] private Text selectedPartsName;
    [SerializeField] private Text selectedPartsDescription;
    [SerializeField] private GameObject selectedPartsImage;
    [SerializeField] private Text ability1Title;
    [SerializeField] private Text ability2Title;
    [SerializeField] private Text ability3Title;
    [SerializeField] private Text ability1Description;
    [SerializeField] private Text ability2Description;
    [SerializeField] private Text ability3Description;
    [SerializeField] private Text ability1UpgradeJem;
    [SerializeField] private Text ability2UpgradeJem;
    [SerializeField] private Text ability3UpgradeJem;
    [SerializeField] private Image[] partsUpgradeButtons;

    [Header("achievement===================================")]
    [SerializeField] private Transform uiParticleBase;
    [SerializeField] private Transform root;
    [SerializeField] private ParticleSystem uiParticle; 

    [Header("Popup===================================")]
    [SerializeField] private Scrollbar bgmSlider;
    [SerializeField] private Scrollbar sfxSlider;
    [SerializeField] private SwitchButton bloomToggle;
    [SerializeField] private SwitchButton hitToggle;
    [SerializeField] private GameObject exitPopup;
    [SerializeField] private GameObject battlePopup;
    [SerializeField] private Text ticketNum;
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject unlockPopup;
    [SerializeField] private Text unlockConditionText;
    [SerializeField] private Text toastMessage;
    [SerializeField] private GameObject toastBase;
    [SerializeField] private GameObject tipsPopup;
    [SerializeField] private GameObject tipsPanel;
    [SerializeField] private Dropdown tipList;
    [SerializeField] private GameObject[] tips;

    #endregion

    #region Member Variables
    private Vector3 originalJemScale;
    private bool isTweening=false;
    private GameObject currentPart; // reference to the currently equiped part obj
    private bool isConvertingUi = false;
    private int currentFragment = 0;
    private float currentTime = 0f;  
    private readonly float fadeoutTime = 1f;
    private int selectedPartsIdx = 0;
    private string currentParts;
    private int[] partsUpgradeInfo;
    private float punchScale = 0.2f;
    public bool isPopupOpen { get; private set; } = false;
    private readonly Dictionary<string, int> selectedParts = new()
    {
        {"Missile",0 },
        {"Laser",1 },
        {"Barrier",2 },
        {"Emp",3 }
    };
    private readonly string[] partsDescriptions = new string[4]
    {   "Destroy all Enemies with\r\nExplosiove Missiles!",
        "Nobody can Survive\r\nafter Laser Attack",
        "Barrier will Protects\r\nYour Weakest Part!",
        "Enemies couldn't\r\nGet Close to You!"};
    private readonly string[,,] partsInfo = new string[4, 3, 2]
    {
        {{"Parts Damage","Missile Damage +"},{ "Parts Speed","Missile Speed +"},{ "Abilities","Increase Explosion Range"} },
        {{"Parts Damage","Laser Damage +"},{ "Parts Speed","Laser Speed +"},{ "Abilities","Add Random Laser"} },
        {{"Parts Damage","Barrier Damage +"},{ "Parts Speed","Set Enemy Speed "},{ "Abilities","Make Powerful Shield"} },
        {{"Parts Damage","Emp Damage +"},{ "Parts Speed","Attack Speed +"},{ "Abilities","Add Knock-Back Effect"} }
    };

    [SerializeField] AdsManager adsManager;
    #endregion

    private void Start()
    {
        SetUi();
    }

    #region Initiallization
    // Set user data to the Text at the start of the game
    private void SetUi()
    {
        // Set main panel
        jemCount.text = LocalDatabaseManager.instance.JemCount.ToString()+" J";
        highScore.text =LocalDatabaseManager.instance.HighScore;
        ticket.text = LocalDatabaseManager.instance.Ticket.ToString();
        equipment.sprite = partsIcons[selectedParts[LocalDatabaseManager.instance.CurrentParts]];

        // Set Parts Unlock&Equipment
        for (int i = 0; i < 4; i++)       
            if (int.Parse(LocalDatabaseManager.instance.HighScore) > i * 10)
                parts[i].GetComponent<PartsElement>().PartsUnlock();
       
        currentPart= GameObject.Find("Parts" + LocalDatabaseManager.instance.CurrentParts);
        if (currentPart != null)
            currentPart.GetComponent<Animation>().Play("PartsUiEquipAnim");

        // Set Settings&Tips popup
        bgmSlider.value = SoundManager.instance.GetBgmVolume();
        sfxSlider.value = SoundManager.instance.GetSfxVolume();
        bgmSlider.onValueChanged.AddListener(SetBgmSlider);
        sfxSlider.onValueChanged.AddListener(SetSfxSlider);

        bloomToggle.SetSwitch(GameManger.instance.onBloomEffect);
        hitToggle.SetSwitch(GameManger.instance.onHitEffect);

        tipList.onValueChanged.AddListener(delegate { SetDropdown(tipList.value); });

        // Set Achievements
        SetAchievementFlag();

        // Prepare Ui Effect  settings
        bgParticle.Play();
        originalJemScale = jemIcon.transform.localScale;
    }
    #endregion

    #region Goto Play Mode(Start Game)
    /// <summary>
    /// Make Fade-out effect when the 'Battle' button clicked
    /// </summary>
    public void ChangeScene(bool useTicket)
    {
        GameManger.instance.isTicketMode = useTicket;
        SoundManager.instance.PlaySFX("BasicButtonSound");
        StartCoroutine("FadeOut");
    }

    IEnumerator FadeOut()
    {
        panel.gameObject.SetActive(true);
        Color alpha = panel.color;
        while (alpha.a < 1)
        {
            currentTime += Time.deltaTime / fadeoutTime;
            alpha.a = Mathf.Lerp(0, 1, currentTime);    // make smooth effect by modifying the alpha value using linear interpolation
            panel.color = alpha;
            yield return null;
        }
        GameManger.instance.StartGame();
    }
    #endregion

    #region Fragment Change
    // Change the alpha value( 0.3 or 1) to Emphasize the selected fragment button
    private void ButtonAlphaChange(Image image, float a)
    {
        Color alpha = image.color;
        alpha.a = a;
        image.color = alpha;
    }

    /// <summary>
    /// Convet to each Fragment by setting their parents and activate the animation
    /// </summary>
    /// <param name="targetFragment"></param>
    public void OnClickFragmentChange(int targetFragment)
    {
        if (currentFragment == targetFragment || isConvertingUi)
            return;

        isConvertingUi = true;
        SoundManager.instance.PlaySFX("BasicButtonSound");

        // Set the alpha value of each fragment button(if selected, assign 1)
        foreach (Image i in fragmentButtons)
            ButtonAlphaChange(i, 0.15f);
        switch (targetFragment)
        {
            case 0:
                ButtonAlphaChange(fragmentButtons[0], 1f);
                break;
            case 1:
                ButtonAlphaChange(fragmentButtons[1], 1f);
                break;
            case 2:
                ButtonAlphaChange(fragmentButtons[2], 1f);
                break;
        }

        // Calc Gap between current and target fragments
        float moveAmount = (currentFragment - targetFragment) * 1440;
        currentFragment = targetFragment;
        StartCoroutine(MoveFragment(moveAmount));
    }

    // use Coroutine + DOTween
    IEnumerator MoveFragment(float distance)
    {
        var tween = mainUiContainer.DOLocalMoveX(mainUiContainer.localPosition.x + distance, 1f);
        yield return tween.WaitForCompletion();
        isConvertingUi = false; //enable fragment change after converting finished
    }
    #endregion

    #region Parts Equipment Animation
    /// <summary>
    /// Converting Parts in Lobby Scene, Parts Fragment
    /// </summary>
    /// <param name="part"></param>
    public void EquipParts(GameObject part)
    {
        // Conduct converting if not exceptional situation
        if (part == null || currentPart==part || isConvertingUi)
            return;
        if (!part.GetComponent<PartsElement>().IsUnlocked)
        {
            OpenUnlockPopup(part.transform.name[5..]);
            return;
        }
        isConvertingUi = true;
        SoundManager.instance.PlaySFX("PartsEquipSound");

        // Converting Animation
        part.GetComponent<Animation>().Play("PartsUiEquipAnim");
        currentPart.GetComponent<Animation>().Play("PartsUiOffAnim");
        currentPart = part;

        // Update newly equiped part to the database manager
        LocalDatabaseManager.instance.CurrentParts = part.transform.name.Substring(5);
        LocalDatabaseManager.instance.SavePartsData();
        equipment.sprite = partsIcons[selectedParts[LocalDatabaseManager.instance.CurrentParts]];
        switch (LocalDatabaseManager.instance.CurrentParts)
        {
            case "Missile":
                LocalDatabaseManager.instance.PartsValue = LocalDatabaseManager.instance.PartsMissile;
                break;
            case "Laser":
                LocalDatabaseManager.instance.PartsValue = LocalDatabaseManager.instance.PartsLaser;
                break;
            case "Barrier":
                LocalDatabaseManager.instance.PartsValue = LocalDatabaseManager.instance.PartsBarrier;
                break;
            case "Emp":
                LocalDatabaseManager.instance.PartsValue = LocalDatabaseManager.instance.PartsEmp;
                break;
        }
        Invoke("UiConvertingState", 0.6f);
    }
    private void UiConvertingState()
    {
        isConvertingUi = !isConvertingUi;
    }
    #endregion

    #region When Parts Upgrade button clicked
    /// <summary>
    /// On Parts Upgrade Button Clicked
    /// </summary>
    /// <param name="index">param indicated 'which' button clicked, the order matters</param>
    public void OnUpgradeButtonClicked(int index)
    {
        bool returnFlag = false;
        Vector3 originalScale = partsUpgradeButtons[index].transform.localScale;

        // return if current value is already max
        if (partsUpgradeInfo[index] >= LocalDatabaseManager.instance.MaxUpgradeInfo[index])
            returnFlag = true;
        // return if current jem is insufficient
        else if (LocalDatabaseManager.instance.PartsUpgradeJem[selectedPartsIdx, index, partsUpgradeInfo[index]] > LocalDatabaseManager.instance.JemCount)
            returnFlag = true;

        if (returnFlag)
        {
            SoundManager.instance.PlaySFX("ButtonDenied");
            if (!isTweening)    // Prevent multi-clicking
            {
                isTweening = true;
                partsUpgradeButtons[index].transform.DOPunchPosition(new Vector3(20, 0, 0), 0.5f, 10, 1f).OnComplete(() =>
                {
                    isTweening = false;
                });
            }
            return;
        }

        SoundManager.instance.PlaySFX("PartsUpgradeSound");
        if (!isTweening)    // Prevent multi-clicking
        {
            isTweening = true;
            partsUpgradeButtons[index].transform.DOPunchScale(originalScale * punchScale, 0.2f, 0, 1f).OnComplete(() =>
            {
                isTweening = false;
            });
        }

        // set Local data & UI components
        LocalDatabaseManager.instance.JemCount -= LocalDatabaseManager.instance.PartsUpgradeJem[selectedPartsIdx, index, partsUpgradeInfo[index]];
        jemCount.text = LocalDatabaseManager.instance.JemCount.ToString() + " J";
        partsUpgradeInfo[index] += 1;
        SetPartsUpgradeJemText();
        LocalDatabaseManager.instance.SavePartsData();
        LocalDatabaseManager.instance.SaveGameData();
    }

    private void SetPartsUpgradeJemText()
    {
        int description1 = (int)(LocalDatabaseManager.instance.PartsStatInfo[currentParts][0, partsUpgradeInfo[0]] * 100);
        int description2 = (int)(LocalDatabaseManager.instance.PartsStatInfo[currentParts][1, partsUpgradeInfo[1]] * 100);
        ability1Description.text = partsInfo[selectedPartsIdx, 0, 1] + description1.ToString();
        ability2Description.text = partsInfo[selectedPartsIdx, 1, 1] + description2.ToString();
        ability3Description.text = partsInfo[selectedPartsIdx, 2, 1];


        if (partsUpgradeInfo[0] >= LocalDatabaseManager.instance.MaxUpgradeInfo[0])
            ability1UpgradeJem.text = "Max";
        else
            ability1UpgradeJem.text = LocalDatabaseManager.instance.PartsUpgradeJem[selectedPartsIdx, 0, partsUpgradeInfo[0]].ToString() + " J";

        if (partsUpgradeInfo[1] >= LocalDatabaseManager.instance.MaxUpgradeInfo[1])
            ability2UpgradeJem.text = "Max";
        else
            ability2UpgradeJem.text = LocalDatabaseManager.instance.PartsUpgradeJem[selectedPartsIdx, 1, partsUpgradeInfo[1]].ToString() + " J";

        if (partsUpgradeInfo[2] >= LocalDatabaseManager.instance.MaxUpgradeInfo[2])
            ability3UpgradeJem.text = "Max";
        else
            ability3UpgradeJem.text = LocalDatabaseManager.instance.PartsUpgradeJem[selectedPartsIdx, 2, partsUpgradeInfo[2]].ToString() + " J";
    }
    #endregion

    #region Achievement Effect
    /// <summary>
    /// Notify User for any new cleared achievements exist by show icon in the bottom Ui
    /// </summary>
    public void SetAchievementFlag()
    {
        if (AchievementManager.instance.IsNewAchievementCleared())
            achievementFlag.SetActive(true);
        else
            achievementFlag.SetActive(false);
    }

    /// <summary>
    /// Used for Jem Gaining effect when achievement reward cleared
    /// </summary>
    /// <param name="startPos"></param>
    public void SetJem(Transform startPos)
    {
        jemCount.text = LocalDatabaseManager.instance.JemCount.ToString() + " J";

        uiParticleBase.parent = startPos;
        uiParticleBase.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        uiParticleBase.parent = root;

        uiParticle.Stop();
        uiParticle.Clear();
        uiParticle.Play();
    }

    /// <summary>
    /// for Jem icon in the top Ui, Play punch effect when jem particle touched
    /// </summary>
    public void JemParticleEffect()
    {
        if (!isTweening)
        {
            isTweening = true;
            jemIcon.transform.DOPunchScale(originalJemScale * 0.5f, 0.1f, 0, 1f).OnComplete(() =>
            {
                isTweening = false;
            });
        }
    }
    #endregion

    #region Manage Popup Ui
    /// <summary>
    /// OnClick Parts Upgrade button
    /// </summary>
    /// <param name="name"></param>
    public void OpenPartsUpgradeBase(string name)
    {
        ClosePopupUi();
        isPopupOpen = true;
        bgParticle.Stop();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        currentParts = name;
        partsUpgradeBase.SetActive(true);
        selectedPartsIdx = selectedParts[currentParts];
        switch (selectedPartsIdx)
        {
            case 0:
                partsUpgradeInfo = LocalDatabaseManager.instance.PartsMissile;
                break;
            case 1:
                partsUpgradeInfo = LocalDatabaseManager.instance.PartsLaser;
                break;
            case 2:
                partsUpgradeInfo = LocalDatabaseManager.instance.PartsBarrier;
                break;
            case 3:
                partsUpgradeInfo = LocalDatabaseManager.instance.PartsEmp;
                break;
            default:
                Debug.Log("selected part is out of index range");
                break;
        }

        // Set each UI Component with Selected Parts
        selectedPartsName.text = currentParts;
        selectedPartsDescription.text = partsDescriptions[selectedPartsIdx];
        selectedPartsImage.transform.GetChild(selectedPartsIdx).gameObject.SetActive(true);
        ability1Title.text = partsInfo[selectedPartsIdx, 0, 0];
        ability2Title.text = partsInfo[selectedPartsIdx, 1, 0];
        ability3Title.text = partsInfo[selectedPartsIdx, 2, 0];
        
        SetPartsUpgradeJemText();
    }

    public void ClosePartsUpgradeBase()
    {
        if (!partsUpgradeBase.activeSelf)
            return;
        bgParticle.Play();
        isPopupOpen = false;
        SoundManager.instance.PlaySFX("BasicButtonSound");
        selectedPartsImage.transform.GetChild(selectedPartsIdx).gameObject.SetActive(false);
        partsUpgradeBase.SetActive(false);
    }

    public void OpenExitPopup()
    {
        ClosePopupUi();
        bgParticle.Stop();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = true;
        exitPopup.SetActive(true);
    }

    public void CloseExitPopup()
    {
        if (!exitPopup.activeSelf)
            return;
        bgParticle.Play();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = false;
        exitPopup.SetActive(false);
    }

    public void OpenBattlePopup()
    {
        ClosePopupUi();
        bgParticle.Stop();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = true;
        battlePopup.SetActive(true);
        ticketNum.text = LocalDatabaseManager.instance.Ticket + " / 10";
    }

    public void CloseBattlePopup()
    {
        if (!battlePopup.activeSelf)
            return;
        bgParticle.Play();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = false;
        battlePopup.SetActive(false);
    }

    public void OpenSettingsPopup()
    {
        ClosePopupUi();
        bgParticle.Stop();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = true;
        settingsPopup.SetActive(true);
        settingsPanel.transform.DOLocalMoveY(1000f, 0.2f).SetEase(Ease.InBack).SetRelative(true);
    }

    public void CloseSettingsPopup()
    {
        if (!settingsPopup.activeSelf)
            return;
        bgParticle.Play();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = false;
        settingsPanel.transform.DOLocalMoveY(-1000f, 0.2f).SetEase(Ease.InBack).OnComplete(() => settingsPopup.SetActive(false));
    }

    public void OpenUnlockPopup(string partName)
    {
        ClosePopupUi();
        bgParticle.Stop();
        isPopupOpen = true;
        unlockPopup.SetActive(true);
        SoundManager.instance.PlaySFX("BasicButtonSound");
        string wave ="Wave";
        switch (partName)
        {
            case "Laser":
                wave += " 10";
                break;
            case "Barrier":
                wave += " 20";
                break;
            case "Emp":
                wave += " 30";
                break;
            default:
                wave = "Error";
                break;

        }
        unlockConditionText.text = "Clear "+ "<color=#00CEFF><size=70>" + wave +"</size></color>"+ " to Unlock " + "<color=#00CEFF><size=70>" + partName + "</size></color>";
    }

    public void CloseUnlockPopup()
    {
        if (!unlockPopup.activeSelf)
            return;
        bgParticle.Play();
        isPopupOpen = false;
        unlockPopup.SetActive(false);
    }

    public void OpenTipsPopup()
    {
        ClosePopupUi();
        bgParticle.Stop();
        SoundManager.instance.PlaySFX("BasicButtonSound");
        isPopupOpen = true;
        tipsPopup.SetActive(true);
        tipsPanel.transform.DOLocalMoveY(1000f, 0.2f).SetEase(Ease.InBack).SetRelative(true);
    }

    public void CloseTipsPopup()
    {
        if (!tipsPopup.activeSelf)
            return;
        bgParticle.Play();
        isPopupOpen = false;
        tipsPanel.transform.DOLocalMoveY(-1000f, 0.2f).SetEase(Ease.InBack).OnComplete(() => tipsPopup.SetActive(false));
    }

    /// <summary>
    /// Close All Popup in the Lobby Scene
    /// </summary>
    public void ClosePopupUi()
    {
        if (!isPopupOpen)
            return;
        ClosePartsUpgradeBase();
        CloseExitPopup();
        CloseBattlePopup();
        CloseSettingsPopup();
        CloseUnlockPopup();
        CloseTipsPopup();
    }
    #endregion

    #region Set Settings/Tips Popup Ui
    private void SetBgmSlider(float value)
    {
        SoundManager.instance.SetBgmVolume(value);
    }

    private void SetSfxSlider(float value)
    {
        SoundManager.instance.SetSfxVolume(value);
    }

    private void SetDropdown(int option)
    {
        foreach (GameObject tip in tips)
            tip.SetActive(false);
        tips[option].SetActive(true);
    }
    #endregion

    #region Interactions in the Battle popup (watch AD, use Ticket)
    public void OnAdsButtonClick()
    {
        adsManager.ShowRewardedAd();
    }

    public void EndAdsReward()
    {
        ChangeScene(true);
    }

    public void OnClickUseTicketButton()
    {
        if (LocalDatabaseManager.instance.Ticket < 10)
        {
            StartCoroutine(MakeToast("You  have  Insufficient  Tickets"));
            return;
        }
        SoundManager.instance.PlaySFX("BasicButtonSound");
        LocalDatabaseManager.instance.Ticket -= 10;
        LocalDatabaseManager.instance.SaveGameData();
        ChangeScene(true);
    }

    public void DoToast(string msg)
    {
        StartCoroutine(MakeToast(msg));
    }

    IEnumerator MakeToast(string msg)
    {
        toastBase.SetActive(true);
        toastMessage.text = msg;

        var tween= toastMessage.DOFade(0, 1f).SetEase(Ease.InExpo);
        yield return tween.WaitForCompletion();

        toastMessage.DOFade(1, 0);
        toastBase.SetActive(false);
    }
    #endregion

    #region System Settings
    public void OnExitButton()
    {
        Application.Quit();
    }
    #endregion
}
