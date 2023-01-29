using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{

    
    public bool isAutomatic;
    [SerializeField]
    private float timeBetweenShot = 0.1f, heatPerShot = 1f;
    [SerializeField]
    private int shotDamage;
    public GameObject muzzleFlash;

    public float GetTimeBetweenShot()
    {
        return timeBetweenShot;
    }

    public float GetheatPerShot()
    {
        return heatPerShot;
    }

    public int GetDamagePerShot()
    {
        return shotDamage;
    }
}
