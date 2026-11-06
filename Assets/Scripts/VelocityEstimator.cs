
using UnityEngine;
using System.Collections;

public class VelocityEstimator : MonoBehaviour
{
    public int velocityAverageFrames = 5;
    public int angularVelocityAverageFrames = 11;
    public bool estimateOnAwake = false;

    private Coroutine routine;
    private int sampleCount;

    private Vector3[] velocitySamples;
    private Vector3[] angularVelocitySamples;

    public void BeginEstimatingVelocity()
    {
        FinishEstimatingVelocity();
        routine = StartCoroutine(EstimateVelocityCoroutine());
    }

    public void FinishEstimatingVelocity()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    public Vector3 GetVelocityEstimate()
    {
        Vector3 velocity = Vector3.zero;

        int count = Mathf.Min(sampleCount, velocitySamples.Length);

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
                velocity += velocitySamples[i];

            velocity /= count;
        }

        return velocity;
    }

    public Vector3 GetAngularVelocityEstimate()
    {
        Vector3 velocity = Vector3.zero;

        int count = Mathf.Min(sampleCount, angularVelocitySamples.Length);

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
                velocity += angularVelocitySamples[i];

            velocity /= count;
        }

        return velocity;
    }

    private void Awake()
    {
        velocityAverageFrames = Mathf.Max(1, velocityAverageFrames);
        angularVelocityAverageFrames = Mathf.Max(1, angularVelocityAverageFrames);

        velocitySamples = new Vector3[velocityAverageFrames];
        angularVelocitySamples = new Vector3[angularVelocityAverageFrames];

        if (estimateOnAwake)
            BeginEstimatingVelocity();
    }

    private IEnumerator EstimateVelocityCoroutine()
    {
        sampleCount = 0;

        Vector3 previousPosition = transform.position;
        Quaternion previousRotation = transform.rotation;

        while (true)
        {
            yield return new WaitForEndOfFrame();

            float factor = 1f / Mathf.Max(Time.deltaTime, 0.0001f);

            int v = sampleCount % velocitySamples.Length;
            int w = sampleCount % angularVelocitySamples.Length;

            sampleCount++;

            velocitySamples[v] =
                (transform.position - previousPosition) * factor;

            Quaternion deltaRotation =
                transform.rotation * Quaternion.Inverse(previousRotation);

            float theta = 2f * Mathf.Acos(
                Mathf.Clamp(deltaRotation.w, -1f, 1f));

            if (theta > Mathf.PI)
                theta -= 2f * Mathf.PI;

            Vector3 angularVelocity = new Vector3(
                deltaRotation.x,
                deltaRotation.y,
                deltaRotation.z
            );

            if (angularVelocity.sqrMagnitude > 0f)
            {
                angularVelocity =
                    theta * factor * angularVelocity.normalized;
            }

            angularVelocitySamples[w] = angularVelocity;

            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }
    }

    private void OnDisable()
    {
        FinishEstimatingVelocity();
    }
}
