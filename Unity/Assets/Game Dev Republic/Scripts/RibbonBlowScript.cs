using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RibbonBlowScript : MonoBehaviour
{
    public bool stopHairMove;
    public GameObject[] ribbonJoints;
    int numberOfHairJoints;

    [Space]
    public float moveSpeed;
    
    [Header("Rotate in X, Y, & Z")]
    public bool canRotateX;
    public bool canRotateY;
    public bool canRotateZ;

    [Header("Custom Roation Add Values")]
    public bool useCustomRoationValues;


    public Vector3 roatationAddV3;


    void Start()
    {
        if(ribbonJoints.Length > 0)
        {
            numberOfHairJoints = ribbonJoints.Length;

        }
        if (!stopHairMove)
        {
            if(moveSpeed <= 0)
            {
                moveSpeed = 1f;
            }
        }
        if(canRotateX && canRotateY && canRotateZ)
        {
            if (!useCustomRoationValues)
            {
                roatationAddV3 = new Vector3(10, 10, 10);
            }
        }
        else
        {
            if (canRotateX)
            {
                if (!useCustomRoationValues)
                {
                    roatationAddV3.x = 10;
                }
                
            }
            else if (!canRotateX)
            {
                roatationAddV3.x = 0;
            }
           
            if (canRotateY)
            {
                if (!useCustomRoationValues)
                {
                    roatationAddV3.y = 10;
                }
            }
            else if (!canRotateY)
            {
                roatationAddV3.y = 0;
            }

            if (canRotateZ)
            {
                if (!useCustomRoationValues)
                {
                    roatationAddV3.z = 10;
                }
            }
            else if (!canRotateZ)
            {
                roatationAddV3.z = 0;
            }
        }
        BlowHairFront();


    }

    // Update is called once per frame
    void Update()
    {
        if (stopHairMove)
        {
            StopCoroutine("HairBlowingBack");
            StopCoroutine("HairBlowingFront");
        }
    }

    void BlowHairFront()
    {
        StopCoroutine("HairBlowingBack");
        StartCoroutine("HairBlowingFront");
    }

    void BlowHairBack()
    {
        StopCoroutine("HairBlowingFront");
        StartCoroutine("HairBlowingBack");
    }

    public IEnumerator HairBlowingFront()
    {
        for (int i = 0; i < numberOfHairJoints; i++)
        {
            ribbonJoints[i].transform.DOLocalRotate(ribbonJoints[i].transform.localRotation.eulerAngles + roatationAddV3, moveSpeed);

            if (i == numberOfHairJoints - 1)
            {
                yield return new WaitForSeconds(moveSpeed);
                BlowHairBack();
            }
        }
        yield return null;
    }

    public IEnumerator HairBlowingBack()   
    {
        for (int i = 0; i < numberOfHairJoints; i++)
        {
            ribbonJoints[i].transform.DOLocalRotate(ribbonJoints[i].transform.localRotation.eulerAngles - roatationAddV3, moveSpeed);

            if (i == numberOfHairJoints - 1)
            {
                yield return new WaitForSeconds(moveSpeed);
                BlowHairFront();
            }
        }
        yield return null;
    }
}
