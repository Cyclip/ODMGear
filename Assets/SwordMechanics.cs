using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMechanics : MonoBehaviour
{
    public Camera mainCam;
    public AnimationClip attack;
    Animation anim;

    private float lastAttack;
    private bool attacking = false;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        //animator = GetComponent<Animator>();
        //animator.SetTrigger("Idle");
        anim = GetComponent<Animation>();
        anim.clip = attack;
        anim["SwordAttack1"].speed = 2f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Attack") && !attacking)
        {
            attacking = true;
            anim.Play();
            Debug.Log("attacking");
        } else if (attacking)
        {
            attacking = false;
            Debug.Log("not attacking");
        }
    }
}
