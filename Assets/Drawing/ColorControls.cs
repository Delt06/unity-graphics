using UnityEngine;
using UnityEngine.UI;

namespace Drawing
{
    public class ColorControls : MonoBehaviour
    {
        [SerializeField] private Image _preview;
        [SerializeField] private DrawingCanvas _drawingCanvas;
        [SerializeField] private Slider _r;
        [SerializeField] private Slider _g;
        [SerializeField] private Slider _b;
        [SerializeField] private Slider _a;

        private Color _color;

        public float R
        {
            get => _color.r;
            set
            {
                _color.r = value;
                UpdateColor();
            }
        }

        public float G
        {
            get => _color.g;
            set
            {
                _color.g = value;
                UpdateColor();
            }
        }

        public float B
        {
            get => _color.b;
            set
            {
                _color.b = value;
                UpdateColor();
            }
        }

        public float A
        {
            get => _color.a;
            set
            {
                _color.a = value;
                UpdateColor();
            }
        }

        private void Awake()
        {
            _color = _drawingCanvas.Color;
            _r.value = R;
            _g.value = G;
            _b.value = B;
            _a.value = A;
            _r.onValueChanged.AddListener(OnSliderChangedR);
            _g.onValueChanged.AddListener(OnSliderChangedG);
            _b.onValueChanged.AddListener(OnSliderChangedB);
            _a.onValueChanged.AddListener(OnSliderChangedA);
            UpdateColor();
        }

        private void OnDestroy()
        {
            _r.onValueChanged.RemoveListener(OnSliderChangedR);
            _g.onValueChanged.RemoveListener(OnSliderChangedG);
            _b.onValueChanged.RemoveListener(OnSliderChangedB);
            _a.onValueChanged.RemoveListener(OnSliderChangedA);
        }

        private void OnSliderChangedB(float arg0)
        {
            B = arg0;
        }

        private void OnSliderChangedA(float arg0)
        {
            A = arg0;
        }

        private void OnSliderChangedG(float arg0)
        {
            G = arg0;
        }

        private void OnSliderChangedR(float arg0)
        {
            R = arg0;
        }

        private void UpdateColor()
        {
            _drawingCanvas.Color = _color;
            _preview.color = _color;
        }
    }
}