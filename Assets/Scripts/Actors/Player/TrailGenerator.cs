using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailGenerator : MonoBehaviour
{
//    [SerializeField] private GameObject trailInstanceTemplate;
    [SerializeField] public Color DefaultColor = new Color(0f, 1f, 1f, 0.3f);
    private Color _color;
    [SerializeField] [Range(0, 5)] public float SampleLifetime = 1;
    [SerializeField] [Range(0, 100)] float _trailFrequency = 40f;

    private SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _color = DefaultColor;
        if (_trailFrequency <= 0) return;
        Invoke("CreateTrail", 1 / _trailFrequency);
    }

    void CreateTrail()
    {
        if (SampleLifetime > 0)
            CreateTrailInstance();
        Invoke("CreateTrail", 1 / _trailFrequency);
    }


    private GameObject GetTrailPrefab()
    {
        GameObject go = new GameObject("Trail Instance", typeof(TrailSample));
        TrailSample trailSample = go.GetComponent<TrailSample>();
        trailSample.SampleLifetime = this.SampleLifetime;
        
        return go;
    }

    /// <summary>
    /// creates and returns a trail instance
    /// </summary>
    /// <returns></returns>
    public GameObject CreateTrailInstance()
    {
        GameObject trailInstance = Instantiate(GetTrailPrefab(), transform.position, transform.rotation);
        trailInstance.transform.localScale = this.transform.localScale;

        SpriteRenderer trailSpriteRenderer = trailInstance.GetComponent<SpriteRenderer>();
        trailSpriteRenderer.sprite = _spriteRenderer.sprite;
        trailSpriteRenderer.color = _color;
        return trailInstance;
    }
}