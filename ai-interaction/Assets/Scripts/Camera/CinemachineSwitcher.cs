using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] private InputAction action;
    private Animator animator;
    private bool isOverworldCamera = true;

    [SerializeField] private CinemachineVirtualCamera vcam1;
    [SerializeField] private CinemachineVirtualCamera vcam2;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (!animator)
            Debug.Log("Missing camera animator");
    } 

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    private void Start()
    {
        action.performed += _ => SwitchState();
    }

    private void SwitchState()
    {
        Debug.Log("Switch camera");
        if (isOverworldCamera)
        {
            animator.Play("FollowAdventurerCamera");
        }
        else
        {
            animator.Play("OverworldCamera");
        }
        isOverworldCamera = !isOverworldCamera; 
    }

    private void SwitchPriority()
    {
        if (isOverworldCamera)
        {
            vcam1.Priority = 0;
            vcam2.Priority = 1;
        }
        else{
            vcam1.Priority = 1;
            vcam2.Priority = 0;
        }
        isOverworldCamera = !isOverworldCamera;
    }
}
