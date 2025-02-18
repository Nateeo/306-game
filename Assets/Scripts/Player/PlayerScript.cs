﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using VIDE_Data;
using UnityEngine.SceneManagement;

public class PlayerScript : MonoBehaviour {
	public bool dialogFix = false;
	public float speed;
	public bool isGrounded;
	public Rigidbody rigidBody;
	Vector3 movement;
	public Vector3 jump;
	public Animator anim;
	public int forceConst;

	// tooltip for various notifications
	public Text toolTip;

	private CharacterController controller;

	private float verticalVelocity;
	private float gravity = 14.0f;
	private float jumpForce = 20.0f;
	public float walkSpeed = 4.0f;
	public float runSpeed = 50.0f;
	private float leftGround = 0f; // y height of ground before a jump
	public float maxHeight;


	// jump modifiers to reduce low-gravity feel
	public float fallMultiplier = 2.5f;
	public float lowJumpMultiplier = 2f;

	// for dialogue management
	public UIManager diagUI;

	// for journal management
	public GameObject journal;
	public bool journalEnabled;
	public InputField input;

	public PlayerInventory inventory;

	public static float NPC_RANGE = 2f;

	//fields for the puzzle planet two
	public bool icyMovement = true;
	bool icy = false;
	int lastDirection;
	public int icyspeed = 20;
	float drag;
	GameObject playerCharacter;
	PhysicMaterial physics;
	public CapsuleCollider collider;
	public SphereCollider colIcy;
	public bool icyPuzzle = false;

	private PersistAudioManager _audio;

	// Use this for initialization
	void Start () {

		// bind to the inventory manager
		_audio = GameObject.FindGameObjectWithTag ("Audio").GetComponent<PersistAudioManager>();
		InterfaceManager manager = GameObject.FindGameObjectWithTag("InterfaceManager").GetComponent<InterfaceManager>();
		manager.player = this;
		manager.reset();


		toolTip.enabled = false;
		journal.SetActive (false);
		journalEnabled = false;

		Screen.lockCursor = true;
		//jump = new Vector3 (0.0f, 0.2f, 0.0f);
		anim = GetComponent<Animator> ();
		controller = GetComponent<CharacterController> ();
		rigidBody = GetComponent<Rigidbody> ();
		inventory = GetComponent<PlayerInventory> ();
		drag = rigidBody.drag;

		colIcy = GetComponent<SphereCollider> ();
	}

	// force text selection to end so journal is preserved
	IEnumerator moveEnd()
	{
		yield return 0; // Skip the first frame in which this is called.
		input.MoveTextEnd(false); // Do this during the next frame.
	}

	void Update() {

		// To disable player updates in certain conditions
		if (shouldPlayerDisable ()) {
			return;
		}

		diagUI.interactToolTipDisabled ();

		if (SceneManager.GetActiveScene ().buildIndex == 0) {
			Destroy(gameObject);
		}

		// handle journal interface, disable the rest if active
		if (Input.GetKeyDown (KeyCode.Minus) && PlayerPrefs.GetInt("sceneIndex") != 2) {
			toggleJournal ();
			if (journalEnabled) {
				input.Select ();
				input.ActivateInputField ();
				int length = input.text.Length < 1 ? 0 : input.text.Length - 1;
				input.text = input.text.Substring (0, length);
				StartCoroutine (moveEnd ());
			}
		}
		// disable all other interaction if journal is enabled
		if (journalEnabled && inventory.enabled) {
			inventory.disable ();
		} else if (inventory.disabled) {
			inventory.enable ();
		}

		if (journalEnabled) {
			return;
		}

		if (Input.GetKeyDown (KeyCode.F)) {
			TryInteract ();
		}
		Collider[] hits = Physics.OverlapSphere (transform.position, NPC_RANGE);
		for (int i = 0; i < hits.Length; i++) {
			Collider rHit = hits [i];
			if (rHit.GetComponent<Collider> ().GetComponent<VIDE_Assign> () != null) {
				diagUI.interactToolTipEnabled();
				break;
			}
		}

		//if icyPuzzle is true must start puzzle
		if (icyPuzzle == true && icy == false) {

			//set everything to true and set the corrent mechanics for the puzzle
			icy = true;
			icyMovement = true;
			lastDirection = 0;

			rigidBody.isKinematic = true;

			//collider to stop player from falling through the ground
			colIcy.enabled = true;
			diagUI.playerCamera.dialogFix = true;

		}  else if (icyPuzzle == false && icy == true) {

			//set all mechanics and settings back to normal
			colIcy.enabled = false;
			collider.enabled = true;
			rigidBody.isKinematic = false;
			icy = false;

			//unlock camera
			diagUI.playerCamera.dialogFix = false;

		}
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (dialogFix) {
			rigidBody.freezeRotation = true;
			return;
		}


		// handle jumping velocity for more responsive jump
		if (rigidBody.velocity.y < 0) {
			rigidBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
		} else if (rigidBody.velocity.y > 0 && !Input.GetKey(KeyCode.Space)) {
			rigidBody.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
		}
		float h = Input.GetAxisRaw ("Horizontal");
		float v = Input.GetAxisRaw ("Vertical");
		Animating (h, v);
		Move (h, v);

		// clamp max height jump
		if (!isGrounded && rigidBody.position.y > leftGround + maxHeight && rigidBody.velocity.y > 0) {
			Debug.Log("MAX REACHED");
			GetComponent<Rigidbody>().velocity = Vector3.zero;
			GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
			rigidBody.AddForce(0, -3 * forceConst, 0, ForceMode.Impulse);
		}
	}

