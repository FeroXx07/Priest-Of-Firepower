using System;
using System.IO;
using _Scripts.Networking;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using UnityEngine;

namespace _Scripts.Weapon
{
    public enum WeaponSwitcherStates
    {
        NONE = 0,
        NEW_WEAPON,
        CHANGE_WEAPON
    }
    public class WeaponSwitcher : NetworkBehaviour
    {
        [SerializeField] private WeaponSwitcherStates _state = WeaponSwitcherStates.NONE;
        public KeyCode[] keys;
        public WeaponSlot[] slots;
        int _selectedWeapon = 0;

        public float switchTime;
        float _lastSwtichTime;

        public Action<Transform> OnWeaponSwitch;
        public Action<Player.Player, GameObject, int> OnWeaponChange;

        [SerializeField] GameObject initialWeaponPrefab;
        [SerializeField] GameObject initialSecondaryWeaponPrefab;
        [SerializeField] private Player.Player player;
        [SerializeField] private AudioClip changeWpSound;
        private AudioSource _audioSource;
        [SerializeField] private UInt64 myUserId => NetworkManager.Instance.getId;

        [Serializable]
        public struct WeaponSlot
        {
            public Transform holder;
            public GameObject weapon;
            public bool empty;
            public int index;
        }

        public override void Awake()
        {
            base.Awake();
            InitNetworkVariablesList();
            BITTracker = new ChangeTracker(NetworkVariableList.Count);
            if (player == null) GetComponent<Player.Player>();
        }

        private void Start()
        {
            SetWeapons();
            _audioSource = GetComponent<AudioSource>();
        }

        public void InitializeWeapons()
        {
            SelectWeapon(_selectedWeapon);
            if (NetworkManager.Instance.IsHost()) ChangeWeaponServer(initialWeaponPrefab);
            if (NetworkManager.Instance.IsHost()) ChangeWeaponServer(initialSecondaryWeaponPrefab);
            SelectWeapon(_selectedWeapon);
        }

        private void SelectWeapon(int selectedWeapon)
        {
            for(int i = 0;i< slots.Length;i++)
            {
                slots[i].holder.gameObject.SetActive(i == selectedWeapon);
            }
            _lastSwtichTime = 0;
            OnWeaponSwitch?.Invoke(slots[selectedWeapon].holder);
            OnWeaponSelected();
        }

        private void SetWeapons()
        {
            //clean up the weapons slots
            for(int i = 0;i < slots.Length;i++)
            {
                slots[i].empty = true;
                if (slots[i].holder != null)
                    slots[i].holder.gameObject.SetActive(false);
                slots[i].weapon = null;
                slots[i].index = i;
            }
        }

        protected override void InitNetworkVariablesList()
        {
        }

        public override void Update()
        {
            base.Update();
            if (!player.isOwner()) return;  // Only the owner of the player will control it
            
            int previousWeapon = _selectedWeapon;
            
            for(int i = 0; i<keys.Length; i++)
            {
                if (Input.GetKey(keys[i]) && _lastSwtichTime >= switchTime)
                {
                    _selectedWeapon = i;
                }
            }

            if (_selectedWeapon != previousWeapon)
            {
                _state = WeaponSwitcherStates.CHANGE_WEAPON;
                
                SelectWeapon(_selectedWeapon);
                SendReplicationData(ReplicationAction.UPDATE);
            }

            _lastSwtichTime += Time.deltaTime;
        }

        public override bool ReadReplicationPacket(BinaryReader reader, long position = 0)
        {
            WeaponSwitcherStates receivedState = (WeaponSwitcherStates)reader.ReadInt32();

            if (receivedState == WeaponSwitcherStates.CHANGE_WEAPON)
            {
                _selectedWeapon = reader.ReadInt32();
                Debug.Log($"Weapon switcher: Reading change weapon {_selectedWeapon}");
                SelectWeapon(_selectedWeapon);
            }
            
            _state = receivedState;
            return true;
        }

        protected override ReplicationHeader WriteReplicationPacket(MemoryStream outputMemoryStream, ReplicationAction action)
        {
            BinaryWriter writer = new BinaryWriter(outputMemoryStream);
            writer.Write((int)_state);

            if (_state == WeaponSwitcherStates.CHANGE_WEAPON)
            {
                Debug.Log($"Weapon switcher: {_state} Sending change weapon {_selectedWeapon}");
                writer.Write(_selectedWeapon);
            }
            
            ReplicationHeader replicationHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, action, outputMemoryStream.ToArray().Length);
            return replicationHeader;
        }

