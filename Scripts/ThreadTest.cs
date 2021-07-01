using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Voxul.Utilities;

public class ThreadTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Am I nuts??");
        UnityMainThreadDispatcher.EnsureSubscribed();
        var t = new Task(() => UnityMainThreadDispatcher.Enqueue(() => Debug.Log("I am not")));
        t.Start();
    }
}
