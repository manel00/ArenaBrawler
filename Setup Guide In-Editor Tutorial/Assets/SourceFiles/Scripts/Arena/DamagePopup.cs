using UnityEngine;
using TMPro;

namespace ArenaEnhanced
{
    public class DamagePopup : MonoBehaviour
    {
        private TextMeshPro _textMesh;
        private float _fadeTimer = 1f;
        private Color _startColor;
        private Vector3 _moveVector;

        public static void Create(Vector3 position, float damage)
        {
            GameObject go = new GameObject("DamagePopup");
            go.transform.position = position;
            var popup = go.AddComponent<DamagePopup>();
            popup.Setup(damage);
        }

        private void Awake()
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontSize = 4f;
            _textMesh.sortingOrder = 500; // Ensure it's on top
        }

        public void Setup(float damage)
        {
            // Show damage with 1 decimal place for small values, rounded for larger
            if (damage < 1f && damage > 0)
                _textMesh.text = damage.ToString("F1");  // Shows "0.5"
            else
                _textMesh.text = Mathf.Round(damage).ToString();
            
            // Color based on damage amount (whiter for small, redder for large)
            _startColor = Color.Lerp(Color.white, Color.red, damage / 50f);
            _textMesh.color = _startColor;
            
            _moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0) * 2f;
        }

        private void Update()
        {
            transform.position += _moveVector * Time.deltaTime;
            _moveVector -= _moveVector * 1.5f * Time.deltaTime; // Drag

            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                var color = _textMesh.color;
                color.a = _fadeTimer;
                _textMesh.color = color;
                
                // Scale slightly
                transform.localScale = Vector3.one * (1f + (1f - _fadeTimer) * 0.5f);
            }

            // Face Camera
            if (CameraCache.Main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - CameraCache.Main.transform.position);
            }
        }
    }
}
