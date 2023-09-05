using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IKASAMADUS
{
    [ExecuteInEditMode]
    public class GrassTerrian : MonoBehaviour
    {
        private static readonly HashSet<GrassTerrian> _actives = new HashSet<GrassTerrian>();
        [Header("草的材质")] [SerializeField]
        private Material _material;
        [Header("主纹理")][SerializeField]
        private Texture2D _grassTexture = null;
        [Header("使用主纹理着色")] [SerializeField]
        private bool _UseBaseMap;
        [Header("基础色")][ColorUsageAttribute(true, true)][SerializeField]
        private Color _baseColor = new Color(0.1015625f, 0.6484375f, 0.18359375f, 1f);
        [Header("偏移色")][ColorUsageAttribute(true, true)][SerializeField]
        private Color _secondColor = new Color(0.1015625f, 0.6484375f, 0.18359375f, 1f);
        [Header("底部色")][SerializeField]
        private Color _groundColor = new Color(0f,0.26171875f,0.05078125f,1f);
        [Header("菲尼尔强度")] [SerializeField][Range(0,1)]
        private float _translucencyIntensity = 0.5f;
        
        [Header("透明裁剪")][SerializeField][Range(0,1)]
        private float _Cutoff = 0.5f;
        [Header("颜色偏移范围")] [SerializeField][Range(-1,1)]
        private float _Offset = 0.15f;
        [Header("偏移色渐变")][SerializeField][Range(-1,1)]
        private float lerp = 0.5f;
        [Header("颜色偏移位置")] [SerializeField]
        private float _WorldScale = 1f;
        
        [Header("风力参数,xyz偏移方向,w强度")] [SerializeField]
        private Vector4 _windParameters = new Vector4(1f,0f,1f,2f);
        
        [Header("风力噪声强度")] [SerializeField]
        private float _windNoiseScale = 4.8f;
        [Header("风浪开关")] [SerializeField]
        private bool _stormToggle;
        [Header("风浪参数,x开始,y持续,z结束,w间隔")] [SerializeField]
        private Vector4 _stromParameters = new Vector4(1,100,40,100);
        [Header("风浪强度")] [SerializeField]
        private float _stromStrength = 10f;
        [Header("草的大小;xy,z(坡度)")] [SerializeField]
        private Vector3 _grassQuadSize = new(1f, 1.3f, 1f);
        [Header("高度变化")] [SerializeField]
        private float _heightNoiseScale = 0f;
        [Header("生长进度条")] [Range(0, 1f)] [SerializeField]
        private float _grow = 1f;

        [Header("改变生长方向")] [SerializeField] public bool _growDir;

        //[SerializeField] private bool _invert;
        [Header("单位面积中草的数量")] [SerializeField] private int _grassCountPerMeter = 100;

        private ComputeBuffer _grassBuffer;

        private MaterialPropertyBlock _materialBlock;

        private int _seed;

        public static IReadOnlyCollection<GrassTerrian> actives{
            get{
                return _actives;
            }
        }

        public Material material => _material;
        
        private int _grassCount;
        
        private bool supInstanceing;

        public int grassCount{
            get{
                return _grassCount;
            }
        }
         public ComputeBuffer grassBuffer{
            get{
                if(_grassBuffer != null){
                    return _grassBuffer;
                }
                var filter = GetComponent<MeshFilter>();
                var terrianMesh = filter.sharedMesh;
                var matrix = transform.localToWorldMatrix;
                var grassIndex = 0;
                List<GrassInfo> grassInfos = new List<GrassInfo>();
                var maxGrassCount = 10000;
                Random.InitState(_seed);

                var indices = terrianMesh.triangles;
                var vertices = terrianMesh.vertices;

                for(var j = 0; j < indices.Length / 3; j ++){
                    var index1 = indices[j * 3];
                    var index2 = indices[j * 3 + 1];
                    var index3 = indices[j * 3 + 2];
                    var v1 = vertices[index1];
                    var v2 = vertices[index2];
                    var v3 = vertices[index3];

                    //面得到法向
                    var normal = GrassUtil.GetFaceNormal(v1,v2,v3);

                    //计算up到faceNormal的旋转四元数
                    var upToNormal = Quaternion.FromToRotation(Vector3.up,normal);

                    //三角面积
                    var arena = GrassUtil.GetAreaOfTriangle(v1,v2,v3);

                    //计算在该三角面中，需要种植的数量
                    var countPerTriangle = Mathf.Max(1,_grassCountPerMeter * arena);

                    for(var i = 0; i < countPerTriangle; i ++){
                        
                        var positionInTerrian = GrassUtil.RandomPointInsideTriangle(v1,v2,v3);
                        float rot = Random.Range(0,180);
                        var localToTerrian = Matrix4x4.TRS(positionInTerrian,  upToNormal * Quaternion.Euler(0,rot,0) ,Vector3.one);

                        Vector2 texScale = Vector2.one;
                        Vector2 texOffset = Vector2.zero;
                        Vector4 texParams = new Vector4(texScale.x,texScale.y,texOffset.x,texOffset.y);
                        

                        var grassInfo = new GrassInfo(){
                            localToTerrian = localToTerrian,
                            texParams = texParams
                        };
                        grassInfos.Add(grassInfo);
                        grassIndex ++;
                        if(grassIndex >= maxGrassCount){
                            break;
                        }
                    }
                    if(grassIndex >= maxGrassCount){
                        break;
                    }
                }
               
                _grassCount = grassIndex;
                _grassBuffer = new ComputeBuffer(_grassCount,64 + 16);
                _grassBuffer.SetData(grassInfos);
                return _grassBuffer;
            }
        }

         public MaterialPropertyBlock materialPropertyBlock{
             get{
                 if(_materialBlock == null){
                     _materialBlock = new MaterialPropertyBlock();
                 }
                 return _materialBlock;
             }
         }

         [ContextMenu("ForceRebuildGrassInfoBuffer")]
         private void ForceUpdateGrassBuffer(){
             if(_grassBuffer != null){
                 _grassBuffer.Dispose();
                 _grassBuffer = null;
             }
             UpdateMaterialProperties();
         }
         

         void OnEnable(){
             _actives.Add(this);
             
             //_material = CoreUtils.CreateEngineMaterial("TA Shaders/Grass");
         }

         void OnDisable(){
             _actives.Remove(this);
             if(_grassBuffer != null){
                 _grassBuffer.Dispose();
                 _grassBuffer = null;
             }
         }


         public struct GrassInfo{
             public Matrix4x4 localToTerrian;
             public Vector4 texParams;
         }
        
        public void UpdateMaterialProperties()
        {
            materialPropertyBlock.SetMatrix(ShaderProperties.TerrianLocalToWorld, transform.localToWorldMatrix);
            materialPropertyBlock.SetBuffer(ShaderProperties.GrassInfos, grassBuffer);
            
            materialPropertyBlock.SetVector(ShaderProperties.GrassQuadSize, _grassQuadSize);
            materialPropertyBlock.SetFloat(ShaderProperties.Fade, 1 -_grow);
            materialPropertyBlock.SetColor(ShaderProperties.BaseColor, _baseColor);
            materialPropertyBlock.SetColor(ShaderProperties.GroundColor, _groundColor);
            materialPropertyBlock.SetFloat(ShaderProperties.CutOff,_Cutoff);
            materialPropertyBlock.SetVector(ShaderProperties.WindParameters,_windParameters);
            materialPropertyBlock.SetFloat(ShaderProperties.WindNoiseScale,_windNoiseScale);
            materialPropertyBlock.SetFloat(ShaderProperties.GrassDirToggle,_growDir ? 1 : 0);
            materialPropertyBlock.SetFloat(ShaderProperties.UseBaseMap,_UseBaseMap ? 1 : 0);
            materialPropertyBlock.SetColor(ShaderProperties.SencondColor,_secondColor);
            materialPropertyBlock.SetFloat(ShaderProperties.Offset,_Offset);
            materialPropertyBlock.SetFloat(ShaderProperties.Lerp,lerp);
            materialPropertyBlock.SetFloat(ShaderProperties.WorldScale,_WorldScale);
            materialPropertyBlock.SetFloat(ShaderProperties.TranslucencyIntensity,_translucencyIntensity);
            materialPropertyBlock.SetFloat(ShaderProperties.HeightNoiseScale,_heightNoiseScale);
            
            materialPropertyBlock.SetFloat(ShaderProperties.StromToggle,_stormToggle ? 1 : 0);
            materialPropertyBlock.SetVector(ShaderProperties.StromParameters,_stromParameters);
            materialPropertyBlock.SetFloat(ShaderProperties.StromStrength,_stromStrength);
            material.mainTexture = _grassTexture;
        }
        
        private class ShaderProperties
        {
            public static readonly int TerrianLocalToWorld = Shader.PropertyToID("_TerrianLocalToWorld");
            public static readonly int GrassInfos = Shader.PropertyToID("_GrassInfos");
            public static readonly int GrassQuadSize = Shader.PropertyToID("_GrassQuadSize");
            public static readonly int Fade = Shader.PropertyToID("_Fade");
            public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int GroundColor = Shader.PropertyToID("_GroundColor");
            public static readonly int CutOff = Shader.PropertyToID("_Cutoff");
            public static readonly int WindParameters = Shader.PropertyToID("_Wind");
            public static readonly int WindNoiseScale = Shader.PropertyToID("_WindNoiseStrength");
            public static readonly int GrassDirToggle = Shader.PropertyToID("_GrowDirToggle");
            public static readonly int UseBaseMap = Shader.PropertyToID("_UseBaseMap");
            
            public static readonly int SencondColor = Shader.PropertyToID("_SecondColor");
            public static readonly int Offset = Shader.PropertyToID("_SecondColorOffset");
            public static readonly int Lerp = Shader.PropertyToID("_SecondColorFade");
            public static readonly int WorldScale = Shader.PropertyToID("_WorldScale");
            public static readonly int TranslucencyIntensity = Shader.PropertyToID("_TranslucencyInt");
            public static readonly int HeightNoiseScale = Shader.PropertyToID("_Height");
            
            public static readonly int StromToggle = Shader.PropertyToID("_StormToggle");
            public static readonly int StromParameters = Shader.PropertyToID("_StormParams");
            public static readonly int StromStrength = Shader.PropertyToID("_StormStrength");
        }
    }
}