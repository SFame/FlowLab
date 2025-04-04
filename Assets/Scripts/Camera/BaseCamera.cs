using UnityEngine;

public class BaseCamera : MonoBehaviour
{
    [SerializeField]
    private Transform Target;


    // Update is called once per frame
    void Update()
    {
        if (!Target) return;
        // x,y 좌표만 따라가고 z좌표는 유지
        transform.position = new Vector3(Target.position.x, Target.position.y, transform.position.z);
    }
}
