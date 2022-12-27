using CommonUtils;
using CommonUtils.Extensions;
using CommonUtils.Inspector.HelpBox;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

public class FootstepsPlayer : EnhancedMonoBehaviour {
	#region Inspector fields
	[HelpBox("Typically, the Footstep Audio Source will be a game object which is a child of the PlayerCapsule, in the local position (0,0,0).")]
	[SerializeField] private AudioSource footstepAudioSource;

	[HelpBox("Drag the child object PlayerCameraRoot into this field. If left unassigned, this script will try to find it on Start.")]
	[SerializeField] private Transform playerCameraRoot;

	[SerializeField] private float m_StepInterval = 5f;
	[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten = 0.7f;

	[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
	[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
	[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

	[Header("Head Bob")]
	[SerializeField] private bool m_UseHeadBob;
	[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
	[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
	#endregion

	#region Properties
	[ShowInInspector] private float m_StepCycle { get; set; }
	[ShowInInspector] private float m_NextStep { get; set; }
	[ShowInInspector] private bool m_IsWalking => !Sprint;
	#endregion

	#region Fields
	private CharacterController characterController;
	#endregion


	private bool wasGrounded;

	private void Awake() {
		characterController = GetComponent<CharacterController>();
		if (!characterController) {
			this.LogError("This component must be attached to the FPS PlayerCapsule prefab of the Starter Assets package!");
			enabled = false;
		}

		if (!footstepAudioSource) {
			footstepAudioSource = GetComponentInChildren<AudioSource>();
			if (!footstepAudioSource) {
				footstepAudioSource = GetComponent<AudioSource>();
				if (!footstepAudioSource) {
					footstepAudioSource = gameObject.AddComponent<AudioSource>();
				}
			}
		}

		if (!playerCameraRoot) {
			playerCameraRoot = transform.FindChildWithTag("CinemachineTarget");
			if (!playerCameraRoot) {
				this.LogError("Player Camera Root couldn't be found!");
			}
		}
	}

	private void Start() {
		m_HeadBob.Setup(playerCameraRoot, m_StepInterval);
		m_StepCycle = 0f;
		m_NextStep = m_StepCycle/2f;
	}

	private void Update() {
		if (!wasGrounded && characterController.isGrounded) {
			StartCoroutine(m_JumpBob.DoBobCycle());
			PlayLandingSound();
		}

		if (wasGrounded && !characterController.isGrounded && /*_input.jump*/ Jump) {
			PlayJumpSound();
		}

		wasGrounded = characterController.isGrounded;
		Jump = false;
	}

	private void LateUpdate() {
		var speed = characterController.velocity.magnitude;
		ProgressStepCycle(speed);
		UpdateCameraPosition(speed);
	}

	private void PlayJumpSound()
	{
		footstepAudioSource.clip = m_JumpSound;
		footstepAudioSource.Play();
	}

	private void PlayLandingSound()
	{
		footstepAudioSource.clip = m_LandSound;
		footstepAudioSource.Play();
		m_NextStep = m_StepCycle + .5f;
	}

	private void ProgressStepCycle(float speed)
	{
		if (characterController.velocity.sqrMagnitude > 0 && (Move.x != 0 || Move.y != 0)) {
			m_StepCycle +=
				(characterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
				Time.deltaTime; //Time.fixedDeltaTime;
		}

		if (!(m_StepCycle > m_NextStep))
		{
			return;
		}

		m_NextStep = m_StepCycle + m_StepInterval;

		PlayFootStepAudio();
	}


	private void PlayFootStepAudio()
	{
		if (!characterController.isGrounded)
		{
			return;
		}
		// pick & play a random footstep sound from the array,
		// excluding sound at index 0
		int n = Random.Range(1, m_FootstepSounds.Length);
		footstepAudioSource.clip = m_FootstepSounds[n];
		footstepAudioSource.PlayOneShot(footstepAudioSource.clip);
		// move picked sound to index 0 so it's not picked next time
		m_FootstepSounds[n] = m_FootstepSounds[0];
		m_FootstepSounds[0] = footstepAudioSource.clip;
	}

	private void UpdateCameraPosition(float speed)
	{
		Vector3 newCameraPosition;
		if (!m_UseHeadBob)
		{
			return;
		}
		if (characterController.velocity.magnitude > 0 && characterController.isGrounded)
		{
			playerCameraRoot.localPosition =
				m_HeadBob.DoHeadBob(characterController.velocity.magnitude +
									(speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
			newCameraPosition = playerCameraRoot.localPosition;
			newCameraPosition.y = playerCameraRoot.localPosition.y - m_JumpBob.Offset();
		}
		else
		{
			newCameraPosition = playerCameraRoot.localPosition;
			newCameraPosition.y = m_HeadBob.OriginalCameraPosition.y - m_JumpBob.Offset();
		}
		playerCameraRoot.localPosition = newCameraPosition;
	}

	#region Player Input listeners
	[ShowInInspector] public Vector2 Move { get; private set; }
	[ShowInInspector] public bool Jump { get; private set; }
	[ShowInInspector] public bool Sprint { get; private set; }


#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	public void OnMove(InputValue value) => Move = value.Get<Vector2>();

	public void OnJump(InputValue value) => Jump = value.isPressed;

	public void OnSprint(InputValue value) => Sprint = value.isPressed;
#endif
	#endregion
}
