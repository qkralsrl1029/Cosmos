using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataBaseEntity;

public class LocalDatabaseManager : MonoBehaviour
{
    // Save and Manage all user data( high score, jem, parts info)

    // make Singleton pattern
    public static LocalDatabaseManager instance;

    #region User Data
    public int FirstPlay { get; private set; } = 0;
    public int JemCount { get; set; } = 0;
    private string highScore = "1";
    public string HighScore
    {
        get { return highScore; }
        set { highScore = value == "Bonus" ? "40" : highScore == "Bonus" ? "40" : Mathf.Max(int.Parse(value), int.Parse(highScore)).ToString(); }
    }

    public string CurrentParts { get; set; } = "Missile";

    //Parts Upgrade info, each index indicates how many times it had been upgraded
    public int[] MaxUpgradeInfo { get; } = { 19, 19, 1 };
    public int[] PartsMissile { get; set; } = { 0, 0, 0 };
    public int[] PartsBarrier { get; set; } = { 0, 0, 0 };
    public int[] PartsLaser { get; set; } = { 0, 0, 0 };
    public int[] PartsEmp { get; set; } = { 0, 0, 0 };
    public int[] PartsValue { get; set; } = { 0, 0, 0 };
    private DataBase PartsDB;

    public int Ticket { get; set; } = 3;

    public int[] AchieveCurValue { get; set; }
    public int[] AchieveCurLevel { get; set; }

    /// <summary>
    /// 사용법(기본적으로 3차원 배열 형태, 근데 딕셔너리를 사용해서 첫번째 원소는 파츠 이름으로 쉽게 알아볼수 있도록..)
    /// 미사일 파츠의 첫번째 강화 정도를 알고 싶다면?
    /// LocalDatabaseManager.instance.PartsStatInfo["Missile"][0, LocalDatabaseManager.instance.PartsMissile[0]];
    /// 두번째 능력의 강화 정도를 알고싶다면?
    /// LocalDatabaseManager.instance.PartsStatInfo["Missile"][1, LocalDatabaseManager.instance.PartsMissile[1]];
    /// PartsStatInfo 자체는 데이터 테이블 느낌으로 readonly, PartsMissile에서 저장하는 강화 '정도'를 이용해서 현재 강화 정도에 해당하는 값 '참조'
    /// </summary>
    public Dictionary<string, float[,]> PartsStatInfo { get; set; } = new Dictionary<string, float[,]>
    {
        {"Missile",new float[3,20]}, {"Laser",new float[3,20]}, {"Barrier",new float[3,20]}, {"Emp",new float[3,20]},
    };

    /*
         {
        // 각 파츠별로 세 가지 속성(공격력, 공격 속도 - 공통, 특수 능력 해방 여부)- 순서대로
        //                                                 공격력                                                      공격속도                                     특수능력 해방(1이면 해방)
        {"Missile",new float[3,20]{ { 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2.1f, 2.3f, 3f }, { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f, 0.325f, 0.35f, 0.375f, 0.4f, 0.425f, 0.45f, 0.475f, 0.5f, 0.525f, 0.55f, 0.575f, 0.6f, 0.625f, 0.65f, 0.675f }, { 0, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } } },
        {"Laser",new float[3,20]{ { 2.4f, 2.5f, 2.6f, 2.7f, 2.8f, 2.9f, 3f, 3.1f, 3.2f, 3.3f, 3.4f, 3.5f, 3.6f, 3.7f, 3.8f, 3.9f, 4f, 4.4f, 4.8f, 5.5f }, { 0.4f, 0.45f, 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f, 1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f }, { 0, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } } },
        {"Barrier",new float[3,20]{ { 0.05f, 0.075f, 0.1f, 0.125f, 0.15f, 0.175f, 0.2f, 0.225f, 0.25f, 0.275f, 0.3f, 0.325f, 0.35f, 0.375f, 0.4f, 0.425f, 0.45f, 0.475f, 0.5f, 0.525f}, { 0.775f, 0.75f, 0.725f, 0.7f, 0.675f, 0.65f, 0.625f, 0.6f, 0.575f, 0.55f, 0.525f, 0.5f, 0.475f, 0.45f, 0.425f, 0.4f, 0.375f, 0.35f, 0.325f, 0.3f }, { 0, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } } },
        {"Emp",new float[3,20]{ { 0.15f, 0.175f, 0.2f, 0.225f, 0.25f, 0.275f, 0.3f, 0.325f, 0.35f, 0.375f, 0.4f, 0.425f, 0.45f, 0.475f, 0.5f, 0.525f, 0.55f, 0.575f, 0.6f, 0.625f}, { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.11f, 1.12f, 1.13f, 1.14f, 1.15f, 1.16f, 1.17f, 1.18f, 1.19f, 1.2f }, { 0, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } } },
    };
     */

