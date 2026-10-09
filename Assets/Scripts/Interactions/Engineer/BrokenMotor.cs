using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrokenMotor : MonoBehaviour, IInteractable
{
    [SerializeField] private GameInput gameInput;
    [SerializeField] private MotorUI MotorUI;

    private bool isUsing;


    public void Interact() { 
        
    }
}
