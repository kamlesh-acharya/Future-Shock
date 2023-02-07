using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapsDetails : MonoBehaviour
{
    [SerializeField]
    private string[] allMaps;
    [SerializeField]
    private bool changeMapBetweenRounds = true;

    private static MapsDetails _instance;
    public static MapsDetails Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("MapsDetails is Null");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool GetChangeMapBetweenRounds()
    {
        return changeMapBetweenRounds;
    }

    public string[] GetAllMaps()
    {
        return allMaps;
    }
}
