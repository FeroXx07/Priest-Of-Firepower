using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIInfo : MonoBehaviour
{
    [SerializeField] WeaponData weaponLocalData;
    [SerializeField] Image visibleSprite;

    public void SetWeapon(WeaponData data)
    {
        visibleSprite.sprite = data.sprite;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
