using System.Collections;
using System.Collections.Generic;
using Tobii.G2OM;
using UnityEngine;

public class TargetEyegazeScript : MonoBehaviour, IGazeFocusable
{
    public bool isLookedAt;
    public void GazeFocusChanged(bool hasFocus)
    {
        isLookedAt = hasFocus;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
