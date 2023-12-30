using System;
using System.Collections;
using System.IO;
using _Scripts.Attacks;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.Object_Pool;
using _Scripts.ScriptableObjects;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace _Scripts.Weapon
{
    public enum BulletState
    {
        CREATION,
        HIT
    }
    public class Weapon : NetworkBehaviour
    {
        #region Fields

        public WeaponData weaponData;
        public WeaponData localData;
        [SerializeField] GameObject bulletRef; //for testing
        [SerializeField] Transform firePoint;
        private float _timeSinceLastShoot;
        private SpriteRenderer _spriteRenderer;
        [SerializeField] VisualEffect muzzleFlash;
        GameObject _owner;
        public Player.Player shooterOwner { get; private set; }
        bool _localDataCopied = false;
        private AudioSource _audioSource;
        #endregion

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            if (!_localDataCopied) SetData();
        }

        public void SetData() //forces to copy the data, even if the parents are unactive
        {
            if (!_localDataCopied)
            {
                localData = Instantiate(
                    weaponData); // We don't want to modify the global weapon template, but only ours weapon!
                _localDataCopied = true;
            }
        }

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = localData.sprite;          
            _timeSinceLastShoot = 10;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _audioSource = GetComponent<AudioSource>();
        }

        protected override void InitNetworkVariablesList()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();

        }

        public override void OnDisable()
        {
            base.OnDisable();
            localData.reloading = false;
        }

        public override void Update()
        {
            base.Update();
            _timeSinceLastShoot += Time.deltaTime;
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write(localData.damage);
            writer.Write(localData.ammoInMagazine);
            writer.Write(localData.totalAmmo);
            writer.Write(localData.maxAmmoCapacity);
            writer.Write(localData.magazineSize);
            writer.Write(localData.reloadSpeed);
            writer.Write(localData.reloading);
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            localData.damage = reader.ReadInt32();
            localData.ammoInMagazine = reader.ReadInt32();
            localData.totalAmmo = reader.ReadInt32();
            localData.maxAmmoCapacity = reader.ReadInt32();
            localData.magazineSize = reader.ReadInt32();
            localData.reloadSpeed = reader.ReadSingle();
            localData.reloading = reader.ReadBoolean();
            return true;
        }

        #region Reload

        public void Reload()
        {
            if (localData.reloading || localData.totalAmmo <= 0 || localData.ammoInMagazine >= localData.magazineSize ||
                !gameObject.activeSelf)
                return;
            
            if (shooterOwner != null) shooterOwner.OnStartingReload?.Invoke(localData);
            StartCoroutine(Reloading());
            
            int reloadSound = Random.Range(0, localData.reloadSound.Count);
            _audioSource.PlayOneShot(localData.reloadSound[reloadSound]);
        }

        IEnumerator Reloading()
        {
            localData.reloading = true;
            yield return new WaitForSeconds(localData.reloadSpeed);
            if (localData.totalAmmo > 0)
            {
                if (localData.totalAmmo > localData.magazineSize)
                    localData.ammoInMagazine = localData.magazineSize;
                else
                    localData.ammoInMagazine = localData.totalAmmo;
                localData.reloading = false;
            }

            if (shooterOwner != null) shooterOwner.OnFinishedReload?.Invoke(localData);
        }

        #endregion

        #region Shoot

        bool CanShoot()
        {
            //if is reloading or the fire rate is less than the current fire time
            return !localData.reloading && _timeSinceLastShoot > 1 / localData.fireRate / 60;
        }

        public void ShootServer()
        {
            if (!NetworkManager.Instance.IsHost())
                return;
            
            if (localData.ammoInMagazine > 0)
            {
                if (CanShoot())
                {
                    InstantiateBulletServer();
                    OnGunShoot();
                }
            }
            else
            {
                Reload();
            }
        }

        public void ShootClient()
        {
            if (localData.ammoInMagazine > 0)
            {
                if (CanShoot())
                {
                    OnGunShoot();
                }
            }
            else
            {
                Reload();
            }
        }

        void InstantiateBulletServer()
        {

            float dispersion = UnityEngine.Random.Range(-localData.dispersion, localData.dispersion);

            MemoryStream bulletDataStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(bulletDataStream);
                    
            writer.Write(shooterOwner.GetPlayerId());
            writer.Write(weaponData.weaponName);
            writer.Write((int)BulletState.CREATION);
            writer.Write(dispersion);
            ReplicationHeader bulletDataHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, bulletDataStream.ToArray().Length);

            GameObject bullet =
                NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(bulletRef, bulletDataHeader,
                    bulletDataStream);
                    
            OnTriggerAttack onTriggerAttack = bullet.GetComponent<OnTriggerAttack>();
            onTriggerAttack.Damage = localData.damage;
            onTriggerAttack.SetOwner(_owner);

            transform.localRotation = transform.parent.rotation;
       
            
            Quaternion newRot = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y,
                transform.localEulerAngles.z + dispersion);
            transform.rotation = newRot;
            bullet.transform.rotation = transform.rotation;
            bullet.transform.position = firePoint.position;
            bullet.GetComponent<Rigidbody2D>().velocity = transform.right * localData.bulletSpeed;
            localData.ammoInMagazine--;
            localData.totalAmmo--;
            _timeSinceLastShoot = 0;
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, Int64 timeStamp, int lenght)
        {
            UInt64 shooterOwnerId = reader.ReadUInt64();
            if (shooterOwner.GetPlayerId() != shooterOwnerId) Debug.LogError("Wrong owner");
            string weaponName = reader.ReadString();
            if (!weaponName.Equals(weaponData.weaponName)) Debug.LogError("Wrong weapon");
            BulletState bulletState = (BulletState)reader.ReadInt32();

            if (bulletState == BulletState.CREATION)
            {
                OnTriggerAttack onTriggerAttack = objectSpawned.GetComponent<OnTriggerAttack>();
                onTriggerAttack.Damage = localData.damage;
                onTriggerAttack.SetOwner(_owner);

                transform.localRotation = transform.parent.rotation;
                float dispersion = reader.ReadSingle();

                Quaternion newRot = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y,
                    transform.localEulerAngles.z + dispersion);
                transform.rotation = newRot;



                Int64 currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                Rigidbody2D rigidbody2D = objectSpawned.GetComponent<Rigidbody2D>();
                rigidbody2D.velocity = transform.right * localData.bulletSpeed;
                
                objectSpawned.transform.rotation = transform.rotation;
                //objectSpawned.transform.position = firePoint.position + (Vector3)rigidbody2D.velocity * (currentTime * 0.001f);
                objectSpawned.transform.position = firePoint.position;
                localData.ammoInMagazine--;
                localData.totalAmmo--;
                _timeSinceLastShoot = 0;
            }
        }

        void OnGunShoot()
        {
            //VFX, sound
            CameraShaker.Instance.Shake(0.3f,1.0f);
            muzzleFlash.Play();
            int shotSound = Random.Range(0, localData.shotSound.Count);
            _audioSource.PlayOneShot(localData.shotSound[shotSound]);
        }

        #endregion

        public void FlipGun(bool flip)
        {
            //_spriteRenderer.flipY = flip;
            if (flip)
                transform.localScale = new Vector3(1, -1, 1);
            else
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
        }

        public void GiveMaxAmmo()
        {
            localData.totalAmmo = localData.maxAmmoCapacity;
            shooterOwner.OnReload?.Invoke(localData);
        }

        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }

        public GameObject GetOwner()
        {
            return _owner;
        }

        public void SetPlayerShooter(Player.Player player)
        {           
            shooterOwner = player;
        }
        

        public void ServerDespawn()
        {
            NetworkObject.isDeSpawned = true;
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            ReplicationHeader enemyDeSpawnHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.DESTROY, stream.ToArray().Length);
            NetworkManager.Instance.replicationManager.Server_DeSpawnNetworkObject(NetworkObject, enemyDeSpawnHeader, stream);
            DisposeGameObject();
        }
        public override void OnClientNetworkDespawn(NetworkObject destroyer, BinaryReader reader, long timeStamp, int lenght)
        {
            DisposeGameObject();
        }
        void DisposeGameObject()
        {
            Debug.Log("Weapon: Disposing");
            NetworkObject.isDeSpawned = true;
            if (TryGetComponent(out PoolObject pool))
            {
                gameObject.SetActive(false);
            }
            else
                Destroy(gameObject, 0.1f);
        }
    }
}