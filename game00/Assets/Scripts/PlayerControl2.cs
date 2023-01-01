using UnityEngine;
using System.Collections;
using System.Net.Sockets;

public class PlayerControl2 : MonoBehaviour
{
	[HideInInspector]
	public bool facingRight = true;         // For determining which way the player is currently facing.
	[HideInInspector]
	public bool jump = false;               // Condition for whether the player should jump.


	public float moveForce = 365f;          // Amount of force added to move the player left and right.
	public float maxSpeed = 5f;             // The fastest the player can travel in the x axis.
	public AudioClip[] jumpClips;           // Array of clips for when the player jumps.
	public float jumpForce = 1000f;         // Amount of force added when the player jumps.
	public AudioClip[] taunts;              // Array of clips for when the player taunts.
	public float tauntProbability = 50f;    // Chance of a taunt happening.
	public float tauntDelay = 1f;           // Delay for when the taunt should happen.


	private int tauntIndex;                 // The index of the taunts array indicating the most recent taunt.
	private Transform groundCheck;          // A position marking where to check if the player is grounded.
	private bool grounded = false;          // Whether or not the player is grounded.
	private Animator anim;                  // Reference to the player's animator component.

	Network nobj;
	Socket clientSocket;
	void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("groundCheck");
		anim = GetComponent<Animator>();
		
	}


	void Update()
	{
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		grounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));

		// If the jump button is pressed and the player is grounded then the player should jump.
		if (Input.GetButtonDown("Jump") && grounded)
			jump = true;
	}
	public void Flip()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;

		// Multiply the player's x local scale by -1.
		
		Vector3 theScale = GameObject.Find("hero2").gameObject.transform.localScale;
		theScale.x *= -1;
		
		GameObject.Find("hero2").gameObject.transform.localScale = theScale;
		// Debug.Log("fliped");

	/*	byte[] sendBuff = new byte[1024];
		sendBuff = System.Text.Encoding.Default.GetBytes("Flip ");
		clientSocket.Send(sendBuff);*/
	}


	public IEnumerator Taunt()
	{
		// Check the random chance of taunting.
		float tauntChance = Random.Range(0f, 100f);
		if (tauntChance > tauntProbability)
		{
			// Wait for tauntDelay number of seconds.
			yield return new WaitForSeconds(tauntDelay);

			// If there is no clip currently playing.
			if (!GetComponent<AudioSource>().isPlaying)
			{
				// Choose a random, but different taunt.
				tauntIndex = TauntRandom();

				// Play the new taunt.
				GetComponent<AudioSource>().clip = taunts[tauntIndex];
				GetComponent<AudioSource>().Play();
			}
		}
	}


	int TauntRandom()
	{
		// Choose a random index of the taunts array.
		int i = Random.Range(0, taunts.Length);

		// If it's the same as the previous taunt...
		if (i == tauntIndex)
			// ... try another random taunt.
			return TauntRandom();
		else
			// Otherwise return this index.
			return i;
	}
}
