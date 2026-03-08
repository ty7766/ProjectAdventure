using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class GraphicManager : Singleton<GraphicManager>
{
    private Volume _volume;

    public Volume Volume => _volume;

    protected override void Awake()
    {
        base.Awake();
        if(Instance != this)
        {
            return;
        }

        _volume = GetComponent<Volume>();
    }
}
