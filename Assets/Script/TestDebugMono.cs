using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDebugMono : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if DEV_MODE2

    Debug.Log("111");
#endif
#if DEV_MODE
    Debug.Log("22222");
#endif
    }

    
}
