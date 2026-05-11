using System.Collections;
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

        yield return FadeMove(map, target, startPosition, endPosition, 1f, 0f, duration);

        if(map != null)
            Destroy(map);
    }

    private IEnumerator AnimateEnter(GameObject map, Vector3 endPosition, float duration)
    {
        MapTransitionTarget target = new MapTransitionTarget(map);
        NotifyEnterStart(target);

        Vector3 startPosition = endPosition + Vector3.up * _enterDropDistance;
        map.transform.position = startPosition;

        target.SetTransparent(true);
        target.ApplyAlpha(0f);

        yield return FadeMove(map, target, startPosition, endPosition, 0f, 1f, duration);

        if (map == null)
            yield break;

        map.transform.position = endPosition;

        target.ApplyAlpha(1f);
        target.SetTransparent(false);
        NotifyEnterEnd(target);
    }

    //위치와 알파를 보간하는 공통 페이드 루프, map이 파괴되면 즉시 종료
    private IEnumerator FadeMove(
        GameObject map,
        MapTransitionTarget target,
        Vector3 startPosition,
        Vector3 endPosition,
        float startAlpha,
        float endAlpha,
        float duration)
    {
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
            target.ApplyAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
            yield return null;
        }
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
