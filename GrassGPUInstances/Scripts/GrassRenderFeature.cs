using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace IKASAMADUS
{
    public class GrassRenderFeature : ScriptableRendererFeature
    {
        private GrassRenderPass _pass;
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.renderType == CameraRenderType.Base) renderer.EnqueuePass(_pass);
        }
        
        public override void Create()
        {
            _pass = new GrassRenderPass();
        }


        public class GrassRenderPass : ScriptableRenderPass
        {
            private const string NameOfCommandBuffer = "Grass";
            public GrassRenderPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get(NameOfCommandBuffer);
                try
                {
                    cmd.Clear();
                    var index = 0;
                    foreach (var grassTerrian in GrassTerrian.actives)
                    {
                        if (!grassTerrian) continue;
                        if (!grassTerrian.material) continue;
                        grassTerrian.UpdateMaterialProperties();
                        if(!SystemInfo.supportsInstancing) continue;
                        cmd.DrawMeshInstancedProcedural(GrassUtil.unitMesh, 0, grassTerrian.material, 0,
                            grassTerrian.grassCount, grassTerrian.materialPropertyBlock);
                        index++;
                    }

                    context.ExecuteCommandBuffer(cmd);
                }
                finally
                {
                    cmd.Release();
                }
            }
        }
    }
}