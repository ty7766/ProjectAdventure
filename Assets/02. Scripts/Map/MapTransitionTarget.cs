using UnityEngine;

public class MapTransitionTarget
{
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int SurfaceID = Shader.PropertyToID("_Surface");
    private static readonly int BlendID = Shader.PropertyToID("_Blend");
    private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
    private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
    private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";

    private readonly Material[] _materials;
    private readonly Color[] _baseColors;
    private readonly bool[] _hasBaseColor;

    public IMapTransitionHandler[] Handlers { get; }

    public MapTransitionTarget(GameObject map)
    {
        Handlers = map.GetComponentsInChildren<IMapTransitionHandler>(true);

        Renderer[] renderers = map.GetComponentsInChildren<Renderer>(true);
        int total = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            total += renderers[i].sharedMaterials.Length;
        }

        _materials = new Material[total];
        _baseColors = new Color[total];
        _hasBaseColor = new bool[total];

        int wirteIndex = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] instanceMats = renderers[i].materials;
            for (int j = 0; j < instanceMats.Length; j++)
            {
                Material mat = instanceMats[j];
                _materials[wirteIndex] = mat;

                bool has = mat.HasProperty(BaseColorID);
                _hasBaseColor[wirteIndex] = has;

                if (has)
                {
                    Color c = mat.GetColor(BaseColorID);
                    c.a = 1f;
                    _baseColors[wirteIndex] = c;
                }
                wirteIndex++;
            }
        }
    }

    /// <summary>
    /// URP Lit 셰이더의 Surface 타입을 토글
    /// </summary>
    /// <param name="materials"></param>
    /// <param name="isTransparent"></param>
    public void SetTransparent(bool isTransparent)
    {
        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (!mat.HasProperty(SurfaceID))
            {
                continue;
            }

            if (isTransparent)
            {
                mat.SetFloat(SurfaceID, 1f);
                mat.SetFloat(BlendID, 0f);
                mat.EnableKeyword(TransparentKeyword);
                mat.renderQueue = 3000;
                mat.SetFloat(ZWriteID, 1f);
                mat.SetFloat(SrcBlendID, 5f);
                mat.SetFloat(DstBlendID, 10f);
            }
            else
            {
                mat.SetFloat(SurfaceID, 0f);
                mat.DisableKeyword(TransparentKeyword);
                mat.renderQueue = 2000;
                mat.SetFloat(ZWriteID, 1f);
                mat.SetFloat(SrcBlendID, 1f);
                mat.SetFloat(DstBlendID, 0f);
                if (mat.HasProperty(BaseColorID))
                {
                    Color c = mat.GetColor(BaseColorID);
                    c.a = 1f;
                    mat.SetColor(BaseColorID, c);
                }
            }
        }     
    }

    /// <summary>
    /// 캐시된 BaseColor에 알파만 적용해 Material에 다시 씀
    /// </summary>
    /// <param name="alpha"></param>
    public void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < _materials.Length; i++)
        {
            if (!_hasBaseColor[i])
            {
                continue;
            }

            Color c = _baseColors[i];
            c.a = alpha;
            _materials[i].SetColor(BaseColorID, c);
        }
    }
}
