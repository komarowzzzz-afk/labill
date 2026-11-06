
using UnityEngine;
using EzySlice;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class SliceObject : MonoBehaviour
{
    [Header("Slice Settings")]
    public Transform startSlicePoint;
    public Transform endSlicePoint;
    public VelocityEstimator velocityEstimator;

    [Header("Materials and Layers")]
    public LayerMask sliceableLayer;
    public Material crossSectionMaterial;

    [Header("Physics Settings")]
    public float cutForce = 10f;
    public float destroyDelay = 30f;

    private readonly HashSet<GameObject> slicedObjects =
        new HashSet<GameObject>();

    private void FixedUpdate()
    {
        // Проверяем назначение компонентов
        if (startSlicePoint == null ||
            endSlicePoint == null ||
            velocityEstimator == null)
        {
            Debug.LogError("SliceObject: Не назначены точки или VelocityEstimator!");
            return;
        }

        // Проверяем слой
        if (sliceableLayer.value == 0)
        {
            Debug.LogError("SliceObject: Не выбран Sliceable Layer!");
            return;
        }

        // Проверяем пересечение лезвия с объектом
        bool hasHit = Physics.Linecast(
            startSlicePoint.position,
            endSlicePoint.position,
            out RaycastHit hit,
            sliceableLayer
        );

        if (hasHit)
        {
            GameObject target = hit.collider.gameObject;

            if (!slicedObjects.Contains(target))
            {
                Debug.Log("Меч обнаружил объект: " + target.name);

                Slice(target);
            }
        }
    }

    public void Slice(GameObject target)
    {
        if (target == null)
            return;

        // Получаем скорость меча
        Vector3 velocity =
            velocityEstimator.GetVelocityEstimate();

        Debug.Log("Скорость меча: " + velocity.magnitude);

        if (velocity.sqrMagnitude < 0.01f)
        {
            Debug.Log("Разрез отменён: меч движется слишком медленно.");
            return;
        }

        // Вычисляем направление лезвия
        Vector3 bladeDirection =
            endSlicePoint.position - startSlicePoint.position;

        // Вычисляем нормаль плоскости разреза
        Vector3 planeNormal =
            Vector3.Cross(bladeDirection, velocity);

        if (planeNormal.sqrMagnitude < 0.0001f)
        {
            Debug.Log("Разрез отменён: неправильная нормаль плоскости.");
            return;
        }

        planeNormal.Normalize();

        // Выполняем разрез EzySlice
        SlicedHull hull = target.Slice(
            endSlicePoint.position,
            planeNormal
        );

        if (hull != null)
        {
            Debug.Log("УСПЕХ! Объект разрезан: " + target.name);

            GameObject upperHull =
                hull.CreateUpperHull(target, crossSectionMaterial);

            GameObject lowerHull =
                hull.CreateLowerHull(target, crossSectionMaterial);

            if (upperHull != null)
            {
                SetupSlicedComponent(upperHull, target);
                Destroy(upperHull, destroyDelay);
            }

            if (lowerHull != null)
            {
                SetupSlicedComponent(lowerHull, target);
                Destroy(lowerHull, destroyDelay);
            }

            Destroy(target);
        }
        else
        {
            Debug.Log("EzySlice не смог разрезать объект: " + target.name);
        }
    }

    public void SetupSlicedComponent(
        GameObject slicedObject,
        GameObject target)
    {
        // Настраиваем положение половинки
        slicedObject.transform.SetPositionAndRotation(
            target.transform.position,
            target.transform.rotation
        );

        slicedObject.transform.localScale =
            target.transform.localScale;

        // Добавляем физику
        Rigidbody rb =
            slicedObject.AddComponent<Rigidbody>();

        rb.mass = 1f;

        // Добавляем Mesh Collider
        MeshCollider collider =
            slicedObject.AddComponent<MeshCollider>();

        collider.convex = true;

        // Добавляем возможность захвата
        XRGrabInteractable grab =
            slicedObject.AddComponent<XRGrabInteractable>();

        Debug.Log("Создана половинка: " + slicedObject.name);
    }
}
