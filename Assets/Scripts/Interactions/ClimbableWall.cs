using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbableWall : MonoBehaviour
{
    [SerializeField] private Transform topDestination;
    [SerializeField] private Transform bottomDestination;

    public Vector3 GetTopDestination() { 
        return topDestination.transform.position;
    }

    public Vector3 GetBottomDestination() { 
        return bottomDestination.transform.position;
    }
}
