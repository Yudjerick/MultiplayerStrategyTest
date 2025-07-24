using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HighlightController : NetworkBehaviour
{
    public void SetSelectionHighlight(bool value)
    {
        if(value)
        {
            GetComponent<MeshRenderer>().material.color = Color.green;
        }
        else
        {
            GetComponent<MeshRenderer>().material.color = Color.white;
        }
    }

    public void SetAttackHighlight(bool value)
    {
        if (value)
        {
            GetComponent<MeshRenderer>().material.color = Color.red;
        }
        else
        {
            var mr = GetComponent<MeshRenderer>();
            if(mr != null)
            {
                mr.material.color = Color.white;
            }
            
        }
    }
}
