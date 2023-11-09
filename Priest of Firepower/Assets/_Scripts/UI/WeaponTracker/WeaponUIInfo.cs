using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIInfo : MonoBehaviour
{
    [SerializeField] WeaponData weaponLocalData;
    [SerializeField] Image visibleSprite;

    private void Awake()
    {
        visibleSprite.preserveAspect = true;
    }

    public void SetWeapon(WeaponData data)
    {
        visibleSprite.sprite = data.sprite;
    }

    // Start is called before the first frame update
    void Start()
    {

    }
}
