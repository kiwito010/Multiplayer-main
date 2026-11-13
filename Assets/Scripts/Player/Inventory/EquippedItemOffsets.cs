using UnityEngine;

public class EquippedItemOffsets : MonoBehaviour {

    [Header("Primera persona")]
    public Vector3 firstPersonPosition;
    public Vector3 firstPersonRotation;

    [Header("Tercera persona")]
    public Vector3 thirdPersonPosition;
    public Vector3 thirdPersonRotation;

    public void ApplyOffset(bool isFirstPerson) {

        if (isFirstPerson) {
            transform.localPosition = firstPersonPosition;

            transform.localRotation =
                Quaternion.Euler(firstPersonRotation);
        } else {
            transform.localPosition = thirdPersonPosition;

            transform.localRotation =
                Quaternion.Euler(thirdPersonRotation);
        }
    }
}
