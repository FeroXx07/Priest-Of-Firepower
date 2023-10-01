using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerShooter : MonoBehaviour
{
    Vector2 shootDir;
    [SerializeField] float range;
    [SerializeField] LineRenderer shootMarker;
    Transform weaponHolder;
    [SerializeField] float weaponOffset = .5f;
     private SpriteRenderer playerSR;
    public static Action<Vector2> OnShoot;
    public static Action OnReload;

    void Start()
    {
        shootMarker.positionCount = 2;
        playerSR = GetComponent<SpriteRenderer>();
     
    }
    private void OnEnable()
    {
        WeaponSwitcher.OnWeaponSwitch += ChangeHolder;
    }
    void Update()
    {
        // Get the mouse position in world coordinates.
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 shootDir = (mousePos - transform.position).normalized;
        Vector3 lineEnd = transform.position + shootDir * range;

        UpdateShootMarker(lineEnd);

        //
        if (shootDir.x < 0)
            Flip(true);
        else
            Flip(false);


        // Calculate the rotation angle in degrees.
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

        // Create a Quaternion for the rotation.
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));


        weaponHolder.transform.rotation = targetRotation;

        weaponHolder.transform.position = transform.position + shootDir * weaponOffset;  

        if (Input.GetMouseButton(0))
            OnShoot?.Invoke(shootDir);

        if (Input.GetKeyDown(KeyCode.R))
            OnReload?.Invoke();
    }

    void UpdateShootMarker(Vector3 finalPos)
    {
        // Set the positions of the line renderer.
        shootMarker.SetPosition(0, transform.position);
        shootMarker.SetPosition(1, finalPos);
    }

    void Flip(bool flip)
    {
        playerSR.flipX = flip;
    }    

    void ChangeHolder(Transform holder)
    {
        weaponHolder = holder;
    }
}

