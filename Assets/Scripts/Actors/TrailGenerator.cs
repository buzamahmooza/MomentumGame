using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Actors
{
    public class TrailGenerator : MonoBehaviour
    {
//    [SerializeField] private GameObject trailInstanceTemplate;
        [SerializeField] public Color DefaultColor = new Color(0f, 1f, 1f, 0.3f);
        [SerializeField] [Range(0, 5)] public float SampleLifetime = 1;
        [SerializeField] [Range(0, 100)] float _trailFrequency = 40f;

        private Color m_color;
        private SpriteRenderer m_spriteRenderer;

        void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            m_color = DefaultColor;
            if (_trailFrequency <= 0) return;
            Invoke(nameof(CreateTrail), 1 / _trailFrequency);
        }

        void CreateTrail()
        {
            if (SampleLifetime > 0)
                CreateTrailInstance();
            Invoke(nameof(CreateTrail), 1 / _trailFrequency);
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
            trailSpriteRenderer.sprite = m_spriteRenderer.sprite;
            trailSpriteRenderer.color = m_color;
            return trailInstance;
        }
    }

    class TrailSample : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        public float SampleLifetime = 0.5f;
        private float _timer = 0;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            _timer += Time.deltaTime;

            _spriteRenderer.color = Color.Lerp(_spriteRenderer.color, Color.clear, _timer / SampleLifetime);
            if (_spriteRenderer.color.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}