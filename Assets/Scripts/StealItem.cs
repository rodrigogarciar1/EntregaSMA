using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StealItem : MonoBehaviour
{   
    public GameObject ItemOnPlayer;
    public GameObject Text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private MeshRenderer renderer;
    private void Awake()
    {
        renderer = GetComponent<MeshRenderer>();
    }
    void Start()
    {
        ItemOnPlayer.SetActive(false);
        Text.SetActive(false);
    }
    private void OnTriggerStay(Collider obj)
    {
        if (obj.CompareTag("Player") && renderer.enabled) {
            Text.SetActive(true);
            PlayerInventory playerInventory = obj.GetComponent<PlayerInventory>();
            if (Input.GetKeyDown(KeyCode.F) && (playerInventory != null))

            {   
                
                renderer.enabled = false;
                playerInventory.RelicsCollected();
                Text.SetActive(false);

                // ItemOnPlayer.SetActive(true);

            }}
    
    }
    private void OnTriggerExit(Collider obj)
    {
        Text.SetActive(false);
    }
}
