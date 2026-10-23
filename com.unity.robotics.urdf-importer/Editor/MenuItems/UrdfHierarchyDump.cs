using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UrdfHierarchyDump
{
    [MenuItem("Tools/URDF/Dump Selected Robot")]
    private static void DumpSelectedRobot()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogWarning("[URDF][HierarchyDump] Select a robot root GameObject in the Hierarchy first.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Robot: {root.name}");
        sb.AppendLine($"Unity Version: {Application.unityVersion}");
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        DumpTransform(root.transform, sb, 0);

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, "URDF_HierarchyDump.txt");
        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"[URDF][HierarchyDump] Saved: {outputPath}");
    }

    [MenuItem("Tools/URDF/Dump Selected Robot", true)]
    private static bool ValidateDumpSelectedRobot() => Selection.activeGameObject != null;

    private static void DumpTransform(Transform transform, StringBuilder sb, int depth)
    {
        GameObject go = transform.gameObject;
        string indent = new string(' ', depth * 2);
        string detail = indent + "  ";

        sb.AppendLine($"{indent}[GameObject] {go.name}");
        sb.AppendLine($"{detail}ActiveSelf={go.activeSelf}, ActiveInHierarchy={go.activeInHierarchy}");
        sb.AppendLine($"{detail}Position={FormatVector(transform.localPosition)}, Rotation={FormatVector(transform.localEulerAngles)}, Scale={FormatVector(transform.localScale)}");
        sb.AppendLine($"{detail}PrefabInstance={PrefabUtility.IsPartOfPrefabInstance(go)}");

        foreach (MeshFilter filter in go.GetComponents<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            sb.AppendLine($"{detail}[MeshFilter] Mesh={(mesh != null ? mesh.name : "NULL/MISSING")}");
            if (mesh != null)
            {
                string path = AssetDatabase.GetAssetPath(mesh);
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long referenceId))
                    sb.AppendLine($"{detail}  AssetPath={path}, GUID={guid}, ReferenceID={referenceId}");
                else
                    sb.AppendLine($"{detail}  AssetPath={path}, GUID=UNAVAILABLE, ReferenceID=UNAVAILABLE");
            }
        }

        foreach (MeshRenderer renderer in go.GetComponents<MeshRenderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            sb.AppendLine($"{detail}[MeshRenderer] Enabled={renderer.enabled}, Materials={materials.Length}");
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    sb.AppendLine($"{detail}  Material=NULL/MISSING");
                    continue;
                }
                string shaderName = material.shader != null ? material.shader.name : "NULL/MISSING";
                sb.AppendLine($"{detail}  Material={material.name}, Shader={shaderName}, Path={AssetDatabase.GetAssetPath(material)}");
            }
        }

        foreach (MeshCollider collider in go.GetComponents<MeshCollider>())
        {
            Mesh mesh = collider.sharedMesh;
            sb.AppendLine($"{detail}[MeshCollider] Mesh={(mesh != null ? mesh.name : "NULL/MISSING")}, Convex={collider.convex}, Enabled={collider.enabled}");
            if (mesh != null)
                sb.AppendLine($"{detail}  AssetPath={AssetDatabase.GetAssetPath(mesh)}");
        }

        foreach (ArticulationBody body in go.GetComponents<ArticulationBody>())
            sb.AppendLine($"{detail}[ArticulationBody] JointType={body.jointType}, Immovable={body.immovable}, Mass={body.mass}");

        foreach (Transform child in transform)
            DumpTransform(child, sb, depth + 1);
    }

    private static string FormatVector(Vector3 value) => $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
}
