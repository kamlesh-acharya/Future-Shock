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

    [SerializeField]
    private float adsZoom;

    [SerializeField]
    private AudioSource shotSound;

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

    public float GetAdsZoom()
    {
        return adsZoom;
    }

    public void PlayShotSound()
    {
        shotSound.Stop();
        shotSound.Play();
    }
}
