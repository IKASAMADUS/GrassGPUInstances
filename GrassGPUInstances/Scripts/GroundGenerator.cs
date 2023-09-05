using System.Collections.Generic;
using UnityEngine;

namespace IKASAMADUS
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundGenerator : MonoBehaviour
    {
        [Header("地形块大小")] [SerializeField] private int _size = 10;
        [Header("地形高度强度")][SerializeField] private float _heightStrength = 0f;
        [Header("随机噪声变化")][SerializeField] private float _noiseStrength = 10f;
        private void Awake()
        {
            RebuildMesh();
        }

        [ContextMenu("RebuildMesh")]
        private void RebuildMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateMesh(_size,_noiseStrength,_heightStrength);
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Shader Graphs/Terrain"));
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static Mesh CreateMesh(int size = 100, float noise = 10, float heightStrength = 0f)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            for (var x = 0; x <= size; x++)
            {
                for (var z = 0; z <= size; z++)
                {
                    //var height = 0; //Mathf.PerlinNoise(x / 10f,z/10f) * 5;
                    var height = Mathf.PerlinNoise(x / noise,z / noise) * heightStrength;
                    var v = new Vector3(x, height, z);
                    vertices.Add(v);
                }
            }

            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    var i1 = x * (size + 1) + z;
                    var i2 = (x + 1) * (size + 1) + z;
                    var i3 = x * (size + 1) + z + 1;
                    var i4 = (x + 1) * (size + 1) + z + 1;
                    indices.Add(i1);
                    indices.Add(i3);
                    indices.Add(i2);
                    indices.Add(i2);
                    indices.Add(i3);
                    indices.Add(i4);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.UploadMeshData(false);
            var uvs = new Vector2[vertices.Count];
            var offset = 1.0f / size;
            for (var i = 0; i < vertices.Count; i++)
            {
                uvs[i] = new Vector2(vertices[i].x * offset, vertices[i].z * offset);
                Shader.SetGlobalVector("_uvs", uvs[i]);
            }

            mesh.uv = uvs;
            return mesh;
        }
    }
}