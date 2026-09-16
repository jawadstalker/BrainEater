using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{

    public CinemachinePositionComposer CPC;

    Rigidbody2D Rb;

    Animator Anim;

    [Header("Movement variables")]
    public float CurrentSpeed;
    public float RunSpeed = 3.0f;
    public float MoveSpeed = 2.0f;
    public float CrouchMoveSpeed = 1.0f;
    public float JumpForce = 5.0f;
    public float PipeJumpForce = 8.0f;
    public float JumpCutMulitplier = 0.5f;
    public float NormalGravity;
    public float FallGravity;
    public float JumpGravity;

    [Min(0.0f)] public float FallSpeedMax = 5.0f;

    [Header("Dash variables")]
    public float DashSpeed = 12.0f;
    public float DashDuration = 0.15f;
    public float DashCooldown = 0.5f;

    [SerializeField]
    private bool IsDashing;
    private bool CanDash = true;

    public bool AllowMove = true;

    [Header("Debug movement variables")]
    public Vector2 MoveInput = Vector2.zero;
    public bool FacingRight = true;
    [SerializeField]
    private bool JumpPressed;
    [SerializeField]
    private bool JumpReleased;
    [SerializeField]
    private bool IsLanding;
    [SerializeField]
    private bool IsFalling;

    [Header("Ground checking")]
    public GroundCheck Check;
    public LayerMask GroundLayerMask;
    public LayerMask PipeLayerMask;
    public bool IsGrounded = true;

    [SerializeField]
    private int Money = 0;

    [SerializeField] private float Acceleration = 20f;
    [SerializeField] private float Deceleration = 30f;

    public LayerMask EnemeyLayer;

    private float TargetScale = 1.0f;

    private float ScaleMultiplier = 1.0f;

    public float ScaleSpeed = 10.0f;

    public float DecreaseScaleSpeed = 25.0f;

    public bool Invulnerable = false;

    private bool Respawning = false;

    private bool Dying = false;

    private bool Crouched = false;

    private bool Running = false;

    private bool Shooting = false;

    public Transform ShootSpot;

    public GameObject BulletPrefab;

    [SerializeField]
    private Vector2 GroundNormal;

    [Min(0.0f)] public float LookUpOffset = 2.0f;

    public float LookUpTime = 0.5f;

    private Coroutine LookUpCoroutine = null;

    [Min(0.0f)] public float LookDownOffset = 2.0f;

    public float LookDownTime = 0.5f;

    private Coroutine LookDownCoroutine = null;

    private bool LoadingLevel = false;

    void Start()
    {
        Rb = GetComponent<Rigidbody2D>();

        Rb.gravityScale = NormalGravity;

        Anim = GetComponent<Animator>();

        Check = GetComponent<GroundCheck>();
    }

    #region Updates
    void FixedUpdate()
    {
        if(Respawning)
        {
            return;
        }

        // if (CurrentPlatform != null && IsGrounded)
        // {
        //     Rb.position += CurrentPlatform.MovementDelta;
        // }

        CheckGrounded();

        if(IsDashing)
        {
            return;
        }

        if(!LoadingLevel)
        {
            ApplyVariableGravity();
        }

        if(!IsLanding && AllowMove)
        {
            if(!Crouched && !Shooting)
            {
                HandleMovement();
                HandleJump();
            }
        }

        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Mathf.Max(Rb.linearVelocity.y, -FallSpeedMax));
    }
    void Update()
    {
        if(Respawning)
        {
            HandleAnimations();
            return;
        }

        Flip();
        HandleAnimations();
        HandleResize();
    }

    private void HandleResize()
    {
        Vector3 startScale = transform.localScale;

        float speed = ScaleSpeed;

        if(ScaleMultiplier == 0.5f)
        {
            speed = DecreaseScaleSpeed;
        }

        float scale = Mathf.MoveTowards(startScale.y, TargetScale * ScaleMultiplier, speed * Time.deltaTime);

        float delta = scale - startScale.y;

        startScale.y = scale;

        //since scales both sides we half and add
        float deltaHalf = delta * 0.5f;

        transform.position += Vector3.up * deltaHalf;
        transform.localScale = startScale;
    }

    private void HandleMovement()
    {
        float speed = 0.0f;

        if(Running)
        {
            speed = RunSpeed;
        }
        else
        {
            speed = MoveSpeed;
        }

        if (IsGrounded)
        {
            Vector2 groundTangent = new Vector2(
                GroundNormal.y,
                -GroundNormal.x
            ).normalized;

            // Current speed along the slope
            float currentSlopeSpeed = Vector2.Dot(
                Rb.linearVelocity,
                groundTangent
            );

            float slopeCos = Vector2.Dot(GroundNormal.normalized, Vector2.up);

            float targetSlopeSpeed = speed * MoveInput.x;

            float rate = Mathf.Abs(targetSlopeSpeed) > Mathf.Abs(currentSlopeSpeed)
                ? Acceleration
                : Deceleration;

            float newSlopeSpeed = Mathf.MoveTowards(
                currentSlopeSpeed,
                targetSlopeSpeed,
                rate * Time.fixedDeltaTime
            );


            bool sloped = slopeCos < 0.98;
            bool moving = Mathf.Abs(MoveInput.x) > 0.1f;
            //we aren't on flat ground, don't apply acceleration
            if(sloped)
            {
                newSlopeSpeed = targetSlopeSpeed;
            }

            if(!sloped || moving)
            {
                Rb.linearVelocity = groundTangent * newSlopeSpeed;
            }
        }
        else
        {
            float targetSpeed = speed * MoveInput.x;

            float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(Rb.linearVelocity.x)
                ? Acceleration
                : Deceleration;

            float newSpeed = Mathf.MoveTowards(
                Rb.linearVelocity.x,
                targetSpeed,
                rate * Time.fixedDeltaTime
            );

            Rb.linearVelocity = new Vector2(
                newSpeed,
                Rb.linearVelocity.y
            );
        }
    }

    private void HandleJump()
    {
        if(IsFalling || Crouched)
        {
            JumpReleased = false;
            JumpPressed = false;
            return;
        }

        if(JumpPressed && IsGrounded)
        {
            float force = Check.Hit.collider && (1 << Check.Hit.collider.gameObject.layer) == PipeLayerMask ? PipeJumpForce : JumpForce;
                
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, force);
            JumpPressed = false;
            JumpReleased = false;

            MovingPlatform isOnPlatform = gameObject.GetComponentInParent<MovingPlatform>();
            if(isOnPlatform != null)
            {
                transform.SetParent(null);
            }

        }
        if(JumpReleased)
        {
            if (Rb.linearVelocity.y > 0.0f)
            {
                Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Rb.linearVelocity.y * JumpCutMulitplier);
            }

            JumpReleased = false;
        }
    }

    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }
    private void SetAnimationState(string name, bool state)
    {
        Anim.SetBool(name, !Dying && !Respawning && state);
    }
    private void HandleAnimations()
    {
        bool moving = Mathf.Abs(MoveInput.x) > 0.1f && IsGrounded && !Crouched && !Shooting;


        {
            SetAnimationState("IsJumping", !IsGrounded && Rb.linearVelocity.y > 0.1f);
            SetAnimationState("IsGrounded", IsGrounded);
            SetAnimationState("IsIdle", !Shooting && IsGrounded && !moving && !Crouched);
            SetAnimationState("IsWalking", moving && !Running);
            SetAnimationState("IsRunning", moving && Running);
            SetAnimationState("IsCrouched", IsGrounded && Crouched);
            SetAnimationState("IsShooting", Shooting);

            SetAnimationState("IsFalling", !IsGrounded && Rb.linearVelocity.y < -0.1f);

            Anim.SetBool("Dying", Dying);
        }

        // Anim.SetBool("IsDashing", IsDashing);
        Anim.SetFloat("yVelocity", Rb.linearVelocity.y);

        IsLanding = Anim.GetCurrentAnimatorStateInfo(0).IsName("PlayerLand");

        // IsFalling = Anim.GetCurrentAnimatorStateInfo(0).IsName("Falling");
    }

    #endregion
    
    #region Helpers/Getters/Setters
    void ApplyVariableGravity()
    {
        if (Rb.linearVelocity.y < -0.1f)
        {
            Rb.gravityScale = FallGravity;
        }
        else if(Rb.linearVelocity.y > 0.1f)
        {
            Rb.gravityScale = JumpGravity;
        }
        else
        {
            Rb.gravityScale = NormalGravity;
        }
    }

    void Flip()
    {
        if (MoveInput.x > 0.1f)
        {
            FacingRight = true;
        }
        else if (MoveInput.x < -0.1f)
        {
            FacingRight = false;
        }

        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * (FacingRight ? 1.0f : -1.0f), transform.localScale.y, transform.localScale.z);
    }

    void CheckGrounded()
    {
        IsGrounded = Check.CheckGrounded(GroundLayerMask);
    }

    public void AddMoney(int amount)
    {
        Money += amount;
    }

    public void Grow()
    {
        TargetScale = 1.5f;

        float distance = Mathf.Abs(TargetScale - transform.localScale.y);
        float timeToTarget = distance / ScaleSpeed;

        StartCoroutine(FlashCharacter(timeToTarget));
    }

    public void Damage(bool forceKill = false)
    {
        if(Invulnerable || Respawning)
        {
            return;
        }

        if(forceKill || TargetScale == 1.0f)
        {
            StartCoroutine(Die());
            return;
        }
        
        TargetScale = 1.0f;

        StartCoroutine(DamageFlash(2.0f));
    }

    public void Shoot()
    {
        Instantiate(BulletPrefab, ShootSpot.position, ShootSpot.rotation, null).GetComponent<Bullet>().SetDirection(FacingRight);
    } 

    public void ShootAvailable()
    {
        Shooting = false;
    }

    public void DyingAnimationFinished()
    {
        Respawning = false;
    }

    #endregion

    #region Coroutines
    //Coroutines
    private IEnumerator FlashCharacter(float flashDuration)
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Color orgColor = spriteRenderer.color;

        float flashInterval = 0.1f;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            spriteRenderer.color = orgColor;

            yield return new WaitForSeconds(flashInterval);

            spriteRenderer.color = Color.clear;

            yield return new WaitForSeconds(flashInterval);

            elapsed += flashInterval * 2f;
        }

        // Restore the sprite.
        spriteRenderer.color = orgColor;
    }

    private IEnumerator DamageFlash(float invulnerabilityDuration)
    {
        Invulnerable = true;

        Rb.excludeLayers |= EnemeyLayer;

        yield return StartCoroutine(FlashCharacter(invulnerabilityDuration));

        Rb.excludeLayers &= ~EnemeyLayer;

        Invulnerable = false;
    }

    private IEnumerator _LoadLevel(string name, IEnumerator optionalWaitCoroutine = null)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(name);

        op.allowSceneActivation = false;

        if(optionalWaitCoroutine != null)
        {
            yield return StartCoroutine(optionalWaitCoroutine);
        }

        while(op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
    }

    public void LoadLevel(string name, IEnumerator optionalWaitCoroutine = null)
    {
        LoadingLevel = true;
        StartCoroutine(_LoadLevel(name, optionalWaitCoroutine));
    }

    private IEnumerator WaitForRespawn()
    {
        Respawning = true;
        Dying = true;

        while(Respawning)
        {
            yield return null;
        }
    }

    private IEnumerator Die()
    {
        RigidbodyType2D oldType = Rb.bodyType;
        Rb.bodyType = RigidbodyType2D.Static;

        yield return StartCoroutine(_LoadLevel(SceneManager.GetActiveScene().name, WaitForRespawn()));
    }
    
    private IEnumerator MoveCameraUp()
    {
        yield return new WaitForSeconds(LookUpTime);

        CPC.TargetOffset.y = LookUpOffset;
    }

    private IEnumerator MoveCameraDown()
    {
        yield return new WaitForSeconds(LookDownTime);

        CPC.TargetOffset.y = -LookDownOffset;
    }

    #endregion

    #region Events
    //Events
    void OnDrawGizmosSelected()
    {
        Check.OnDrawGizmosSelected();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                GroundNormal = contact.normal;
                return;
            }
        }
    }

    void OnMove(InputValue value)
    {
        if(IsLanding)
        {
            MoveInput = Vector2.zero;
            return;
        }
        
        MoveInput = value.Get<Vector2>();

        if(MoveInput.x != 0.0f)
        {
            MoveInput.y = 0.0f;
        }

        if(MoveInput.y > 0.0f && LookUpCoroutine == null)
        {
            if(LookDownCoroutine != null)
            {
                StopCoroutine(LookDownCoroutine);
                LookDownCoroutine = null;
            }
            LookUpCoroutine = StartCoroutine(MoveCameraUp());
        }
        else if(MoveInput.y < 0.0f && LookDownCoroutine == null)
        {
            if(LookUpCoroutine != null)
            {
                StopCoroutine(LookUpCoroutine);
                LookUpCoroutine = null;
            }
            LookDownCoroutine = StartCoroutine(MoveCameraDown());
        }
        else if(MoveInput.y == 0.0f)
        {
            CPC.TargetOffset.y = 0.0f;
            if(LookUpCoroutine != null)
            {
                StopCoroutine(LookUpCoroutine);
                LookUpCoroutine = null;
            }
            else if(LookDownCoroutine != null)
            {
                StopCoroutine(LookDownCoroutine);
                LookDownCoroutine = null;
            }
        }
    }

    void OnJump(InputValue value)
    {
        if(IsLanding || Shooting || Crouched)
        {
            JumpPressed = false;
            JumpReleased = false;
            return;
        }

        if(value.isPressed)
        {
            if(IsGrounded)
            {
                JumpPressed = true;
            }
            
            JumpReleased = false;
        }
        else
        {
            JumpPressed = false;
            JumpReleased = true;
        }
    }

    private IEnumerator Dash()
    {
        IsDashing = true;
        CanDash = false;

        float direction = FacingRight ? 1.0f : -1.0f;

        float gravity = Rb.gravityScale;

        // Disable gravity
        Rb.gravityScale = 0.0f;

        // Dash
        Rb.linearVelocity = new Vector2(direction * DashSpeed, 0.0f);

        yield return new WaitForSeconds(DashDuration);

        // End dash
        IsDashing = false;

        // Restore gravity
        Rb.gravityScale = gravity;

        // Optional: stop horizontal movement
        Rb.linearVelocity = new Vector2(0.0f, 0.0f);

        // Cooldown
        yield return new WaitForSeconds(DashCooldown);

        CanDash = true;
    }

    void OnDash(InputValue value)
    {
        if (value.isPressed && CanDash && !IsDashing && AllowMove)
        {
            StartCoroutine(Dash());
        }
    }

    void OnCrouch(InputValue value)
    {
        if(Shooting)
        {
            return;
        }

        if (value.isPressed  && IsGrounded)
        {
            Crouched = !Crouched;
            Rb.linearVelocity = new Vector2(0.0f, Rb.linearVelocityY);
        }
    }

    void OnRun(InputValue value)
    {
        if(value.isPressed)
        {
            Running = !Running;
        }
    }
    
    void OnShoot(InputValue value)
    {
        if(value.isPressed && IsGrounded)
        {
            Rb.linearVelocity = new Vector2(0.0f, Rb.linearVelocityY);
            Shooting = true;
            Crouched = false;
        }
    }

    void OnRestart(InputValue value)
    {
        if(value.isPressed)
        {
            Damage(true);
        }
    }

    #endregion
}
