using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PickableOutline : MonoBehaviour {
    [Header("Material del contorno")]
    [SerializeField] private Material outlineMaterial;

    private readonly List<GameObject> outlineObjects =
        new List<GameObject>();

    private bool created;

    private void Awake() {
        CreateOutline();
    }

    private void CreateOutline() {
        if (created || outlineMaterial == null)
            return;

        created = true;

        MeshFilter[] meshes =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter source in meshes) {
            MeshRenderer originalRenderer =
                source.GetComponent<MeshRenderer>();

            if (originalRenderer == null ||
                source.sharedMesh == null)
                continue;

            GameObject outlineObject =
                new GameObject("OutlineVisual");

            outlineObject.transform.SetParent(
                source.transform, false
            );

            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter =
                outlineObject.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = source.sharedMesh;

            MeshRenderer renderer =
                outlineObject.AddComponent<MeshRenderer>();

            int materialCount =
                Mathf.Max(1, originalRenderer.sharedMaterials.Length);

            Material[] materials = new Material[materialCount];

            for (int i = 0; i < materialCount; i++)
                materials[i] = outlineMaterial;

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            outlineObject.SetActive(false);
            outlineObjects.Add(outlineObject);
        }
    }

    public void SetHighlighted(bool highlighted) {
        if (!created)
            CreateOutline();

        foreach (GameObject outlineObject in outlineObjects) {
            if (outlineObject != null &&
                outlineObject.activeSelf != highlighted) {
                outlineObject.SetActive(highlighted);
            }
        }
    }

    private void OnDisable() {
        SetHighlighted(false);
    }
}
