using System;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] LineRenderer shootMarker;
        [SerializeField] LayerMask layerMask;
        Transform weaponHolder;
        [SerializeField] float weaponOffset = .5f;
        public static Action OnShoot;
        public static Action OnReload;
        public static Action<bool> OnFlip;
        private bool Flipped;
        private float range = 1;
        void Start()
        {
            shootMarker.positionCount = 2;
            Flipped = false;
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

            RaycastHit2D hit = Physics2D.Raycast(transform.position, shootDir, range, layerMask);
            if (hit)
            {
                UpdateShootMarker(hit.point);
            }
            else
            {
                Vector3 lineEnd = transform.position + shootDir * range;

                UpdateShootMarker(lineEnd);
            }

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
                OnShoot?.Invoke();

            if (Input.GetKeyDown(KeyCode.R))
                OnReload?.Invoke();
        }

        void UpdateShootMarker(Vector3 finalPos)
        {
            // Set the positions of the line _spriteRenderer.
            shootMarker.SetPosition(0, transform.position);
            shootMarker.SetPosition(1, finalPos);
        }

        void Flip(bool flip)
        {
            if (Flipped != flip)
            {
                Flipped = flip;
                OnFlip?.Invoke(flip);

                if (flip)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
            }
        }    

        void ChangeHolder(Transform holder)
        {
            weaponHolder = holder;
            Weapon.Weapon wp = holder.GetComponentInChildren<Weapon.Weapon>();
            if(wp != null)
                range = wp.localData.range;
        }
    }
}