	// handle bounty notifications
	public void notifyBounty(bool enabled) {
		if (toolTip.enabled != enabled) {
			toolTip.enabled = enabled;
		}
		if (enabled) {
			toolTip.text = "Press 'F' to capture the fugitive!";
		} else {
			toolTip.text = "";
		}
	}

	void Move(float h, float v) {

		// move towards the camera position

		Vector3 movement = new Vector3(h, 0.0f, v);

		if (Input.GetKey (KeyCode.LeftShift)) {
			speed = runSpeed;
		} else {
			speed = walkSpeed;
		}

		//the mechanics for the puzzle are in this if statement
		if (icy == true) {
			//checks direction and restricts movement
			if (Input.GetKeyDown (KeyCode.A) && rigidBody.isKinematic == true && lastDirection != 1){
				dialogFix = true;
				collider.enabled = false;
				rigidBody.isKinematic = false;

				//direction to go in
				rigidBody.velocity = new Vector3 (-icyspeed, 0, 0);
				lastDirection = 1;

			}
			if (Input.GetKeyDown (KeyCode.D) && rigidBody.isKinematic == true && lastDirection != 2){
				//restrict movement til collision
				dialogFix = true;
				collider.enabled = false;
				rigidBody.isKinematic = false;

				//direction to go in
				rigidBody.velocity = new Vector3(icyspeed, 0, 0);
				lastDirection = 2;

			}
			if (Input.GetKeyDown (KeyCode.W) && rigidBody.isKinematic == true && lastDirection != 3){
				//restrict movement til collision
				dialogFix = true;
				collider.enabled = false;
				rigidBody.isKinematic = false;

				//direction to go in
				rigidBody.velocity = new Vector3(0, 0, icyspeed);
				lastDirection = 3;

			}
			if (Input.GetKeyDown (KeyCode.S) && rigidBody.isKinematic == true && lastDirection != 4){
				//restrict movement til collision
				dialogFix = true;
				collider.enabled = false;
				rigidBody.isKinematic = false;

				//direction to go in
				rigidBody.velocity = new Vector3(0, 0, -icyspeed);
				lastDirection = 4;

			}
		}  else if (icy == false) {

			//normal movement
			movement = Camera.main.transform.TransformDirection(movement);

			movement = movement.normalized * speed * Time.deltaTime;

			rigidBody.MovePosition (transform.position + movement);

		}

		if (movement != Vector3.zero) {
			rigidBody.MoveRotation (Quaternion.LookRotation (new Vector3(movement.x, 0.0f, movement.z)));
		}

		//jumping vector generation
		if (Input.GetKey (KeyCode.Space) && isGrounded) {
			rigidBody.AddForce (0, forceConst, 0, ForceMode.Impulse);
			leftGround = rigidBody.position.y;
			isGrounded = false;
		}

	}

	void OnCollisionStay()
	{
		isGrounded = true;
	}

	void Animating (float h, float v) {
		// did we press horizontal axis or vertical axis
		bool walking = false;
		bool running = false;

		if (speed == walkSpeed) {
			walking = h != 0f || v != 0f;
			running = false;
		} else if (speed == runSpeed) {
			running = h != 0f || v != 0f;
			walking = false;
		}

		anim.SetBool ("IsWalking", walking);
		anim.SetBool ("IsRunning", running);
	}

	public void StopAnimating() {
		anim.SetBool ("IsWalking", false);
		anim.SetBool ("IsRunning", false);
	}

	void toggleJournal() {
		if (!journalEnabled && VD.isActive) {
			return;
		}
		journalEnabled = !journalEnabled;
		if (journalEnabled) {
			diagUI.interfaceOpen ();
			journal.SetActive (true);
		} else {
			diagUI.interfaceClosed ();
			journal.SetActive (false);
		}
	}

	// to talk to NPCs
    void TryInteract()
    {
        if (VD.isActive)
        {
			diagUI.CallNext ();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, NPC_RANGE);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider rHit = hits[i];
            VIDE_Assign assigned;
            if (rHit.GetComponent<Collider>().GetComponent<VIDE_Assign>() != null)
            {
                assigned = rHit.GetComponent<Collider>().GetComponent<VIDE_Assign>();
                if (!VD.isActive)
                {
                    //... and use it to begin the conversation, look at the target
					// disable journal if open
					if (journalEnabled) {
						toggleJournal ();
					}
					_audio.playNPCSound ();
                    diagUI.Begin(rHit, assigned);
                }
                return;
            }
        }
    }

	bool shouldPlayerDisable() {
		int index = SceneManager.GetActiveScene ().buildIndex;
		if (index == 2 || index == 6 || index == 7 || index == 8) {
			return true;
		} else {
			return false;
		}
	}
}
