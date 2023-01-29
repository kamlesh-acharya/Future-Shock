using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Transform viewPoint;
    [SerializeField]
    private float mouseSenstivity = 1.3f;
    [SerializeField]
    private bool invertLook;
    private float verticalRotStore;
    private Vector2 mouseInput;

    [SerializeField]
    private float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;
    private float jumpForce = 12f, gravityMod = 2.5f;

    [SerializeField]
    private Transform groundCheckPoint;
    private bool isGrounded;
    [SerializeField]
    private LayerMask groundLayers;

    [SerializeField]
    private CharacterController charCon;
    private Camera cam;

    [SerializeField]
    private GameObject bulletImpact;
    //[SerializeField]
    //private float timeBetweenShots = 0.1f;
    private float shotCounter;
    [SerializeField]
    private float muzzleDisplayTime;
    private float muzzleCounter;

    [SerializeField]
    private float maxHeat = 10f, /* heatPerShot = 1f, */ coolRate = 4f, overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    [SerializeField]
    private Gun[] allGuns;
    private int selectedGun = 1;

    [SerializeField]
    private GameObject playerHitImpact;

    [SerializeField]
    private int maxHealth = 100;
    private int currentHealth;

    [SerializeField]
    private Animator playerAnim;

    [SerializeField]
    private GameObject playerModel;

    [SerializeField]
    private Transform modelGunPoint, gunHolder;

    // Start is called before the first frame update
    void Start()
    {
        //It locks the cursor to center of the screen
        Cursor.lockState = CursorLockMode.Locked;

        // Assining Main Camera to cam
        cam = Camera.main;

        //allGuns[selectedGun].gameObject.SetActive(true);
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            playerModel.SetActive(false);
            UIController.Instance.SetHealthSliderMax(maxHealth);
            UIController.Instance.SetHealthValueToSlider(currentHealth);
        } else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        //Transform newTrans = SpawnManager.Instance.GetSpawnPoint();
        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            if(Cursor.lockState == CursorLockMode.Locked)
            {
                MoveViewPoint();
                MovePlayer();
                AnimatePlayer();
                TakeShotsOnClick();
                SwitchGuns();
            }

            HandleCursor();
        }
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if(MatchManager.Instance.GetCurrentGameState() == MatchManager.GameState.Playing)
            {
                //To update the camera position outside the Player component 
                cam.transform.position = viewPoint.transform.position;
                cam.transform.rotation = viewPoint.transform.rotation;

            } else
            {
                Transform camTransform = MatchManager.Instance.GetCamPosition();
                cam.transform.position = camTransform.position;
                cam.transform.rotation = camTransform.rotation;
            }
        }
    }

    private void SwitchGuns()
    {
        if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;
            if(selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            //SelectGun();
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        } 
        else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;
            if (selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            //SelectGun();
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        }

        for(int i=0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i+1).ToString()))
            {
                selectedGun = i;
                //SelectGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
        }

    }

    private void SelectGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    private void MoveViewPoint()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSenstivity;
        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        if (invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
    }

    private void MovePlayer()
    {
        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        float yVel = movement.y;

        //Normalising the forward and right move in case of diagonal movement and applying activeMoveSpeed
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;

        //Resetting y Velocity after normalising
        movement.y = yVel;

        if (charCon.isGrounded)
        {
            movement.y = 0f;
        }

        // To check the distance between player and ground to stop player from jumping in the air
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayers);

        //Player will only jump when Space is hit and player is on ground
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        //Applying gravity and gravity Mod to player
        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        charCon.Move(movement * Time.deltaTime);
    }

    private void AnimatePlayer()
    {
        playerAnim.SetBool("grounded", isGrounded);
        playerAnim.SetFloat("speed", moveDir.magnitude);
    }

    private void TakeShotsOnClick()
    {
        UIController.Instance.SetHeatValueToSlider(heatCounter/maxHeat);

        if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;
            if(muzzleCounter <= 0)
            {
                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0)
                {

                    Shoot();
                }
            }
            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;
            }
        }

        if (heatCounter <= 0)
        {
            heatCounter = 0;
        }

        UIController.Instance.SetOverHeatedMessage(overHeated);
    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.origin = cam.transform.position;
            
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("We hit " + hit.collider.gameObject.name);
            if(hit.collider.gameObject.CompareTag("Player"))
            {
                Debug.Log("Hit: " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].GetDamagePerShot(), PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject hitBullet = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(hitBullet, 10f);
            }
        }
            
        //shotCounter = timeBetweenShots;
        shotCounter = allGuns[selectedGun].GetTimeBetweenShot();

        //Gun Heat related code below
        //heatCounter += heatPerShot;
        heatCounter += allGuns[selectedGun].GetheatPerShot();

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    [PunRPC]
    private void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    private void TakeDamage(string damager, int damageAmount, int actor)
    {
        if (photonView.IsMine)
        {
            //Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);
            //gameObject.SetActive(false);
            currentHealth -= damageAmount;
            if(currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.Instance.Die(damager);

                //Kill = 0 and Amount = 1 to update for All Players
                MatchManager.Instance.UpdateStatsSend(actor, 0, 1);
            }

            UIController.Instance.SetHealthValueToSlider(currentHealth);
        }
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SelectGun();
        }
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

}
