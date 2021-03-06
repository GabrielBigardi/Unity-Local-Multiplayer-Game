using System;
using UnityEngine;
using GabrielBigardi.Animator;

public class PlayerEntity : MonoBehaviour
{
    #region Player/States
    public PlayerCore core;
    public PlayerData data;

    public StateMachine StateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerRunState RunState { get; private set; }
    public PlayerAirState AirState { get; private set; }
    public PlayerHurtState HurtState { get; private set; }
    public PlayerWaterState WaterState { get; private set; }
    public PlayerDeathState DeathState { get; private set; }
    #endregion

    #region Transitions Func
    Func<bool> InputXZero() => () => core.playerInputHandler.mov.x == 0f;
    Func<bool> InputXNotZero() => () => core.playerInputHandler.mov.x != 0f;
    Func<bool> NotGrounded() => () => !IsGrounded;

    Func<bool> GroundedInputXZero() => () => IsGrounded && core.playerInputHandler.mov.x == 0f;
    Func<bool> GroundedInputXNotZero() => () => IsGrounded && core.playerInputHandler.mov.x != 0f;

    Func<bool> OnWater() => () => IsOnWater;
    Func<bool> NotOnWaterNotGrounded() => () => !IsOnWater && !IsGrounded;

    Func<bool> Running() => () => core.playerInputHandler.holdingRun && core.playerInputHandler.mov.x != 0f;
    Func<bool> NotRunning() => () => !core.playerInputHandler.holdingRun || core.playerInputHandler.mov.x == 0f;

    Func<bool> IsDead() => () => core.playerHealth.curHealth <= 0;
    #endregion

    public bool IsGrounded => Physics2D.OverlapCircle(core.groundCheckPoint.position, 0.02f, data.groundLayer);
    public bool IsOnWater => Physics2D.OverlapCircle(core.groundCheckPoint.position, 0.02f, data.waterLayer);

    public bool IsOnWaterSuperState => this.StateMachine._currentState is PlayerWaterState;

    public int CurrentFrame => core.anim.CurrentFrame;
    public string currentState;

    public int currentTeam;

    private void Start()
    {
        InitializeComponents();
        InitializeStateMachine();
        AddStateMachineTransitions();

        StateMachine.SetState(IdleState);

        core.col = GetComponent<Collider2D>();
        core.currentShootDelay = data.shootDelay;
        core.anim.AnimationEventCalled += EventTest;
    }

    private void OnDestroy()
    {
        core.anim.AnimationEventCalled -= EventTest;
    }

    private void EventTest(string eventName)
    {
        if (eventName == "") return;

        //print($"Event named {eventName} has been called");

        if(eventName == "Idle01")
        {
            core.spriteRenderer.color = Color.red;
        }

        if (eventName == "Idle02")
        {
            core.spriteRenderer.color = Color.green;
        }
    }

    private void Update()
    {
        StateMachine.Tick();
        core.currentShootDelay -= Time.deltaTime;
        core.currentJumpBufferTime -= Time.deltaTime;

        if (core.currentJumpBufferTime <= 0)
        {
            core.jumpBuffer = false;
        }
    }
	
