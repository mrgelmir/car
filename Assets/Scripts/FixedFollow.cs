using UnityEngine;

public class FixedFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private Vector3 positionOffset;
    private Vector3 orientationOffset;

    private void Start()
    {
        positionOffset = transform.position - target.position;
        orientationOffset = transform.localEulerAngles - target.localEulerAngles;
    }

    private void LateUpdate()
    {
        transform.position = target.position + positionOffset;
        transform.localEulerAngles = target.localEulerAngles + orientationOffset;
    }
}