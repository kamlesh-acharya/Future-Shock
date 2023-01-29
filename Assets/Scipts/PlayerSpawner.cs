using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;
    private GameObject player;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private float respawnTime = 5.0f;

    private static PlayerSpawner _instance;

    public static PlayerSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("PlayerSpawner is Null");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void Die(string damager)
    {

        UIController.Instance.PlayerDieMessage(true, "You were killed by " + damager);

        MatchManager.Instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if(player != null)
        {
            StartCoroutine(DestroyPlayerAndRespawn());
        }
        //PhotonNetwork.Destroy(player);
        //SpawnPlayer();
    }

    IEnumerator DestroyPlayerAndRespawn()
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);

        yield return new WaitForSeconds(respawnTime);

        UIController.Instance.PlayerDieMessage(false);
        SpawnPlayer();
    }
}
