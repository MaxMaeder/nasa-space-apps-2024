using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planet_script : MonoBehaviour
{
    public main_script main;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

/*
    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        Debug.Log("Mouse is over GameObject.");
    }

    void OnMouseExit()
    {
        //The mouse is no longer hovering over the GameObject so output this message each frame
        Debug.Log("Mouse is no longer on GameObject.");
    }
*/
    void OnMouseDown() {
        //OnMouseDown is called when the user has pressed the mouse button while over the Collider.
        Debug.Log("Mouse is down on the element");
        main.moveCamera(transform.position);

    }
}
