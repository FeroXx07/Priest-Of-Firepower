using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] string message;
    public string prompt => message;

    public void Interact(Interactor interactor)
    {
        Debug.Log(prompt);
    }
}
