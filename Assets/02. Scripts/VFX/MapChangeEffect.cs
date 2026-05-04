using System.Collections;
using Unity.Hierarchy;
using UnityEngine;

public class MapChangeEffect : MonoBehaviour
{
    [Header("Effect Properties")]
    [SerializeField]
    private float _transitionDuration = 0.5f;
    [SerializeField]
    private float _exitDropDistance = 8f;
    [SerializeField]
    private float _enterDropDistance = 8f;

    public void PlayExit(GameObject map)
    {
        StartCoroutine(AnimateExit(map, _transitionDuration));
    }

    public void PlayEnter(GameObject map, Vector3 finalPosition)
    {
        StartCoroutine(AnimateEnter(map, finalPosition, _transitionDuration));
    }

    private IEnumerator AnimateExit(GameObject map, float duration)
    {
        MapTransitionTarget target = new MapTransitionTarget(map);
        NotifyExitStart(target);

        Vector3 startPosition = map.transform.position;
        Vector3 endPosition = startPosition + Vector3.down * _exitDropDistance;
        target.SetTransparent(true);

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
            target.ApplyAlpha(1f - t);
            yield return null;
        }

        Destroy(map);
    }

    private IEnumerator AnimateEnter(GameObject map, Vector3 finalPosition, float duration)
    {
        MapTransitionTarget target = new MapTransitionTarget(map);
        NotifyEnterStart(target);

        Vector3 startPosition = finalPosition + Vector3.up * _enterDropDistance;
        map.transform.position = startPosition;

        target.SetTransparent(true);
        target.ApplyAlpha(0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (map == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            map.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
            target.ApplyAlpha(t);
            yield return null;
        }

        map.transform.position = finalPosition;

        target.ApplyAlpha(1f);
        target.SetTransparent(false);
        NotifyEnterEnd(target);
    }

    private void NotifyExitStart(MapTransitionTarget target)
    {
        IMapTransitionHandler[] handlers = target.Handlers;
        for (int i = 0; i < handlers.Length; i++)
        {
            handlers[i].OnMapExitStart();
        }
    }

    private void NotifyEnterStart(MapTransitionTarget target)
    {
        IMapTransitionHandler[] handlers = target.Handlers;
        for (int i = 0; i < handlers.Length; i++)
        {
            handlers[i].OnMapEnterStart();
        }
    }

    private void NotifyEnterEnd(MapTransitionTarget target)
    {
        IMapTransitionHandler[] handlers = target.Handlers;
        for (int i = 0; i < handlers.Length; i++)
        {
            handlers[i].OnMapEnterEnd();
        }
    }
}
