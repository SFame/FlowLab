using UnityEngine;

public class BaseCamera : MonoBehaviour
{
    [SerializeField]
    private Transform Target;
    [SerializeField] float FollowSpeed = 2f;

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;
        // x,y 좌표만 따라가고 z좌표는 유지
        Vector3 newPos = new Vector3(Target.position.x, Target.position.y, -10f);
        transform.position = Vector3.Slerp(transform.position, newPos, FollowSpeed);
    }
}
