using UnityEngine;
using System.Collections;

public class WarriorAnimationDemo : MonoBehaviour{
	
	public enum Warrior{
		Karate, 
		Ninja, 
		Brute, 
		Sorceress, 
		Knight, 
		Mage, 
		Archer, 
		TwoHanded, 
		Swordsman, 
		Spearman, 
		Hammer, 
		Crossbow
	}
	
	Animator animator;
	public GameObject target;
	public GameObject weaponModel;
	public GameObject secondaryWeaponModel;
	float rotationSpeed = 15f;
	public float gravity = -9.83f;
	public float runSpeed = 8f;
	public float walkSpeed = 3f;
	public float strafeSpeed = 3f;
	bool canMove = true;
	//jumping variables
	public float jumpSpeed = 8f;
	bool canJump = true;
	float fallingVelocity = -2;
	bool isFalling = false;
	// Used for continuing momentum while in air
	public float inAirSpeed = 8f;
	float maxVelocity = 2f;
	float minVelocity = -2f;
	//platform variables
	Vector3 newVelocity;
	Vector3 platformSpeed;
	bool platformAnimated;
	Quaternion platformFacing;
	Vector3 inputVec;
	Vector3 targetDirection;
	Vector3 targetDashDirection;
	bool isDashing = false;
	bool isGrounded = true;
	bool dead = false;
	bool isStrafing;
	bool isBlocking = false;
	bool isStunned = false;
	bool inBlock;
	bool blockGui;
	bool weaponSheathed;
	bool weaponSheathed2;
	bool isInAir;
	bool isStealth;
	public float stealthSpeed;
	bool isWall;
	bool ledgeGui;
	bool ledge;
	public float ledgeSpeed;
	int attack = 0;
	bool canChain;
	bool specialAttack2Bool;
	public Warrior warrior;
  	private IKHands ikhands;

	void Start(){
		animator = this.GetComponent<Animator>();
		if(warrior == Warrior.Archer)
			secondaryWeaponModel.gameObject.SetActive(false);
		//sets the weight on any additional layers to 0
		if(warrior == Warrior.Archer || warrior == Warrior.Crossbow){
			if(animator.layerCount >= 1){
				animator.SetLayerWeight(1, 0);
			}
		}
		//sets the weight on any additional layers to 0
    	if(warrior == Warrior.TwoHanded){
      	secondaryWeaponModel.GetComponent<Renderer>().enabled = false;
      	ikhands = this.gameObject.GetComponent<IKHands>();
    	}
	}

	#region Updates

	void FixedUpdate(){
		CheckForGrounded();
		//gravity
		GetComponent<Rigidbody>().AddForce(0, gravity, 0, ForceMode.Acceleration);
		if(!isGrounded){
			AirControl();
		}
		//check if character can move
		if(canMove){
			UpdateMovement();  
		}
		if(GetComponent<Rigidbody>().velocity.y < fallingVelocity){
			isFalling = true;
		} 
		else{
			isFalling = false;
		}
	}

