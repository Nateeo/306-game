﻿using UnityEngine;
using System.Collections;

/**
 * This class triggers the UI objects when the player enters the collison zone 
 */
public class KeypadTrigger : MonoBehaviour {
	public GameObject canvas;
	public GameObject canvas2; //Holds the F to interact text
	public GameObject player;

	private bool overlayDisplayed = false;

	void Start () {
		//Disable the canvas to start with
		canvas.SetActive (false);
		canvas2.SetActive (false);

		//Disable mouse
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}


	public void closeOverlay() {
		if (overlayDisplayed) {
			overlayDisplayed = false;
			canvas.SetActive (false);

			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			player.GetComponent<MouseLook> ().enabled = true;
			player.GetComponent<FPSInputController> ().enabled = true;
		}
	}

	//The player has entered the keypad zone and pressed F
	void OnTriggerStay(Collider collide) {
		canvas2.SetActive (true);

		if (Input.GetKeyDown (KeyCode.F)) {
			overlayDisplayed = true;

			canvas.SetActive (true);

			//Unlock cursor and show it
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;

			//Lock camera
			player.GetComponent<MouseLook>().enabled = false;

			//Lock character movement
			player.GetComponent<FPSInputController>().enabled = false;
		}

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            closeOverlay();
        }
    }

	void OnTriggerExit(Collider collide) {
		canvas2.SetActive (false);
	}
}
