using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    interface ITask
    {
        
    }
    class TTask : ITask, IComparer
    {
        public int Compare(object x, object y)
        {
            0;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        ITask t = new TTask();
        
        ShowInterfaceProperties(t);
        
    }

    public static T ShowInterfaceProperties<T>(T original)
    {
        Debug.Log(original);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
