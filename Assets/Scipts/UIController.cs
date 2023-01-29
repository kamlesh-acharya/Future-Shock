using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text overheatedMessage;
    [SerializeField]
    private Slider weaponHeatSlider;

    [SerializeField]
    private GameObject deathScreen;
    [SerializeField]
    private TMP_Text deathText;

    [SerializeField]
    private Slider healthSlider;

    [SerializeField]
    private TMP_Text killsText, deathsText;

    [SerializeField]
    private GameObject leaderboard;
    [SerializeField]
    private LeaderboardPlayer leaderboardPlayerDisplay;

    [SerializeField]
    private GameObject endScreen;

    private static UIController _instance;
    public static UIController Instance
    {
        get
        {
            if(_instance == null)
            {
                Debug.LogError("UIController is Null");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    public void SetOverHeatedMessage(bool flag)
    {
        if (flag)
        {
            overheatedMessage.gameObject.SetActive(flag);
        }
        else
        {
            overheatedMessage.gameObject.SetActive(flag);
        }
    }

    public void SetHeatValueToSlider(float value)
    {
        weaponHeatSlider.value = value;
    }

    public void SetHealthValueToSlider(float value)
    {
        healthSlider.value = value;
    }

    public void SetHealthSliderMax(float value)
    {
        healthSlider.maxValue = value;
    }

    public void PlayerDieMessage(bool flag, string message = "")
    {
        deathText.text = message;
        deathScreen.SetActive(flag);
    }

    public void SetKillText(int kills)
    {
        killsText.text = "Kills: " + kills; 
    }

    public void SetDeathText(int deaths)
    {
        deathsText.text = "Death: " + deaths;
    }

    public void SetLeaderboardActive()
    {
        leaderboard.SetActive(true);
    }

    public void SetLeaderboardPlayerDisplay()
    {
        leaderboardPlayerDisplay.gameObject.SetActive(false);
    }

    public LeaderboardPlayer GetLeaderboardPlayerDisplay()
    {
        return leaderboardPlayerDisplay;
    }

    public GameObject GetLeaderboard()
    {
        return leaderboard;
    }

    public void SetEndScreen(bool flag)
    {
        endScreen.SetActive(flag);
    }
}
