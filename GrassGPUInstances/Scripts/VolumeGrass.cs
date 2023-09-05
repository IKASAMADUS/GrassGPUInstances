using System;
using System.Collections.Generic;
using IKASAMADUS;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

[VolumeComponentMenu("Custom Post-Processing/Grass")]
public sealed class VolumeGrass : CustomVolumeComponent
{
    public ColorParameter baseColor = new ColorParameter(Color.white, false, false, true);
    public ColorParameter groundColor = new ColorParameter(Color.white, false, false, true);
    public ClampedFloatParameter randomNormal = new ClampedFloatParameter(0.15f, -1, 1);
    public Vector3Parameter grassQuadSize = new Vector3Parameter(new Vector3(1, 1.3f, 1));
    public ClampedFloatParameter grow = new ClampedFloatParameter(1, 0, 1);
    public BoolParameter growDir = new BoolParameter(false);
    public ClampedIntParameter grassCountPerMeter = new ClampedIntParameter(0, 0, 1000);

    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterOpaqueAndSky;

    Material _material;
    string _shaderName = "TA Shaders/Grass";
    public override void Setup()
    {
        if (_material == null)
            CoreUtils.CreateEngineMaterial(_shaderName);
        
        // baseColor.value = GrassTerrian.GetInstance()._baseColor;
        // groundColor.value = GrassTerrian.GetInstance()._groundColor;
        // randomNormal.value = GrassTerrian.GetInstance()._randomNormal;
        // grassQuadSize.value = GrassTerrian.GetInstance()._grassQuadSize;
        // grow.value = GrassTerrian.GetInstance()._grow;
        // growDir.value = GrassTerrian.GetInstance()._growDir;
        // grassCountPerMeter.value = GrassTerrian.GetInstance()._grassCountPerMeter;
    }

    public override bool IsActive() => grassCountPerMeter.value > 0;

    

    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source,
        RenderTargetIdentifier destination)
    {
        try
        {
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
        }
        finally
        {
            
        }
        
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_material);
    }
    
}