        public void ChangeWeaponServer(GameObject newWeaponPrefab)
        {
            if (newWeaponPrefab == null || !NetworkManager.Instance.IsHost()) return;

            _state = WeaponSwitcherStates.NEW_WEAPON;
            
            WeaponSlot emptySlot = new WeaponSlot{empty = true, holder = null, weapon = null,index = -1 };

            //check if one of the slots is empty, if so add the new weapon there
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].empty)
                {
                    emptySlot = slots[i];
                    emptySlot.empty = false;
                    emptySlot.index = i;
                    break;
                }
            }

            //if the emptySlot is still empty means that other slots are full
            //then change the weapon to the currently selected weapon
            if(emptySlot.empty)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i == _selectedWeapon)
                    {
                        emptySlot = slots[i];
                        break;
                    }
                }
            }

            //remove previous weapon
            foreach (Transform w in emptySlot.holder)
            {
                Weapon wp = w.GetComponent<Weapon>();
                if(wp != null)
                {
                    wp.ServerDespawn();
                }
            }

            emptySlot.weapon = newWeaponPrefab;
            slots[emptySlot.index] = emptySlot;
            _selectedWeapon = emptySlot.index;
            
            Player.Player user = gameObject.GetComponent<Player.Player>();
            
            MemoryStream changeWeaponMemoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(changeWeaponMemoryStream);
            writer.Write(user.GetPlayerId());
            writer.Write(user.GetName());
            writer.Write((int)_state);
            ReplicationHeader changeWeaponHeader = new ReplicationHeader(NetworkObject.GetNetworkId(), this.GetType().FullName, ReplicationAction.CREATE, changeWeaponMemoryStream.ToArray().Length);
            GameObject weapon = NetworkManager.Instance.replicationManager.Server_InstantiateNetworkObject(newWeaponPrefab,
                changeWeaponHeader, changeWeaponMemoryStream);

            weapon.transform.parent = emptySlot.holder.transform;

            weapon.transform.position = Vector3.zero;
            weapon.transform.rotation = Quaternion.identity;

            Weapon weaponComponent =  weapon.GetComponent<Weapon>();
            weaponComponent.SetData();
            weaponComponent.SetOwner(gameObject);

            SelectWeapon(_selectedWeapon);
            SendReplicationData(ReplicationAction.UPDATE);
            OnWeaponChange?.Invoke(user, weapon, emptySlot.index);           
        }

        private void ChangeWeaponClient(GameObject objectSpawned)
        {
            if (objectSpawned == null ||!NetworkManager.Instance.IsClient()) return;
            WeaponSlot emptySlot = new WeaponSlot{empty = true, holder = null, weapon = null,index = -1 };

            //check if one of the slots is empty, if so add the new weapon there
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].empty)
                {
                    emptySlot = slots[i];
                    emptySlot.empty = false;
                    emptySlot.index = i;
                    break;
                }
            }
            //if the emptySlot is still empty means that other slots are full
            //then change the weapon to the currently selected weapon
            if(emptySlot.empty)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i == _selectedWeapon)
                    {
                        emptySlot = slots[i];
                        break;
                    }
                }
            }

            //remove previous weapon
            foreach (Transform w in emptySlot.holder)
            {
                Destroy(w.gameObject);
            }

            emptySlot.weapon = objectSpawned;
            slots[emptySlot.index] = emptySlot;
            _selectedWeapon = emptySlot.index;
            
            objectSpawned.transform.parent = emptySlot.holder.transform;

            objectSpawned.transform.position = Vector3.zero;
            objectSpawned.transform.rotation = Quaternion.identity;

            Player.Player user = gameObject.GetComponent<Player.Player>();
            Weapon weaponComponent = objectSpawned.GetComponent<Weapon>();
            weaponComponent.SetData();
            weaponComponent.SetOwner(gameObject);
            SelectWeapon(_selectedWeapon);
            SendReplicationData(ReplicationAction.UPDATE);
            OnWeaponChange?.Invoke(user, objectSpawned, emptySlot.index);
        }

        public override void CallBackSpawnObjectOther(NetworkObject objectSpawned, BinaryReader reader, Int64 timeStamp, int lenght)
        {
            // This is the call back that the client receives when in the server the this same object has spawned some other object.
            // Called through the replication manager when state is CREATE.
            UInt64 playerId = reader.ReadUInt64();
            string playerName = reader.ReadString();
            _state = (WeaponSwitcherStates)reader.ReadInt32();

            if (_state == WeaponSwitcherStates.NEW_WEAPON)
            {
                ChangeWeaponClient(objectSpawned.gameObject);
            }
        }

        void OnWeaponSelected()
        {
            //TODO add sound 
            _audioSource.clip = changeWpSound;
            _audioSource.Play();
        }
    }
}