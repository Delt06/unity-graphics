using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Drawing
{
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class DrawingCanvas : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [SerializeField] [Min(1)] private int _resolution = 1024;
        [SerializeField] private RenderTextureFormat _format = RenderTextureFormat.Default;
        [SerializeField] private Material _material;
        [SerializeField] [Min(1)] private float _brushSize = 50;
        [SerializeField] private Color _color = Color.red;
        [SerializeField] [Range(0f, 1f)] private float _hardness;

        private RawImage _rawImage;
        private RectTransform _rectTransform;
        private RenderTexture _rt;

        public float Hardness
        {
            get => _hardness;
            set => _hardness = value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public float NormalizedBrushSize
        {
            get => _brushSize / _resolution;
            set => _brushSize = _resolution * value;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rawImage = GetComponent<RawImage>();

            _rt = new RenderTexture(_resolution, _resolution, 0, _format)
            {
                wrapMode = TextureWrapMode.Clamp,
            };
            _rawImage.texture = _rt;
            Clear();
        }

        private void OnDestroy()
        {
            if (!_rt) return;

            Destroy(_rt);
            _rt = null;
        }

        public void OnDrag(PointerEventData eventData) => TryDraw(eventData);

        public void OnPointerDown(PointerEventData eventData) => TryDraw(eventData);

        public void Clear()
        {
            var currentRt = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.Clear(false, true, Color.white);
            RenderTexture.active = currentRt;
        }

        private void TryDraw(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint
                )) return;

            float2 rectHalfSize = _rectTransform.rect.size * 0.5f;
            var uv = math.unlerp(-rectHalfSize, rectHalfSize, localPoint);

            var activeRt = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.PushMatrix();

            _material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);


            var halfBrushSizeNormalized = 0.5f * NormalizedBrushSize;
            GL.Color(_color);
            GL.TexCoord3(0, 0, _hardness);
            GlVertex(uv + new float2(-halfBrushSizeNormalized, -halfBrushSizeNormalized));
            GL.TexCoord3(0, 1, _hardness);
            GlVertex(uv + new float2(-halfBrushSizeNormalized, halfBrushSizeNormalized));
            GL.TexCoord3(1, 1, _hardness);
            GlVertex(uv + new float2(halfBrushSizeNormalized, halfBrushSizeNormalized));
            GL.TexCoord3(1, 0, _hardness);
            GlVertex(uv + new float2(halfBrushSizeNormalized, -halfBrushSizeNormalized));

            GL.End();

            GL.PopMatrix();
            RenderTexture.active = activeRt;
        }

        private static void GlVertex(float2 vertex)
        {
            GL.Vertex(new Vector3(vertex.x, vertex.y));
        }
    }
}