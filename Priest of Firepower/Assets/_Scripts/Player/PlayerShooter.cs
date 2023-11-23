using System;
using _Scripts.Networking;
using _Scripts.Weapon;
using UnityEngine;

namespace _Scripts.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] LineRenderer shootMarker;
        [SerializeField] LayerMask layerMask;
        Transform _weaponHolder;
        [SerializeField] float weaponOffset = .5f;
        
        public Action OnShoot;
        public Action OnStartingReload;
        public Action OnReload;
        public Action OnFinishedReload;
        public Action<bool> OnFlip;
        
        private bool _flipped;
        private float _range = 1;
        
        [SerializeField] private Weapon.Weapon _currentWeapon; 
        [SerializeField] private Player _player;
        [SerializeField] private WeaponSwitcher _weaponSwitcher;
        [SerializeField] private UInt64 myId => NetworkManager.Instance.getId;

        private void Awake()
        {
            _weaponSwitcher = GetComponent<WeaponSwitcher>();
        }

        void Start()
        {
            shootMarker.positionCount = 2;
            _flipped = false;
            _player = GetComponent<Player>();
        }
        private void OnEnable()
        {
            _weaponSwitcher.OnWeaponSwitch += ChangeHolder;
        }

        private void OnDisable()
        {
            _weaponSwitcher.OnWeaponSwitch -= ChangeHolder;
        }

        void Update()
        {
            if (myId != _player.GetPlayerId())
                return;
            
            if (Input.GetMouseButton(0))
                OnShoot?.Invoke();

            if (Input.GetKeyDown(KeyCode.R))
                OnReload?.Invoke();
        }

        private void FixedUpdate()
        {
            if (myId != _player.GetPlayerId())
                return;
            
            // Get the mouse position in world coordinates.
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            Vector3 shootDir = (mousePos - transform.position).normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, shootDir, _range, layerMask);
            if (hit)
            {
                UpdateShootMarker(hit.point);
            }
            else
            {
                Vector3 lineEnd = transform.position + shootDir * _range;

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
            
            _weaponHolder.transform.rotation = targetRotation;

            _weaponHolder.transform.position = transform.position + shootDir * weaponOffset;  
        }

        void UpdateShootMarker(Vector3 finalPos)
        {
            // Set the positions of the line _spriteRenderer.
            shootMarker.SetPosition(0, transform.position);
            shootMarker.SetPosition(1, finalPos);
        }

        void Flip(bool flip)
        {
            if (_flipped != flip)
            {
                _flipped = flip;
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
            _weaponHolder = holder;
            _currentWeapon = holder.GetComponentInChildren<Weapon.Weapon>();
            
            if (_currentWeapon != null)
            {
                _range = _currentWeapon.localData.range;
                _currentWeapon.SetPlayerShooter(this);
            }
        }
    }
}

