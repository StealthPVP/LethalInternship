using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -6f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Rotation")]
    [SerializeField] private bool lockRotation = true;
    [SerializeField] private Vector2 rotationAngles = new Vector2(60f, 0f);
    [SerializeField] private float rotationSmoothTime = 0.1f;

    private Vector3 positionVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, smoothTime);

        if (lockRotation)
        {
            Quaternion desiredRotation = Quaternion.Euler(rotationAngles.x, rotationAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime / Mathf.Max(0.0001f, rotationSmoothTime));
        }
        else
        {
            transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized, Vector3.up);
        }
    }
}
