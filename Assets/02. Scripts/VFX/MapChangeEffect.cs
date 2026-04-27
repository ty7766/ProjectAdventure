using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChangeEffect : MonoBehaviour
{
    [Header("전환 애니메이션 설정")]
    [SerializeField]
    private float _transitionDuration = 0.5f;
    [SerializeField]
    private float _exitDropDistance = 8f;
    [SerializeField]
    private float _enterDropDistance = 8f;

    public float EnterDropDistance => _enterDropDistance;

    public void PlayExit(GameObject map)
    {
        StartCoroutine(AnimateExit(map, _transitionDuration));
    }

    public void PlayEnter(GameObject map, Vector3 startPosition, Vector3 endPosition)
    {
        StartCoroutine(AnimateEnter(map, startPosition, endPosition, _transitionDuration));
    }

    private IEnumerator AnimateExit(GameObject map, float duration)
    {
        foreach (var handler in map.GetComponentsInChildren<IMapTransitionHandler>())
            handler.OnMapExitStart();

        Vector3 startPosition = map.transform.position;
        Vector3 endPosition = startPosition + Vector3.down * _exitDropDistance;
        Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
        Material[] materials = CollectMaterials(renderers);
        SetMaterialsTransparent(materials, true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (map == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            map.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            SetRenderersAlpha(materials, 1f - t);
            yield return null;
        }

        Destroy(map);
    }

    private IEnumerator AnimateEnter(GameObject map, Vector3 startPosition, Vector3 endPosition, float duration)
    {
        foreach (var handler in map.GetComponentsInChildren<IMapTransitionHandler>())
            handler.OnMapEnterStart();

        Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
        Material[] materials = CollectMaterials(renderers);
        SetMaterialsTransparent(materials, true);
        SetRenderersAlpha(materials, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (map == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            map.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            SetRenderersAlpha(materials, t);
            yield return null;
        }

        map.transform.position = endPosition;
        SetRenderersAlpha(materials, 1f);
        SetMaterialsTransparent(materials, false);

        foreach (var handler in map.GetComponentsInChildren<IMapTransitionHandler>())
            handler.OnMapEnterEnd();
    }

    private Material[] CollectMaterials(Renderer[] renderers)
    {
        var list = new List<Material>();
        foreach (var renderer in renderers)
        {
            list.AddRange(renderer.materials);
        }
        return list.ToArray();
    }

    private void SetMaterialsTransparent(Material[] materials, bool isTransparent)
    {
        foreach (var mat in materials)
        {
            if (!mat.HasProperty("_Surface"))
            {
                continue;
            }

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
                mat.SetFloat("_ZWrite", 1f);
                mat.SetFloat("_SrcBlend", 5f);
                mat.SetFloat("_DstBlend", 10f);
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 2000;
                mat.SetFloat("_ZWrite", 1f);
                mat.SetFloat("_SrcBlend", 1f);
                mat.SetFloat("_DstBlend", 0f);
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = 1f;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    private void SetRenderersAlpha(Material[] materials, float alpha)
    {
        foreach (var mat in materials)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
        }
    }
}