	void LateUpdate(){
		//Get local velocity of charcter
		float velocityXel = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).x;
		float velocityZel = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z;
		//Update animator with movement values
		animator.SetFloat("Input X", velocityXel / runSpeed);
		animator.SetFloat("Input Z", velocityZel / runSpeed);
	}

	void Update(){
		//update character position and facing
		UpdateMovement();
		InAir();
		JumpingUpdate();
		if(Input.GetKeyDown(KeyCode.P)){
			Debug.Break();
		}
		//if character isn't dead, blocking, or stunned (or in a move)
		if(!dead || !blockGui || !isBlocking){
			//Get input from controls relative to camera
			Transform cameraTransform = Camera.main.transform;
			//Forward vector relative to the camera along the x-z plane   
			Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;
			//Right vector relative to the camera Always orthogonal to the forward vector
			Vector3 right = new Vector3(forward.z, 0, forward.x);
			//directional inputs
			float v = Input.GetAxisRaw("Vertical");
			float h = Input.GetAxisRaw("Horizontal");
			float dv = Input.GetAxisRaw("DashVertical");
			float dh = Input.GetAxisRaw("DashHorizontal");
			// Target direction relative to the camera
			targetDirection = h * right + v * forward;
			// Target dash direction relative to the camera
			targetDashDirection = dh * right + dv * -forward;
			inputVec = new Vector3(h, 0, v);
			if(!isBlocking){
				//if there is some input (account for controller deadzone)
				if(v > .1 || v < -.1 || h > .1 || h < -.1){
					//set that character is moving
					animator.SetBool("Moving", true);
					animator.SetBool("Running", true);
					//if targetting/strafing
					if(Input.GetKey(KeyCode.LeftShift) || Input.GetAxisRaw("TargetBlock") > .1){
	          		if(weaponSheathed != true){
			  				isStrafing = true;
			  				animator.SetBool("Running", false);
	          		}
					}
					else{
						isStrafing = false;
						animator.SetBool("Running", true);
					}
				}
				else{
					//character is not moving
					animator.SetBool("Moving", false);
					animator.SetBool("Running", false);
				}
			}
			if(!weaponSheathed){
				if(!blockGui){
					if(Input.GetAxis("TargetBlock") < -.1){
						if(!inBlock && !isInAir && attack == 0){
							animator.SetBool("Block", true);
							isBlocking = true;
							animator.SetBool("Running", false);
							animator.SetBool("Moving", false);
							newVelocity = new Vector3(0, 0, 0);
						}
					}
					if(Input.GetAxis("TargetBlock") == 0){
						inBlock = false;
						isBlocking = false;
						animator.SetBool("Block", false);
					}
				}
				//Character is not blocking
				if(!isBlocking){
					if(Input.GetButtonDown("Fire1") && attack <= 3){
						AttackChain();
					}
					if(!isInAir){
						if(Input.GetButtonDown("Jump") && canJump){
							StartCoroutine(_Jump(.8f));
						}
						if(attack == 0){
							if(Input.GetButtonDown("Fire0")){
								RangedAttack();
							}
							if(Input.GetButtonDown("Fire2")){
								MoveAttack();
							}
							if(Input.GetButtonDown("Fire3")){
								SpecialAttack();
							}
					}
						if(Input.GetButtonDown("LightHit")){
							StartCoroutine(_GetHit());
						}
					}
				}
				//Character is blocking, all buttons perform block hit react
				else{
					if(Input.GetButtonDown("Jump")){
						StartCoroutine(_BlockHitReact());
					}
					if(Input.GetButtonDown("Fire0")){
						StartCoroutine(_BlockHitReact());
					}
					if(Input.GetButtonDown("Fire1")){
						StartCoroutine(_BlockHitReact());
					}
					if(Input.GetButtonDown("Fire2")){
						StartCoroutine(_BlockHitReact());
					}
					if(Input.GetButtonDown("Fire3")){
						StartCoroutine(_BlockHitReact());
					}
					if(Input.GetButtonDown("LightHit")){
						StartCoroutine(_BlockBreak());
					}
					if(Input.GetButtonDown("Death")){
						StartCoroutine(_BlockBreak());
					}
				}
				if(Input.GetAxis("DashVertical") > .5 || Input.GetAxis("DashVertical") < -.5 || Input.GetAxis("DashHorizontal") > .5 || Input.GetAxis("DashHorizontal") < -.5){
					if(!isDashing && !isInAir){
						StartCoroutine(_DirectionalDash(Input.GetAxis("DashVertical"), Input.GetAxis("DashHorizontal")));
					}
				}
			}
		}
		//character is dead or blocking, stop character
		else{
			newVelocity = new Vector3(0, 0, 0);
			inputVec = new Vector3(0,0,0);
		}
		if(!dead){
			if(!isBlocking){
				if(Input.GetButtonDown("Death")){
					Dead();
				}
			}
		}
		else{
			if(Input.GetButtonDown("Death")){
				StartCoroutine(_Revive());
			}
		}
		if(Input.GetButtonDown("Special")){
			if(warrior == Warrior.Ninja){
				if(!isStealth){
					isStealth = true;
					animator.SetBool("Stealth", true);
				} 
				else{
					isStealth = false;
					animator.SetBool("Stealth", false);
				}
			}
		}
	}

	float UpdateMovement(){
		Vector3 motion = inputVec;
		if(isGrounded){
			if(!dead && !isBlocking && !blockGui && !isStunned){
				//reduce input for diagonal movement
				motion *= (Mathf.Abs(inputVec.x) == 1 && Mathf.Abs(inputVec.z) == 1)?.7f:1;
				//apply velocity based on platform speed to prevent sliding
				float platformVelocity = platformSpeed.magnitude * .4f;
				Vector3 platformAdjust = platformSpeed * platformVelocity;
				//set speed by walking / running, check if we are on a platform and if its animated, apply the platform's velocity
				if(!platformAnimated){
					if(!isStrafing){
						newVelocity = motion * runSpeed + platformAdjust;
						if(isStealth){
							newVelocity = motion * stealthSpeed + platformAdjust;
						}
					}
					else{
						if(isStealth){
							newVelocity = motion * stealthSpeed + platformAdjust;
						}
						newVelocity = motion * strafeSpeed + platformAdjust;

					}
				}
				//character is not on a platform
				else{
					//character is not strafing
					if(!isStrafing){
						newVelocity = motion * runSpeed + platformSpeed;
					}
					//character is strafing
					else{
						newVelocity = motion * strafeSpeed + platformSpeed;
					}
				}
				if(ledge){
					newVelocity = motion * ledgeSpeed + platformSpeed;
				}
				if(isStealth){
					newVelocity = motion * stealthSpeed + platformSpeed;
				}
			}
			//no input, character not moving
			else{
				newVelocity = new Vector3(0,0,0);
				inputVec = new Vector3(0,0,0);
			}
		}
		//if character is falling
		else{
			newVelocity = GetComponent<Rigidbody>().velocity;
		}
		// limit velocity to x and z, by maintaining current y velocity:
		newVelocity.y = GetComponent<Rigidbody>().velocity.y;
		GetComponent<Rigidbody>().velocity = newVelocity;
		if(!isStrafing  && !isWall){
			//if not strafing, face character along input direction
			if(!ledgeGui || !ledge){
				RotateTowardMovementDirection();
			}
		}
		//if targetting button is held look at the target
		if(isStrafing){
			Quaternion targetRotation;
			float rotationSpeed = 40f;
			targetRotation = Quaternion.LookRotation(target.transform.position - new Vector3(transform.position.x,0,transform.position.z));
			transform.eulerAngles = Vector3.up * Mathf.MoveTowardsAngle(transform.eulerAngles.y,targetRotation.eulerAngles.y,(rotationSpeed * Time.deltaTime) * rotationSpeed);
		}
		//return a movement value for the animator
		return inputVec.magnitude;
	}

	//face character along input direction
	void RotateTowardMovementDirection(){
		if(!dead){
			//if character isn't stunned or blocking
			if(!blockGui && !isBlocking && !isStunned){
				//if character is moving but not strafing
				if(inputVec != Vector3.zero && !isStrafing){
					//take the camera orientated input vector and apply it to our characters facing with smoothing
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * rotationSpeed);
				}
			}
		}
	}

	#endregion

	#region Jumping

	void CheckForGrounded(){
		float distanceToGround;
		float threshold = .45f;
		RaycastHit hit;
		Vector3 offset = new Vector3(0,.4f,0);
		if(Physics.Raycast((transform.position + offset), -Vector3.up, out hit, 100f)){
			distanceToGround = hit.distance;
			if(distanceToGround < threshold){
				isGrounded = true;
				isInAir = false;
				//moving platforms
				if(hit.transform.tag == "Platform"){
					//get platform script from collided platform
					Platform platformScript = hit.transform.GetComponent<Platform>();
					//check if the platform is moved with physics or if it is animated and get velocity from it
					if(platformScript.animated){
						platformSpeed = platformScript.velocity;
						platformAnimated = true;
					}
					if(!platformScript.animated){
						platformSpeed = hit.transform.GetComponent<Rigidbody>().velocity;
					}
					//get the platform rotation to pass into our character when they are on a platform
					platformFacing = hit.transform.rotation;
				}
				else{
					//if we are not on a platform, reset platform variables
					platformSpeed = new Vector3(0,0,0);
					platformFacing.eulerAngles = new Vector3(0, 0, 0);
				}
			}
			else{
				isGrounded = false;
				isInAir = true;
			}
		}
	}

	void JumpingUpdate(){
		//If the character is on the ground
		if(isGrounded){
			//set the animation back to idle
			animator.SetInteger("Jumping", 0);
			canJump = true;
		}
		else{
			//character is falling
			if(!ledge){
				if(isFalling){
					animator.SetInteger("Jumping", 2);
					canJump = false;
				}
			}
		}
	}

	IEnumerator _Jump(float jumpTime){
		//Take the current character velocity and add jump movement
		animator.SetTrigger("JumpTrigger");
		canJump = false;
		GetComponent<Rigidbody>().velocity += jumpSpeed * Vector3.up;
		animator.SetInteger("Jumping", 1);
		yield return new WaitForSeconds(jumpTime);
	}

	void AirControl(){
		float x = Input.GetAxisRaw("Horizontal");
		float z = Input.GetAxisRaw("Vertical");
		Vector3 inputVec = new Vector3(x, 0, z);
		Vector3 motion = inputVec;
		motion *= (Mathf.Abs(inputVec.x) == 1 && Mathf.Abs(inputVec.z) == 1)?.7f:1;
		//allow some control the air
		GetComponent<Rigidbody>().AddForce(motion * inAirSpeed, ForceMode.Acceleration);
		//limit the amount of velocity we can achieve
		float velocityX = 0;
		float velocityZ = 0;
		if(GetComponent<Rigidbody>().velocity.x > maxVelocity){
			velocityX = GetComponent<Rigidbody>().velocity.x - maxVelocity;
			if(velocityX < 0){
				velocityX = 0;
			}
			GetComponent<Rigidbody>().AddForce(new Vector3(-velocityX, 0, 0), ForceMode.Acceleration);
		}
		if(GetComponent<Rigidbody>().velocity.x < minVelocity){
			velocityX = GetComponent<Rigidbody>().velocity.x - minVelocity;
			if (velocityX > 0){
				velocityX = 0;
			}
			GetComponent<Rigidbody>().AddForce(new Vector3(-velocityX, 0, 0), ForceMode.Acceleration);
		}
		if(GetComponent<Rigidbody>().velocity.z > maxVelocity){
			velocityZ = GetComponent<Rigidbody>().velocity.z - maxVelocity;
			if(velocityZ < 0){
				velocityZ = 0;
			}
			GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, -velocityZ), ForceMode.Acceleration);
		}
		if(GetComponent<Rigidbody>().velocity.z < minVelocity){
			velocityZ = GetComponent<Rigidbody>().velocity.z - minVelocity;
			if (velocityZ > 0){
				velocityZ = 0;
			}
			GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, -velocityZ), ForceMode.Acceleration);
		}
	}

	void InAir(){
		if(isInAir){
			if(ledgeGui){
				animator.SetTrigger("Ledge-Catch");
				ledge = true;
			}
		}
	}

	#endregion

	#region Buttons

	void AttackChain(){
		if(isInAir){
			if(warrior == Warrior.Knight || warrior == Warrior.TwoHanded){
				StartCoroutine(_JumpAttack1());
			}
		}
		//if charater is not in air, do regular attack
		else if(attack == 0){
			StartCoroutine(_Attack1());
		}
		//if within chain time
		else if(canChain){
			if(warrior != Warrior.Archer){
				if(attack == 1){
					StartCoroutine(_Attack2());
				}
				else if(attack == 2){
					StartCoroutine(_Attack3());
				}
				else{
					//do nothing
				}
			}
		}
		else{
			//do nothing
		}
	}

	IEnumerator _Attack1(){
		StopAllCoroutines();
		canChain = false;
		animator.SetInteger("Attack", 1);
		attack = 1;
		if(warrior == Warrior.Knight){
			StartCoroutine(_ChainWindow(.1f, .8f));
			StartCoroutine(_LockMovementAndAttack(.7f));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_ChainWindow(.6f, 1f));
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else if(warrior == Warrior.Brute){
			StartCoroutine(_ChainWindow(.5f, .5f));
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Sorceress){
			StartCoroutine(_ChainWindow(.3f, 1.4f));
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Swordsman){
			StartCoroutine(_ChainWindow(.6f, 1.1f));
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else if(warrior == Warrior.Spearman){
			StartCoroutine(_ChainWindow(.2f, .8f));
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_ChainWindow(.6f, 1.2f));
			StartCoroutine(_LockMovementAndAttack(1.4f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(.7f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_ChainWindow(.4f, 1.2f));
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Archer){
			StartCoroutine(_LockMovementAndAttack(.7f));
		}
		else{
			StartCoroutine(_ChainWindow(.2f, .7f));
			StartCoroutine(_LockMovementAndAttack(.6f));
		}
		yield return null;
	}

	IEnumerator _Attack2(){
		StopAllCoroutines();
		canChain = false;
		animator.SetInteger("Attack", 2);
		attack = 2;
		if(warrior == Warrior.Knight){
			StartCoroutine(_ChainWindow(.4f, .9f));
			StartCoroutine(_LockMovementAndAttack(.6f));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_ChainWindow(.5f, .8f));
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Brute){
			StartCoroutine(_ChainWindow(.3f, .7f));
			StartCoroutine(_LockMovementAndAttack(1.4f));
		}
		else if(warrior == Warrior.Sorceress){
			StartCoroutine(_ChainWindow(.6f, 1.2f));
		}
		else if(warrior == Warrior.Karate){
			StartCoroutine(_ChainWindow(.3f, .6f));
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
		else if(warrior == Warrior.Swordsman){
			StartCoroutine(_ChainWindow(.6f, 1.1f));
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Spearman){
			StartCoroutine(_ChainWindow(.6f, 1.1f));
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_ChainWindow(.6f, 1.2f));
			StartCoroutine(_LockMovementAndAttack(1.4f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_ChainWindow(.4f, 1.2f));
			StartCoroutine(_LockMovementAndAttack(1.3f));
		}
		else if(warrior == Warrior.Ninja){
			StartCoroutine(_ChainWindow(.2f, .8f));
			StartCoroutine(_LockMovementAndAttack(.8f));
		}
		else{
			StartCoroutine(_ChainWindow(.1f, 2f));
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		yield return null;
	}

	IEnumerator _Attack3(){
		StopAllCoroutines();
		animator.SetInteger("Attack", 3);
		attack = 3;
		if(warrior == Warrior.Knight){
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
		if(warrior == Warrior.Swordsman){
			StartCoroutine(_LockMovementAndAttack(1.4f));
		} 
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_LockMovementAndAttack(1.5f));
		} 
		else if(warrior == Warrior.Karate){
			StartCoroutine(_LockMovementAndAttack(.8f));
		} 
		else if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(1.7f));
		} 
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_LockMovementAndAttack(1f));
		} 
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(1f));
		} 
		else{
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		canChain = false;
		yield return null;
	}

	void RangedAttack(){
		StopAllCoroutines();
		animator.SetTrigger("RangeAttack1Trigger");
		attack = 4;
		if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(2.4f));
		} 
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(1.7f));
		}
		else if(warrior == Warrior.Ninja){
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
		else if(warrior == Warrior.Archer){
			StartCoroutine(_SetLayerWeight(.6f));
			StartCoroutine(_ArcherArrow(.2f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_SetLayerWeight(.6f));
			StartCoroutine(_ArcherArrow(.2f));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_LockMovementAndAttack(2.6f));
			StartCoroutine(_SecondaryWeaponVisibility(.7f, true));
			StartCoroutine(_WeaponVisibility(.7f, false));
			StartCoroutine(_SecondaryWeaponVisibility(2f, false));
			StartCoroutine(_WeaponVisibility(2f, true));
		}
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_LockMovementAndAttack(1.7f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
	}

	void MoveAttack(){
		StopAllCoroutines();
		attack = 5;
		animator.SetTrigger("MoveAttack1Trigger");
		if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(1.4f));
		}
		else if(warrior == Warrior.Sorceress){
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(1.5f));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_LockMovementAndAttack(2.4f));
		}
		else if(warrior == Warrior.Knight){
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
	}

	void SpecialAttack(){
		StopAllCoroutines();
		attack = 6;
		animator.SetTrigger("SpecialAttack1Trigger");
		if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(2f));
		}
		else if(warrior == Warrior.Sorceress){
			StartCoroutine(_LockMovementAndAttack(1.5f));
		}
		else if(warrior == Warrior.Knight){
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(1.95f));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Swordsman){
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else if(warrior == Warrior.Spearman){
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
		else if(warrior == Warrior.Hammer){
			StartCoroutine(_LockMovementAndAttack(1.6f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(1.7f));
		}
	}

	IEnumerator _JumpAttack1(){
		yield return new WaitForFixedUpdate();
		GetComponent<Rigidbody>().velocity += jumpSpeed * -Vector3.up;
		animator.SetTrigger("JumpAttack1Trigger");
		StartCoroutine(_LockMovementAndAttack(1f));
	}

	#endregion

	#region Dashing

	IEnumerator _DirectionalDash(float x, float v){
		//check which way the dash is pressed relative to the character facing
		float angle = Vector3.Angle(targetDashDirection,-transform.forward);
		float sign = Mathf.Sign(Vector3.Dot(transform.up,Vector3.Cross(targetDashDirection,transform.forward)));
		// angle in [-179,180]
		float signed_angle = angle * sign;
		//angle in 0-360
		float angle360 = (signed_angle + 180) % 360;
		//deternime the animation to play based on the angle
		if(angle360 > 315 || angle360 < 45){
			StartCoroutine(_Dash(1));
		}
		if(angle360 > 45 && angle360 < 135){
			StartCoroutine(_Dash(2));
		}
		if(angle360 > 135 && angle360 < 225){
			StartCoroutine(_Dash(3));
		}
		if(angle360 > 225 && angle360 < 315){
			StartCoroutine(_Dash(4));
		}
		yield return null;
	}

	IEnumerator _Dash(int dashDirection){
		isDashing = true;
		animator.SetInteger("Dash", dashDirection);
		if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Karate){
			StartCoroutine(_LockMovementAndAttack(.6f));
		}
		else if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Knight){
			StartCoroutine(_LockMovementAndAttack(1.15f));
		}
		else if(warrior == Warrior.Archer){
			StartCoroutine(_LockMovementAndAttack(.5f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(.8f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(.55f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(.65f));
		}
		yield return new WaitForSeconds(.1f);
		animator.SetInteger("Dash", 0);
		isDashing = false;
	}

	IEnumerator _Dash2(int dashDirection){
		isDashing = true;
		animator.SetInteger("Dash2", dashDirection);
		yield return new WaitForEndOfFrame();
		animator.SetInteger("Dash2", dashDirection);
		yield return new WaitForEndOfFrame();
		StartCoroutine(_LockMovementAndAttack(.65f));
		animator.SetInteger("Dash2", 0);
		yield return new WaitForSeconds(.95f);
		isDashing = false;
	}

	#endregion

	#region Misc

	IEnumerator _SetInAir(float timeToStart, float lenthOfTime){
		yield return new WaitForSeconds(timeToStart);
		isInAir = true;
		yield return new WaitForSeconds(lenthOfTime);
		isInAir = false;
	}

	public IEnumerator _ChainWindow(float timeToWindow, float chainLength){
		yield return new WaitForSeconds(timeToWindow);
		canChain = true;
		animator.SetInteger("Attack", 0);
		yield return new WaitForSeconds(chainLength);
		canChain = false;
	}

	IEnumerator _LockMovementAndAttack(float pauseTime){
		isStunned = true;
		animator.applyRootMotion = true;
		inputVec = new Vector3(0, 0, 0);
		newVelocity = new Vector3(0, 0, 0);
		animator.SetFloat("Input X", 0);
		animator.SetFloat("Input Z", 0);
		animator.SetBool("Moving", false);
		yield return new WaitForSeconds(pauseTime);
		if(warrior == Warrior.Archer || warrior == Warrior.Crossbow){
			animator.SetInteger("Attack", 0);
		}
		canChain = false;
		isStunned = false;
		animator.applyRootMotion = false;
		//small pause to let blending finish
		yield return new WaitForSeconds(.2f);
		attack = 0;
	}

	void SheathWeapon(){
		animator.SetTrigger("WeaponSheathTrigger");
		if(warrior == Warrior.Archer){
			StartCoroutine(_WeaponVisibility(.4f, false));
		} 
		else if(warrior == Warrior.Swordsman){
			StartCoroutine(_WeaponVisibility(.4f, false));
			StartCoroutine(_SecondaryWeaponVisibility(.4f, false));
		} 
		else if(warrior == Warrior.Spearman){
			StartCoroutine(_WeaponVisibility(.26f, false));
		} 
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_WeaponVisibility(.5f, false));
			StartCoroutine(_BlendIKHandLeftRot(0, .3f, 0));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(1.1f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(1.4f));
			StartCoroutine(_WeaponVisibility(.5f, false));
		}
		weaponSheathed = true;
	}

	void UnSheathWeapon(){
		animator.SetTrigger("WeaponUnsheathTrigger");
		if(warrior == Warrior.Archer){
			StartCoroutine(_WeaponVisibility(.4f, true));
		}
		else if(warrior == Warrior.TwoHanded){
			StartCoroutine(_WeaponVisibility(.35f, true));
		}
		else if(warrior == Warrior.Swordsman){
			StartCoroutine(_WeaponVisibility(.35f, true));
			StartCoroutine(_SecondaryWeaponVisibility(.35f, true));
		}
		else if(warrior == Warrior.Spearman){
			StartCoroutine(_WeaponVisibility(.45f, true));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_WeaponVisibility(.6f, true));
			StartCoroutine(_LockMovementAndAttack(1f));
		}
		else{
			StartCoroutine(_WeaponVisibility(.6f, true));
			StartCoroutine(_LockMovementAndAttack(1.4f));
		}
		weaponSheathed = false;
	}

	IEnumerator _WeaponVisibility(float waitTime, bool weaponVisiblity){
		yield return new WaitForSeconds(waitTime);
		weaponModel.SetActive(weaponVisiblity);
	}

	IEnumerator _SecondaryWeaponVisibility(float waitTime, bool weaponVisiblity){
		yield return new WaitForSeconds(waitTime);
		secondaryWeaponModel.GetComponent<Renderer>().enabled = weaponVisiblity;
	}
	
	IEnumerator _ArcherArrow(float waitTime){
		if(warrior != Warrior.Crossbow){
			secondaryWeaponModel.gameObject.SetActive(true);
		}
		yield return new WaitForSeconds(waitTime);
		if(warrior != Warrior.Crossbow){
			secondaryWeaponModel.gameObject.SetActive(false);
		}
		yield return new WaitForSeconds(.2f);
		animator.SetInteger("Attack", 0);
		attack = 0;
	}

	IEnumerator _SetLayerWeight(float time){
		animator.SetLayerWeight(1, 1);
		yield return new WaitForSeconds(time);
		float a = 1;
		for(int i = 0; i < 20; i++){
			a -= .05f;
			animator.SetLayerWeight(1, a);
			yield return new WaitForEndOfFrame();
		}
		animator.SetLayerWeight(1, 0);
	}

	IEnumerator _BlockHitReact(){
		StartCoroutine(_LockMovementAndAttack(.5f));
		animator.SetTrigger("BlockHitReactTrigger");
		yield return null;
	}

	IEnumerator _BlockBreak(){
		StartCoroutine(_LockMovementAndAttack(1f));
		animator.SetTrigger("BlockBreakTrigger");
		yield return null;
	}

	IEnumerator _GetHit(){
		animator.SetTrigger("LightHitTrigger");
		if(warrior == Warrior.Ninja){
			StartCoroutine(_LockMovementAndAttack(2.4f));
		}
		else if(warrior == Warrior.Archer){
			StartCoroutine(_LockMovementAndAttack(2.7f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(2.5f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(2.8f));
		}
		yield return null;
	}

	void Dead(){
		animator.applyRootMotion = true;
		animator.SetTrigger("DeathTrigger");
		dead = true;
	}

	IEnumerator _Revive(){
		animator.SetTrigger("ReviveTrigger");
		if(warrior == Warrior.Brute){
			StartCoroutine(_LockMovementAndAttack(1.75f));
		}
		else if(warrior == Warrior.Mage){
			StartCoroutine(_LockMovementAndAttack(1.2f));
		}
		else if(warrior == Warrior.Sorceress){
			StartCoroutine(_LockMovementAndAttack(.7f));
		}
		else if(warrior == Warrior.Ninja){
			StartCoroutine(_LockMovementAndAttack(.9f));
		}
		else if(warrior == Warrior.Crossbow){
			StartCoroutine(_LockMovementAndAttack(1.3f));
		}
		else{
			StartCoroutine(_LockMovementAndAttack(1.1f));
			yield return null;
		}
		dead = false;
	}
  
	#endregion

  #region IKHandsBlending
  
   IEnumerator _BlendIKHandLeftPos(float wait, float timeToBlend, float amount){
	   yield return new WaitForSeconds(wait);
	   float currentLeftPos = ikhands.leftHandPositionWeight;
	   float diffOverTime = (Mathf.Abs(currentLeftPos) - amount) / timeToBlend;
	   float time = 0f;
	   if(currentLeftPos > amount){
	      while(time < timeToBlend){
	        	time += Time.deltaTime;
	        	ikhands.leftHandPositionWeight -= diffOverTime;
	        	yield return null;
	      }
	   }
	   if(currentLeftPos < amount){
	      while(time < timeToBlend){
	        	time += Time.deltaTime;
	        	ikhands.leftHandPositionWeight += diffOverTime;
	        	yield return null;
	      }
	   }
   }
  
	IEnumerator _BlendIKHandRightPos(float wait, float timeToBlend, float amount){
 		yield return new WaitForSeconds(wait);
 		float currentRightPos = ikhands.rightHandPositionWeight;
 		float diffOverTime = (Mathf.Abs(currentRightPos) - amount) / timeToBlend;
 		float time = 0f;
 		if(currentRightPos > amount){
   		while(time < timeToBlend){
     		time += Time.deltaTime;
     		ikhands.rightHandPositionWeight -= diffOverTime;
     		yield return null;
   	}		
 	}
	if(currentRightPos < amount){
		while(time < timeToBlend){
		  time += Time.deltaTime;
		  ikhands.rightHandPositionWeight += diffOverTime;
		  yield return null;
		}
	}
}
  
IEnumerator _BlendIKHandLeftRot(float wait, float timeToBlend, float amount){
	yield return new WaitForSeconds(wait);
	float currentLeftRot = ikhands.leftHandRotationWeight;
	float diffOverTime = (Mathf.Abs(currentLeftRot) - amount) / timeToBlend;
	float time = 0f;
	while(time < timeToBlend){
		if(currentLeftRot > amount){
			ikhands.leftHandRotationWeight -= diffOverTime * Time.deltaTime;
			time += Time.deltaTime;
			yield return null;
		}
		if(currentLeftRot < amount){
			ikhands.leftHandRotationWeight += diffOverTime * Time.deltaTime;
			time += Time.deltaTime;
			yield return null;
		}
	}
}
  
  IEnumerator _BlendIKHandRightRot(float wait, float timeToBlend, float amount){
   	yield return new WaitForSeconds(wait);
      float currentRightRot = ikhands.rightHandRotationWeight;
      float diffOverTime = (Mathf.Abs(currentRightRot) - amount) / timeToBlend;
      float time = 0f;
      while(time < timeToBlend){
      	if(currentRightRot > amount){
				ikhands.rightHandRotationWeight -= diffOverTime * Time.deltaTime;
				time += Time.deltaTime;
        		yield return null;
      	}
      	if(currentRightRot < amount){
	        ikhands.rightHandRotationWeight += diffOverTime * Time.deltaTime;
	        time += Time.deltaTime;
	        yield return null;
      	}
    	}
  	}
  
	IEnumerator _BlendIKHandRight(float time, float amount){
		ikhands.rightHandPositionWeight = amount;
		ikhands.rightHandRotationWeight = amount;
		yield return new WaitForSeconds(time);
	}	

	#endregion

	void OnGUI(){
		if(!dead){
			if(warrior == Warrior.Mage || warrior == Warrior.Ninja || warrior == Warrior.Knight || warrior == Warrior.Archer || warrior == Warrior.TwoHanded || warrior == Warrior.Swordsman || warrior == Warrior.Spearman || warrior == Warrior.Hammer || warrior == Warrior.Crossbow){
				if(!dead && weaponSheathed){
					if(GUI.Button(new Rect (30, 310, 100, 30), "Unsheath Weapon")){
						UnSheathWeapon();
					}
				}
			}
		}
		//if character isn't dead or weapon is sheathed
		if(!dead && !weaponSheathed){
			//if character is not blocking
			if(warrior == Warrior.Ninja){
				if(!isBlocking && !blockGui){
					ledgeGui = GUI.Toggle(new Rect(245, 60, 100, 30), ledgeGui, "Ledge Jump");
				}
				if(ledge){
					if(GUI.Button(new Rect(245, 90, 100, 30), "Ledge Drop")){
						animator.SetTrigger("Ledge-Drop");
						ledge = false;
						animator.SetBool("Ledge-Catch", false);
					}
					if(GUI.Button(new Rect(245, 20, 100, 30), "Ledge Climb")){
						animator.SetTrigger("Ledge-Climb-Trigger");
						ledge = false;
						animator.SetBool("Ledge-Catch", false);
					}
				}
			}
			if(!ledge){
				blockGui = GUI.Toggle(new Rect(25, 215, 100, 30), blockGui, "Block");
			}
			if(!ledge){
				if(!isBlocking){
					if(!blockGui){
						animator.SetBool("Block", false);
					} 
					else{
						animator.SetBool("Block", true);
						animator.SetFloat("Input X", 0);
						animator.SetFloat("Input Z", -0);
						newVelocity = new Vector3(0, 0, 0);
					}
				}
				if(blockGui){
					if(GUI.Button(new Rect(30, 240, 100, 30), "BlockHitReact")){
						StartCoroutine(_BlockHitReact());
					}
					if(GUI.Button(new Rect(30, 275, 100, 30), "BlockBreak")){
						StartCoroutine(_BlockBreak());
					}
				} 
				else if(!inBlock){
					if(!inBlock){
						if(GUI.Button(new Rect(30, 240, 100, 30), "Hit React")){
							StartCoroutine(_GetHit());
						}
					}
				}
			}
			if(!blockGui && !isBlocking && !ledge){
				if(GUI.Button(new Rect (25, 20, 100, 30), "Dash Forward")) 
					StartCoroutine(_Dash(1));
				if(GUI.Button(new Rect (135, 20, 100, 30), "Dash Right")) 
					StartCoroutine(_Dash(2));
				if(!ledge){
					if(GUI.Button(new Rect(245, 20, 100, 30), "Jump")){
						if(canJump == true && isGrounded){
							StartCoroutine(_Jump(.8f));
						}
					}
				}
				if(GUI.Button(new Rect(25, 50, 100, 30), "Dash Backward")){
					StartCoroutine(_Dash(3));
				}
				if(GUI.Button(new Rect(135, 50, 100, 30), "Dash Left")){
					StartCoroutine(_Dash(4));
				}
				//2nd Dash/Roll animations for Knight
				if(warrior == Warrior.Knight){
					if(GUI.Button(new Rect (355, 20, 100, 30), "Roll Forward"))
						StartCoroutine(_Dash2(1));
					if(GUI.Button(new Rect (355, 50, 100, 30), "Roll Backward")) 
						StartCoroutine(_Dash2(3));
					if(GUI.Button(new Rect (460, 20, 100, 30), "Roll Left")) 
						StartCoroutine(_Dash2(4));
					if(GUI.Button(new Rect (460, 50, 100, 30), "Roll Right")) 
						StartCoroutine(_Dash2(2));
				}
				//ATTACK CHAIN
				if(GUI.Button(new Rect(25, 85, 100, 30), "Attack Chain")){
					if(attack <= 3){
						AttackChain();
					}
				}
				if(warrior == Warrior.Crossbow){
					if(GUI.Button(new Rect (135, 85, 100, 30), "Reload")){
						StartCoroutine(_SetLayerWeight(1.2f));
						animator.SetTrigger("Reload1Trigger");
					}
				}
				if(warrior == Warrior.Ninja){
					if(GUI.Button(new Rect(135, 85, 100, 30), "Attack1_R")){
						if(attack == 0){
							animator.SetTrigger("Attack1RTrigger");
							attack = 4;
							StartCoroutine(_LockMovementAndAttack(.8f));
						}
					}
					if(GUI.Button(new Rect(245, 85, 100, 30), "Attack2_R")){
						if(attack == 0){
							attack = 4;
							animator.SetTrigger("Attack2RTrigger");
							StartCoroutine(_LockMovementAndAttack(.8f));
						}
					}
				}
				if(!blockGui && !isBlocking){
					if(GUI.Button(new Rect(25, 115, 100, 30), "RangeAttack1")){
						if(attack == 0){
							RangedAttack();
						}
					}
					if(warrior == Warrior.Ninja || warrior == Warrior.Crossbow){
						if(GUI.Button(new Rect(135, 115, 100, 30), "RangeAttack2")){
							if(attack == 0){
								attack = 4;
								animator.SetTrigger("RangeAttack2Trigger");
								if(warrior == Warrior.Crossbow){
									//if character is Crossbow blend in the upper body animation
									if(animator.GetBool("Moving") == true){
										StartCoroutine(_SetLayerWeight(.6f));
									}
									else{
										StartCoroutine(_LockMovementAndAttack(.7f));
									}
									animator.SetInteger("Attack", 0);
									attack = 0;
								}
								else{
									StartCoroutine(_LockMovementAndAttack(.6f));
								}
							}
						}
					}
					if(GUI.Button(new Rect(25, 145, 100, 30), "MoveAttack1")){
						if(attack == 0){
							MoveAttack();
						}
					}
					if(warrior == Warrior.Archer){
						if(GUI.Button(new Rect(135, 145, 100, 30), "MoveAttack2")){
							if(attack == 0){
								attack = 4;
								animator.SetTrigger("MoveAttack2Trigger");
								StartCoroutine(_ArcherArrow(.6f));
								StartCoroutine(_LockMovementAndAttack(1.1f));
							}
						}
					}
					if(GUI.Button(new Rect(25, 175, 100, 30), "SpecialAttack1")){
						if(attack == 0){
							SpecialAttack();
						}
					}
					if(warrior == Warrior.Ninja || warrior == Warrior.Sorceress){
						if(GUI.Button(new Rect(135, 175, 100, 30), "SpecialAttack2")){
							if(attack == 0){
								if(warrior == Warrior.Sorceress){
									if(!specialAttack2Bool){
										attack = 4;
										animator.SetTrigger("SpecialAttack2Trigger");
										animator.SetBool("SpecialAttack2Bool", true);
										specialAttack2Bool = true;
									} 
									else{
										attack = 4;
										specialAttack2Bool = false;
										animator.SetBool("SpecialAttack2Bool", false);
										animator.SetBool("SpecialAttack2Trigger", false);
									}
								} 
								else{
									attack = 4;
									animator.SetTrigger("SpecialAttack2Trigger");
									StartCoroutine(_LockMovementAndAttack(1.25f));
								}
							}
						}
					}
					if(GUI.Button(new Rect(30, 270, 100, 30), "Death")){
						animator.SetTrigger("DeathTrigger");
						dead = true;
					}
					if(warrior == Warrior.Mage || warrior == Warrior.Ninja || warrior == Warrior.Knight || warrior == Warrior.Archer || warrior == Warrior.TwoHanded || warrior == Warrior.Swordsman || warrior == Warrior.Spearman || warrior == Warrior.Hammer || warrior == Warrior.Crossbow){
						if(!dead && !weaponSheathed && !weaponSheathed2){
							if(GUI.Button(new Rect(30, 310, 100, 30), "Sheath Wpn")){
								SheathWeapon();
							}
						}
					}
					if(warrior == Warrior.Knight && !weaponSheathed){
						if(!dead && !weaponSheathed2){
							if(GUI.Button(new Rect(140, 310, 100, 30), "Sheath Wpn2")){
								StartCoroutine(_LockMovementAndAttack(1.4f));
								animator.SetTrigger("WeaponSheath2Trigger");
								StartCoroutine(_WeaponVisibility(.75f, false));
								weaponSheathed2 = true;
							}
						}
					}
					if(warrior == Warrior.Knight){
						if(!dead && weaponSheathed2){
							if(GUI.Button(new Rect(140, 310, 100, 30), "UnSheath Wpn2")){
								StartCoroutine(_LockMovementAndAttack(1.4f));
								animator.SetTrigger("WeaponUnsheath2Trigger");
								StartCoroutine(_WeaponVisibility(.5f, true));
								weaponSheathed2 = false;
								weaponSheathed = false;
							}
						}
					}
					if(warrior == Warrior.Ninja && !isStealth){
						if(!dead && !weaponSheathed){
							if(GUI.Button(new Rect(30, 350, 100, 30), "Stealth")){
								animator.SetBool("Stealth", true);
								isStealth = true;
							}
						}
					}
					if(warrior == Warrior.Ninja && isStealth && !isWall){
						if(!dead && !weaponSheathed){
							if(GUI.Button(new Rect(30, 350, 100, 30), "UnStealth")){
								animator.SetBool("Stealth", false);
								isStealth = false;
							}
						}
					}
					if(warrior == Warrior.Ninja && isStealth){
						if(!dead && !weaponSheathed){
							if(!isWall){
								if(GUI.Button(new Rect(140, 350, 100, 30), "Wall On")){
									animator.applyRootMotion = true;
									animator.SetBool("Stealth-Wall", true);
									isWall = true;
								}
							} 
							else{
								if(GUI.Button(new Rect(140, 350, 100, 30), "Wall Off")){
									animator.SetBool("Stealth-Wall", false);
									isWall = false;
									StartCoroutine(_LockMovementAndAttack(.7f));
								}
							}
						}
					}
				}
			}
		}
		if(dead){
			if(GUI.Button(new Rect (30, 270, 100, 30), "Revive")){
				StartCoroutine(_Revive());
			}
		}
	}
}