﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(CharacterManager))]
public class Enemy : MonoBehaviour
{
    public PlayerManager pc;
        public AttackScript myAttack;
        public Animator myAnim;
        public int idleTimer, proneTimer, startupTimer, activeTimer, endlagTimer, hitStunTimer, idleMax, proneMax, dyingTimer;
        public float groundLevel, fallSpeed;
        public float currentHP, maxHP;
        
        private GameObject hitfx;

        public bool active, vulnerable;
        public enum EnemyState
        {
            Idle,
            Walking,
            AttackStartup,
            AttackActive,
            AttackEndlag,
            HitStun,
            Airborn,
            Prone,
            Dying
        }
    
        public EnemyState myState;
        
        // Start is called before the first frame update
        void Start()
        {
            maxHP = GetComponent<CharacterManager>().life; //HP is called "life" in CharacterManager, setting this up to link with Enemy script
            currentHP = maxHP;
            hitfx = GetComponent<CharacterManager>().hitfx;
            myState = EnemyState.Idle;
            pc = GameObject.FindWithTag("Player").GetComponent<PlayerManager>();
        }
    
        // Update is called once per frame
        void FixedUpdate()
        {
            switch (myState)
            {
                case EnemyState.Idle:
                    idleTimer--;
                    if (idleTimer <= 0)
                    {
                        if (myAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack") == false && Mathf.Abs(pc.transform.position.x - transform.position.x) <= myAttack.horizontalRange &&
                            Mathf.Abs(pc.transform.position.y - transform.position.y) <= myAttack.verticalRange)
                        {
                            EnterState(EnemyState.AttackActive);
                            myState = EnemyState.AttackActive;
                            activeTimer = myAttack.activeTime;
                        }
                        else
                        {
                            EnterState(EnemyState.Walking);
                        }
                    }
                    break;
                case EnemyState.Walking:
                    if (myAnim.GetCurrentAnimatorStateInfo(0).IsName("Attack") == false && Mathf.Abs(pc.transform.position.x - transform.position.x) <= myAttack.horizontalRange &&
                        Mathf.Abs(pc.transform.position.y - transform.position.y) <= myAttack.verticalRange)
                    {
                        EnterState(EnemyState.AttackActive);
                    }
                    else
                    {
                        //original code:
                        //transform.Translate(pc.transform.position - transform.position);
                        //note: enemy would instantly teleport to player position
                        //quick fix (need to come up with a better way for enemy movement):
                        //Debug.Log("Enemy Moving");
                        //transform.Translate(Time.fixedDeltaTime*(pc.transform.position - transform.position)/5);
                        //new code:
                        transform.position = Vector3.MoveTowards(transform.position, pc.transform.position, 0.025f);
                    }
                    break;
//                case EnemyState.AttackStartup:
//                    startupTimer--;
//                    if (startupTimer <= 0)
//                    {
//                        EnterState(EnemyState.AttackActive);
//                    }
//                    break;
                case EnemyState.AttackActive:
                    activeTimer--;
                    CheckHitBox();
                    if (activeTimer <= 0)
                    {
                        EnterState(EnemyState.Idle);
                    }
                    break;
//                case EnemyState.AttackEndlag:
//                    endlagTimer--;
//                    if (endlagTimer <= 0)
//                    {
//                        EnterState(EnemyState.Idle);
//                    }
//                    break;
                case EnemyState.HitStun:
                    hitStunTimer--;
                    //Debug.Log("Ouch");
                    if (hitStunTimer <= 0)
                    {
                        EnterState(EnemyState.Idle);
                    }
                    break;
                case EnemyState.Airborn:
                    transform.Translate(transform.position.x, transform.position.y - fallSpeed, transform.position.x);
                    if (transform.position.y <= groundLevel)
                    {
                        EnterState(EnemyState.Prone);
                    }
                    break;
                case EnemyState.Prone:
                    proneTimer--;
                    if (proneTimer <= 0)
                    {
                        vulnerable = true;
                        EnterState(EnemyState.Idle);
                    }
                    break;
                case EnemyState.Dying:
                    dyingTimer--;
                    if (dyingTimer <= 0)
                    {
                        Destroy(gameObject);
                    }
                    break;
            }
        }
    
        public void EnterState(EnemyState endState)
        {
            myState = endState;
            switch (endState)
            {
                case EnemyState.Idle:
                    myAnim.Play("Idle");
                    idleTimer = idleMax;
                    break;
                case EnemyState.Walking:
                    myAnim.Play("Walking");
                    break;
                case EnemyState.AttackActive:
                    myAnim.Play("Attack");
                    myAttack.enabled = true;
                    myAttack.hitYet = false;
                    activeTimer = myAttack.startupTime;
                    break;
                case EnemyState.HitStun:
                    myAnim.Play("HitStun");
                    break;
                case EnemyState.Airborn:
                    vulnerable = false;
                    groundLevel = transform.position.x;
                    myAnim.Play("Fall");
                    break;
//                case EnemyState.Prone:
//                    vulnerable = false;
//                    myAnim.Play("Prone");
//                    proneTimer = proneMax;
//                    break;
                case EnemyState.Dying:
                    myAnim.Play("Dying");
                    dyingTimer = 30;
                    break;
            }
        }

        private void CheckHitBox()
        {
            RaycastHit2D[] boxResult;
            boxResult = Physics2D.BoxCastAll(gameObject.transform.position + new Vector3(0, 3f), new Vector2(3, 4), 0f,
                new Vector2(1, 0), 1.5f, 1 << 8);
            for (int i = 0; i < boxResult.Length; i++)
            {
                print(boxResult[i].collider.name);
                if (boxResult[i].collider != null && boxResult[i].collider.CompareTag("Player"))
                {
                    pc = boxResult[i].collider.GetComponent<PlayerManager>();
                    pc.currentHp--;
                    pc.GetComponent<PlayerManager>().GetHit(GetComponent<AttackScript>());
                }
                else
                    AudioManager.instance.PlayClip("punchwhiff");
            }
        }

        public void GetHit(AttackScript hitBy)
        {
            if (Input.GetKeyDown(KeyCode.Z))
                AudioManager.instance.PlayClip("punched");
            else if(Input.GetKeyDown(KeyCode.X))
                AudioManager.instance.PlayClip("kicked");
            currentHP -= hitBy.damage;
            var particlepos = new Vector2(transform.position.x-1.2f,transform.position.y +3);
            var hitfxclone = Instantiate(hitfx, particlepos, Quaternion.identity);
            hitStunTimer = 120;
            EnterState(EnemyState.HitStun);
            Destroy(hitfxclone, 1f);
        }
}