	private void FixedUpdate()
	{
        StateMachine.FixedTick();

        if (core.rgbd.velocity.y < -10f)
            core.rgbd.velocity = new Vector2(core.rgbd.velocity.x, -10f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Damage"))
        {
            TakeDamage(25, true);
        }

        if (collision.CompareTag("Death"))
        {
            TakeDamage(core.playerHealth.maxHealth, false);
        }

        if (collision.CompareTag("Pickup"))
        {
            Heal(15);
            Destroy(collision.gameObject);
        }

        if(collision.CompareTag("BulletRed") && currentTeam == 1)
        {
            TakeDamage(collision.GetComponent<Bullet>().baseDamage, true);
        }

        if (collision.CompareTag("BulletGreen") && currentTeam == 0)
        {
            TakeDamage(collision.GetComponent<Bullet>().baseDamage, true);
        }

        if(collision.gameObject.TryGetComponent(out IBumpable _IBumpable))
        {
            _IBumpable.OnBump(transform);
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Death"))
        {
            TakeDamage(core.playerHealth.maxHealth, false);
        }

        if (collision.gameObject.TryGetComponent(out IBumpable _IBumpable))
        {
            _IBumpable.OnBump(transform);
        }
    }

    public void InitializeComponents()
    {
        core.anim = GetComponent<SpriteAnimator>();
        core.spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        core.rgbd = GetComponent<Rigidbody2D>();
    }

    public void InitializeStateMachine()
    {
        StateMachine = new StateMachine();

        IdleState = new PlayerIdleState(StateMachine, this);
        WalkState = new PlayerWalkState(StateMachine, this);
        RunState = new PlayerRunState(StateMachine, this);

        AirState = new PlayerAirState(StateMachine, this);

        HurtState = new PlayerHurtState(StateMachine, this);

        WaterState = new PlayerWaterState(StateMachine, this);

        DeathState = new PlayerDeathState(StateMachine, this);

    }

    public void AddStateMachineTransitions()
    {
        //Idle
        StateMachine.AddTransition(IdleState, WalkState, InputXNotZero());
        StateMachine.AddTransition(IdleState, AirState, NotGrounded());

        //Walk
        StateMachine.AddTransition(WalkState, IdleState, InputXZero());
        StateMachine.AddTransition(WalkState, AirState, NotGrounded());
        StateMachine.AddTransition(WalkState, RunState, Running());

        //Run
        StateMachine.AddTransition(RunState, WalkState, NotRunning());
        StateMachine.AddTransition(RunState, AirState, NotGrounded());

        ////Hurt
        //StateMachine.AddTransition(HurtState, IdleState, GroundedInputZeroLastFrame());
        //StateMachine.AddTransition(HurtState, WalkState, GroundedInputNotZeroLastFrame());
        //StateMachine.AddTransition(HurtState, AirState, NotGroundedLastFrame());

        ////Death
        //StateMachine.AddTransition(DeathState, IdleState, GroundedInputZeroLastFrame());
        //StateMachine.AddTransition(DeathState, WalkState, GroundedInputNotZeroLastFrame());
        //StateMachine.AddTransition(DeathState, AirState, NotGroundedLastFrame());

        //Air
        StateMachine.AddTransition(AirState, IdleState, GroundedInputXZero());
        StateMachine.AddTransition(AirState, WalkState, GroundedInputXNotZero());

        //Water
        StateMachine.AddTransition(WaterState, AirState, NotOnWaterNotGrounded());

        StateMachine.AddAnyTransition(WaterState, OnWater());
        StateMachine.AddAnyTransition(DeathState, IsDead());
    }

    public void SetVelocityX(float velocity)
    {
        core.rgbd.velocity = new Vector2(velocity, core.rgbd.velocity.y);
    }

    public void SetVelocityY(float velocity)
    {
        core.rgbd.velocity = new Vector2(core.rgbd.velocity.x, velocity);
    }

    public void SetFrozen(bool freeze)
    {
        if (freeze)
        {
            core.rgbd.isKinematic = true;
            core.rgbd.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            core.rgbd.isKinematic = false;
            core.rgbd.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
    }

    public void FlipSpriteRenderer(bool flip)
    {
        core.spriteRenderer.flipX = flip;
    }

    public void FlipModel(bool flip)
    {
        core.model.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
    }

    public void RotateModel(Vector3 rotation)
    {
        core.model.localRotation = Quaternion.Euler(rotation);
    }

    public void PlayAnimation(string animationName)
    {
        core.anim.Play(animationName);
    }

    public void FlipBasedOnInput(bool onlySpriteRenderer)
    {
        if (core.playerInputHandler.mov.x < 0)
        {
            if (onlySpriteRenderer) FlipSpriteRenderer(true); else FlipModel(true);
        }
        else if (core.playerInputHandler.mov.x > 0)
        {
            if (onlySpriteRenderer) FlipSpriteRenderer(false); else FlipModel(false);
        }
    }

    public void Jump()
    {
        SetVelocityY(data.jumpForce);
        InstantiateJumpParticles();
    }

    public void JumpCheck()
    {
        if (IsGrounded)
        {
            if (core.playerInputHandler.jump)
            {
                Jump();
            }
            else
            {
                if (core.jumpBuffer)
                {
                    Jump();
                    core.jumpBuffer = false;
                }
            }
        }
        else
        {
            if (core.playerInputHandler.jump)
            {
                if (!core.doubleJump)
                {
                    Jump();
                    core.doubleJump = true;
                }
                else
                {
                    core.currentJumpBufferTime = data.jumpBufferTime;
                    core.jumpBuffer = true;
                }
            }
        }

        core.playerInputHandler.jump = false;
    }

    public void InstantiateJumpParticles()
    {
        Instantiate(core.jumpParticlesPrefab, transform.position + new Vector3(0f, -0.5f, 0f), core.jumpParticlesPrefab.rotation);
    }

    public void Shoot()
    {
        if (core.playerInputHandler.shoot && core.currentShootDelay <= 0f)
        {
            core.currentShootDelay = data.shootDelay;
            Transform bulletTransform = Instantiate(core.bulletPrefab, core.shootPoint.position, Quaternion.identity);
            bulletTransform.gameObject.GetComponent<SpriteRenderer>().color = currentTeam == 0 ? Color.red : Color.green;
            bulletTransform.tag = currentTeam == 0 ? "BulletRed" : "BulletGreen";
            Rigidbody2D bulletRgbd = bulletTransform.gameObject.GetComponent<Rigidbody2D>();
            bulletRgbd.velocity = new Vector2(core.model.localScale.x == -1f ? -20f : 20f, 0f);
            //bulletRgbd.velocity = new Vector2(core.spriteRenderer.flipX ? -10f : 10f, 0f);
        }
    }

    public void TakeDamage(int damageToTake, bool hurtState)
    {
        if(hurtState && !IsOnWaterSuperState)
            StateMachine.SetState(HurtState);

        core.playerHealth.TakeDamage(damageToTake);
    }

    public void Heal(int healAmount)
    {
        core.playerHealth.Heal(healAmount);
    }

    public void SetTeam(int teamId)
    {
        currentTeam = teamId;
        core.nameText.color = teamId == 0 ? Color.red : Color.green;
    }

    public void ShowNameText()
    {
        core.nameText.gameObject.SetActive(true);
        foreach (var nameTextOutline in core.nameTextOutlines)
        {
            nameTextOutline.gameObject.SetActive(true);
        }
    }

    public void HideNameText()
    {
        core.nameText.gameObject.SetActive(false);
        foreach (var nameTextOutline in core.nameTextOutlines)
        {
            nameTextOutline.gameObject.SetActive(false);
        }
    }

    public void ShowHealthBar()
    {
        core.playerHealth.healthBarParent.SetActive(true);
    }

    public void HideHealthBar()
    {
        core.playerHealth.healthBarParent.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(core.groundCheckPoint.position, 0.02f);
    }
}
