using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;							// Amount of force added when the player jumps.
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;	// How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;							// Whether or not a player can steer while jumping;
	[SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_CeilingCheck;							// A position marking where to check for ceilings
	[SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching

	const float k_GroundedRadius = .1f; // Radius of the overlap circle to determine if grounded
	public bool m_Grounded;            // Whether or not the player is grounded.
	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
	public Rigidbody2D m_Rigidbody2D;
	public bool m_FacingRight = true;  // For determining which way the player is currently facing.
	public float jumpTime = 0.3f;
	public int jumpStack =2 ;
	float jumpTimeCounter;
	float moveLoR;
	public Vector3 m_Velocity = Vector3.zero;
	public Vector3 playerVel;

	bool trigger = true;

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

	private void FixedUpdate()
	{
		playerVel = m_Rigidbody2D.velocity;
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
	}


	public void Move(float move, bool crouch, bool jump, int n_jump, bool jumpKeyHeld,int state, bool wallSlide)
	{
		if (move < 0) moveLoR = -1;
		else if (move > 0) moveLoR = 1;
		else moveLoR = 0;

		/* jump height test
		if (m_Rigidbody2D.velocity.y < 0 && trigger)
        {
			Debug.Log(m_Rigidbody2D.position.y);
			trigger = false;
        }
		if (m_Grounded)
        {
			trigger = true;
        }
		*/

		// If crouching, check to see if the character can stand up
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//airControl is off, but custom air movement
		//player won't be able to turn around (flip) after jump unless: aerial or another jump
		if (!m_AirControl)
        {
			if (m_Rigidbody2D.velocity.x < 8 && m_Rigidbody2D.velocity.x > -8) m_Rigidbody2D.AddForce(new Vector2(moveLoR * 30f, 0f));
			else if (m_Rigidbody2D.velocity.x < -8 && moveLoR > 0) m_Rigidbody2D.AddForce(new Vector2(moveLoR * 60f, 0f));
			else if (m_Rigidbody2D.velocity.x > 8 && moveLoR < 0) m_Rigidbody2D.AddForce(new Vector2(moveLoR * 60f, 0f));
			else
            {
				//Vector2 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
				//m_Rigidbody2D.AddForce(targetVelocity);
				//m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);
			}

		}

		//only control the player if grounded or airControl is turned on
		if (m_Grounded || m_AirControl)
		{

			// If crouching
			if (crouch && state != 4)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier

				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Move the character by finding the target velocity
			Vector2 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// And then smoothing it out and applying it to the character
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			// If the input is moving the player right and the player is facing left...
			if (move > 0 && !m_FacingRight && (state != 3 || state != 4 || state != 5))
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight && (state != 3 || state != 4 || state != 5))
			{
				// ... flip the player.
				Flip();
			}
			//else if ((move > 0 && !m_FacingRight) || (move < 0 && m_FacingRight)) && state == 3 && 
		}
		// If the player should jump...
		if (jumpKeyHeld && n_jump>0 && !wallSlide)
		{
			// Add a vertical force to the player.


			if ((m_Grounded && jumpKeyHeld))
            {
				jumpTimeCounter = jumpTime;
            }

			if(jumpTimeCounter > 0 && jumpKeyHeld)
            {
				Vector2 v = m_Rigidbody2D.velocity;
				v.y = 0;
				m_Rigidbody2D.velocity = v;

				Vector2 vj = m_Rigidbody2D.velocity;
				vj.y = m_JumpForce;
				m_Rigidbody2D.velocity = vj;
				jumpTimeCounter -= Time.deltaTime;
            }


			if (move > 0 && !m_FacingRight && n_jump < 2)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && m_FacingRight && n_jump < 2)
			{
				// ... flip the player.
				Flip();
			}
		}
		else if (!jumpKeyHeld)
        {
			jumpTimeCounter = jumpTime;
        }
		
		else if (jump && wallSlide)
        {
			m_Rigidbody2D.velocity = new Vector2(20 * -moveLoR, 15);
		}


	}


	public void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
