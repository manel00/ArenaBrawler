using UnityEngine;
using UnityEngine.UIElements;

namespace ArenaEnhanced
{
    /// <summary>
    /// Generates a procedural violet gradient background with hexagon pattern
    /// for the welcome screen. Creates a texture at runtime.
    /// </summary>
    public class ProceduralBackground : MonoBehaviour
    {
        [Header("Gradient Settings")]
        [SerializeField] private Color centerColor = new Color(0.42f, 0.13f, 0.66f, 1f); // #6B21A8
        [SerializeField] private Color outerColor = new Color(0.06f, 0.04f, 0.18f, 1f); // #0F0518
        [SerializeField] private float gradientRadius = 0.7f;
        
        [Header("Hexagon Pattern")]
        [SerializeField] private bool enableHexagons = true;
        [SerializeField] private Color hexColor = new Color(0.66f, 0.33f, 0.97f, 0.08f); // #A855F7 with low alpha
        [SerializeField] private float hexSize = 40f;
        [SerializeField] private float hexSpacing = 1.2f;
        [SerializeField] private float hexLineWidth = 1.5f;
        
        [Header("Noise & Details")]
        [SerializeField] private bool enableNoise = true;
        [SerializeField] private float noiseIntensity = 0.03f;
        
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        
        private Texture2D _backgroundTexture;
        private VisualElement _backgroundLayer;
        
        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            GenerateBackground();
            ApplyToUI();
        }
        
        private void GenerateBackground()
        {
            int width = Screen.width;
            int height = Screen.height;
            
            // Create texture
            _backgroundTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _backgroundTexture.filterMode = FilterMode.Bilinear;
            _backgroundTexture.wrapMode = TextureWrapMode.Clamp;
            
            // Generate gradient
            GenerateRadialGradient(width, height);
            
            // Add hexagon pattern
            if (enableHexagons)
                DrawHexagonPattern(width, height);
            
            // Add noise
            if (enableNoise)
                AddNoise(width, height);
            
            _backgroundTexture.Apply();
        }
        
        private void GenerateRadialGradient(int width, int height)
        {
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float maxDistance = Mathf.Max(width, height) * gradientRadius;
            
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(distance / maxDistance);
                    
                    // Smooth falloff
                    t = Mathf.SmoothStep(0f, 1f, t);
                    
                    pixels[y * width + x] = Color.Lerp(centerColor, outerColor, t);
                }
            }
            
            _backgroundTexture.SetPixels(pixels);
        }
        
        private void DrawHexagonPattern(int width, int height)
        {
            // Hexagon geometry
            float hexWidth = hexSize * 2f;
            float hexHeight = hexSize * Mathf.Sqrt(3f);
            float xSpacing = hexWidth * 0.75f * hexSpacing;
            float ySpacing = hexHeight * hexSpacing;
            
            // Calculate grid
            int cols = Mathf.CeilToInt(width / xSpacing) + 2;
            int rows = Mathf.CeilToInt(height / ySpacing) + 2;
            
            // Draw each hexagon outline
            for (int row = -1; row < rows; row++)
            {
                for (int col = -1; col < cols; col++)
                {
                    float xOffset = (row % 2) * (xSpacing * 0.5f);
                    float centerX = col * xSpacing + xOffset;
                    float centerY = row * ySpacing;
                    
                    DrawHexagonOutline(centerX, centerY, hexSize, width, height);
                }
            }
        }
        
        private void DrawHexagonOutline(float cx, float cy, float size, int width, int height)
        {
            // Generate 6 vertices of hexagon
            Vector2[] vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f) * Mathf.Deg2Rad;
                vertices[i] = new Vector2(
                    cx + size * Mathf.Cos(angle),
                    cy + size * Mathf.Sin(angle)
                );
            }
            
            // Draw lines between vertices
            for (int i = 0; i < 6; i++)
            {
                Vector2 start = vertices[i];
                Vector2 end = vertices[(i + 1) % 6];
                DrawLine(start, end, width, height);
            }
        }
        
        private void DrawLine(Vector2 start, Vector2 end, int width, int height)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.CeilToInt(distance);
            
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // Draw with thickness
                for (int dy = -Mathf.CeilToInt(hexLineWidth); dy <= Mathf.CeilToInt(hexLineWidth); dy++)
                {
                    for (int dx = -Mathf.CeilToInt(hexLineWidth); dx <= Mathf.CeilToInt(hexLineWidth); dx++)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > hexLineWidth) continue;
                        
                        int px = Mathf.RoundToInt(point.x + dx);
                        int py = Mathf.RoundToInt(point.y + dy);
                        
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            float alpha = 1f - (dist / hexLineWidth);
                            Color existing = _backgroundTexture.GetPixel(px, py);
                            Color blended = Color.Lerp(existing, hexColor, hexColor.a * alpha);
                            _backgroundTexture.SetPixel(px, py, blended);
                        }
                    }
                }
            }
        }
        
        private void AddNoise(int width, int height)
        {
            System.Random rng = new System.Random(42); // Seed for consistency
            
            for (int y = 0; y < height; y += 2) // Skip every other pixel for performance
            {
                for (int x = 0; x < width; x += 2)
                {
                    float noise = ((float)rng.NextDouble() - 0.5f) * noiseIntensity;
                    
                    Color color = _backgroundTexture.GetPixel(x, y);
                    color.r = Mathf.Clamp01(color.r + noise);
                    color.g = Mathf.Clamp01(color.g + noise);
                    color.b = Mathf.Clamp01(color.b + noise);
                    
                    _backgroundTexture.SetPixel(x, y, color);
                    
                    // Fill neighboring pixels
                    if (x + 1 < width) _backgroundTexture.SetPixel(x + 1, y, color);
                    if (y + 1 < height) _backgroundTexture.SetPixel(x, y + 1, color);
                    if (x + 1 < width && y + 1 < height) _backgroundTexture.SetPixel(x + 1, y + 1, color);
                }
            }
        }
        
        private void ApplyToUI()
        {
            if (uiDocument == null || _backgroundTexture == null) return;
            
            var root = uiDocument.rootVisualElement;
            _backgroundLayer = root?.Q<VisualElement>("background-layer");
            
            if (_backgroundLayer != null)
            {
                // Create style background
                _backgroundLayer.style.backgroundImage = Background.FromTexture2D(_backgroundTexture);
                _backgroundLayer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            }
        }
        
        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }
    }
}
