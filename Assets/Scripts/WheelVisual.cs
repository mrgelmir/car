using UnityEngine;

public class WheelVisual : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float diameter = 1f;
    [SerializeField] private Transform wheel;


    private float rotationVelocity = 0f;

    public void SetData(float velocity, bool hasContact)
    {
        // Rotate tire based on speed
        float circumference = Mathf.PI / 2 * diameter;
        rotationVelocity = (velocity / circumference) * Mathf.Rad2Deg;

        // Add skid marks if going sideways
        trail.enabled = hasContact;
    }

    private void Update()
    {
        wheel.localRotation *= Quaternion.Euler(Vector3.up * (rotationVelocity * Time.deltaTime));
    }
}