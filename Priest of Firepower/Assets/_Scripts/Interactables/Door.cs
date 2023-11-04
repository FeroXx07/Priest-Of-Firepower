using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Door : MonoBehaviour, IInteractable
{

    [SerializeField] string message;
    [SerializeField] float time;
    [SerializeField] InteractionPromptUI interactionPromptUI;
    [SerializeField] AudioClip audioClip;

    [SerializeField] List<Door> prerequisiteDoors;
    [SerializeField] List<GameObject> objectsToEnable;
    float timer;
    public string Prompt => message;

    public float InteractionTime => time;

    bool Open  = false;
    private void OnEnable()
    {
        interactionPromptUI.SetText(message);
        EnablePromptUI(false);

        EnableObjects(false);
    }
    public void EnablePromptUI(bool show)
    {
        interactionPromptUI.gameObject.SetActive(show);
    }

    public void Interact(Interactor interactor, bool keyPressed)
    {
        //check wether the door can interacted with or not
        if (!CanInteract()) return;

        if(keyPressed)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Open = true;
                Debug.Log(Prompt);
                //TODO check update points
                timer = InteractionTime;
                EnablePromptUI(false);
                Debug.Log("Open door");
                DisableDoor();
                EnableObjects(true);
            }
        }
        else{
            EnablePromptUI(true);
            timer = time;          
        }
    }
    private bool CanInteract()
    {
        bool canInteract = false;

        // if one of the prerequisite doors is open then enable door interaction
        if(prerequisiteDoors.Count > 0)
        {
            foreach (Door door in prerequisiteDoors)
            {
                if (door.IsOpen())
                    canInteract = true;
            }
        }
        else
        {
            canInteract = true; 
        }


        return canInteract;
    }
    private void EnableObjects(bool enable)
    {
        foreach (var obj in objectsToEnable)
        {
            obj.SetActive(enable);
        }
    }

    private void DisableDoor()
    {
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<ShadowCaster2D>().enabled = false;

        foreach(Transform child in gameObject.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    bool IsOpen() { return Open; }
}
