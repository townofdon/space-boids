using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvertParentRotation : MonoBehaviour
{
    void Update()
    {
        if (transform.parent == null) return;
        transform.localRotation = Quaternion.Euler(0, 0, -transform.parent.rotation.eulerAngles.z);
    }
}