    // readonly data table of amount of the jem comsumtion for each parts' upgrade
    public int[,,] PartsUpgradeJem { get; set; }

    /*        {
    {{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{2000,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 } },
    {{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{2000,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 } },
    {{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{2000,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 } },
    {{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{100,150,200,250,300,350,400,450,500,550,600,650,700,750,800,850,900,950,1000 },{2000,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 } },
};*/

    #endregion

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
            Destroy(this.gameObject);

        LoadData();
        switch (CurrentParts)
        {
            case "Missile":
                PartsValue = PartsMissile;
                break;
            case "Laser":
                PartsValue = PartsLaser;
                break;
            case "Barrier":
                PartsValue = PartsBarrier;
                break;
            case "Emp":
                PartsValue = PartsEmp;
                break;
        }
    }

    private void Start()
    {
        LoadPartsStatData();
    }

    /// <summary>
    /// Load all data at the start of the Game, in Lobby Scene
    /// </summary>
    public void LoadData()
    {
        if (PlayerPrefs.HasKey("FirstPlay"))
            FirstPlay = PlayerPrefs.GetInt("FirstPlay");

        if (PlayerPrefs.HasKey("JemCount"))
            JemCount = PlayerPrefs.GetInt("JemCount");

        if (PlayerPrefs.HasKey("HighScore"))
            HighScore = PlayerPrefs.GetString("HighScore");

        if (PlayerPrefs.HasKey("Ticket"))
            Ticket = PlayerPrefs.GetInt("Ticket");
        if (PlayerPrefs.HasKey("CurrentParts"))
            CurrentParts = PlayerPrefs.GetString("CurrentParts");

        // string format : "val1, val2, val3"
        string[] temp;
        if (PlayerPrefs.HasKey("PartsMissile"))
        {
            temp = PlayerPrefs.GetString("PartsMissile").Split(',');
            for (int i = 0; i < PartsMissile.Length; i++)
                PartsMissile[i] = int.Parse(temp[i]);
        }
        if (PlayerPrefs.HasKey("PartsBarrier"))
        {
            temp = PlayerPrefs.GetString("PartsBarrier").Split(',');
            for (int i = 0; i < PartsBarrier.Length; i++)
                PartsBarrier[i] = int.Parse(temp[i]);
        }
        if (PlayerPrefs.HasKey("PartsLaser"))
        {
            temp = PlayerPrefs.GetString("PartsLaser").Split(',');
            for (int i = 0; i < PartsLaser.Length; i++)
                PartsLaser[i] = int.Parse(temp[i]);
        }
        if (PlayerPrefs.HasKey("PartsEmp"))
        {
            temp = PlayerPrefs.GetString("PartsEmp").Split(',');
            for (int i = 0; i < PartsEmp.Length; i++)
                PartsEmp[i] = int.Parse(temp[i]);
        }
    }

    private void LoadPartsStatData()
    {
        PartsDB = GameManger.instance.database;

        PartsUpgradeJem = new int[4, 3, PartsDB.Missile.Count]; 

        PartsStatInfo = new Dictionary<string, float[,]>
        {
            {"Missile",new float[3,PartsDB.Missile.Count]}, 
            {"Laser",new float[3,PartsDB.Laser.Count]}, 
            {"Barrier",new float[3,PartsDB.Barrier.Count]}, 
            {"Emp",new float[3,PartsDB.Emp.Count]},
        };

        for (int i = 0; i < PartsDB.Missile.Count; i++)
        {
            PartsStatInfo["Missile"][0, i] = PartsDB.Missile[i].damage;
            PartsStatInfo["Missile"][1, i] = PartsDB.Missile[i].attackSpeed;
            PartsStatInfo["Missile"][2, i] = PartsDB.Missile[i].expertSkill;
            PartsUpgradeJem[0, 0, i] = PartsDB.Missile[i].damageUpgradeJem;
            PartsUpgradeJem[0, 1, i] = PartsDB.Missile[i].attackSpeedUpgradeJem;
            PartsUpgradeJem[0, 2, i] = PartsDB.Missile[i].expertSkillUpgradeJem;
        }

        PartsStatInfo["Laser"] = new float[3, PartsDB.Laser.Count];
        for (int i = 0; i < PartsDB.Laser.Count; i++)
        {
            PartsStatInfo["Laser"][0, i] = PartsDB.Laser[i].damage;
            PartsStatInfo["Laser"][1, i] = PartsDB.Laser[i].attackSpeed;
            PartsStatInfo["Laser"][2, i] = PartsDB.Laser[i].expertSkill;
            PartsUpgradeJem[1, 0, i] = PartsDB.Laser[i].damageUpgradeJem;
            PartsUpgradeJem[1, 1, i] = PartsDB.Laser[i].attackSpeedUpgradeJem;
            PartsUpgradeJem[1, 2, i] = PartsDB.Laser[i].expertSkillUpgradeJem;
        }

        PartsStatInfo["barrier"] = new float[3, PartsDB.Barrier.Count];
        for (int i = 0; i < PartsDB.Barrier.Count; i++)
        {
            PartsStatInfo["Barrier"][0, i] = PartsDB.Barrier[i].damage;
            PartsStatInfo["Barrier"][0, i] = PartsDB.Barrier[i].damage;
            PartsStatInfo["Barrier"][1, i] = PartsDB.Barrier[i].attackSpeed;
            PartsStatInfo["Barrier"][2, i] = PartsDB.Barrier[i].expertSkill;
            PartsUpgradeJem[2, 0, i] = PartsDB.Barrier[i].damageUpgradeJem;
            PartsUpgradeJem[2, 1, i] = PartsDB.Barrier[i].attackSpeedUpgradeJem;
            PartsUpgradeJem[2, 2, i] = PartsDB.Barrier[i].expertSkillUpgradeJem;
        }

        PartsStatInfo["Emp"] = new float[3, PartsDB.Emp.Count];
        for (int i = 0; i < PartsDB.Emp.Count; i++)
        {
            PartsStatInfo["Emp"][0, i] = PartsDB.Emp[i].damage;
            PartsStatInfo["Emp"][1, i] = PartsDB.Emp[i].attackSpeed;
            PartsStatInfo["Emp"][2, i] = PartsDB.Emp[i].expertSkill;
            PartsUpgradeJem[3, 0, i] = PartsDB.Emp[i].damageUpgradeJem;
            PartsUpgradeJem[3, 1, i] = PartsDB.Emp[i].attackSpeedUpgradeJem;
            PartsUpgradeJem[3, 2, i] = PartsDB.Emp[i].expertSkillUpgradeJem;
        }
    }

    /// <summary>
    /// Save Ingame Data, jem+high score 
    /// </summary>
    public void SaveGameData()
    {
        PlayerPrefs.SetInt("JemCount", JemCount);
        PlayerPrefs.SetString("HighScore", HighScore);
        PlayerPrefs.SetInt("Ticket", Ticket);
    }

    public void SaveFirstTime()
    {
        FirstPlay = 1;
        PlayerPrefs.SetInt("FirstPlay", FirstPlay);
    }

    /// <summary>
    /// Save Parts Data, currnet equiped parts+parts upgrade info
    /// </summary>
    public void SavePartsData()
    {
        PlayerPrefs.SetString("CurrentParts", CurrentParts);
        string temp = "";
        foreach (int part in PartsMissile)
            temp += (part.ToString() + ",");
        PlayerPrefs.SetString("PartsMissile", temp);
        temp = "";
        foreach (int part in PartsBarrier)
            temp += (part.ToString() + ",");
        PlayerPrefs.SetString("PartsBarrier", temp);
        temp = "";
        foreach (int part in PartsLaser)
            temp += (part.ToString() + ",");
        PlayerPrefs.SetString("PartsLaser", temp);
        temp = "";
        foreach (int part in PartsEmp)
            temp += (part.ToString() + ",");
        PlayerPrefs.SetString("PartsEmp", temp);
    }

    public void LoadAchieveData()
    {
        AchieveCurValue = new int[AchievementManager.instance.achieveDB.Achieves.Count];
        AchieveCurLevel = new int[AchievementManager.instance.achieveDB.Achieves.Count];
        string[] temp;
        if (PlayerPrefs.HasKey("AchieveCurValue"))
        {
            temp = PlayerPrefs.GetString("AchieveCurValue").Split(',');
            for (int i = 0; i < AchieveCurValue.Length; i++)
                AchieveCurValue[i] = int.Parse(temp[i]);
        }
        if (PlayerPrefs.HasKey("AchieveCurLevel"))
        {
            temp = PlayerPrefs.GetString("AchieveCurLevel").Split(',');
            for (int i = 0; i < AchieveCurLevel.Length; i++)
                AchieveCurLevel[i] = int.Parse(temp[i]);
        }
    }

    public void SaveAchieveData()
    {
        string temp = "";
        foreach (int value in AchieveCurValue)
            temp += value.ToString() + ",";
        PlayerPrefs.SetString("AchieveCurValue", temp);
        temp = "";
        foreach (int level in AchieveCurLevel)
            temp += level.ToString() + ",";
        PlayerPrefs.SetString("AchieveCurLevel", temp);
    }
}
