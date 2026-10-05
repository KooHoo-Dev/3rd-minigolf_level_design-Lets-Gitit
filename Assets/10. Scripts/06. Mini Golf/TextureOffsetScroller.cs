using UnityEngine;

namespace Study3D.MiniGolf
{
    public class TextureOffsetScroller : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0f, -1.5f);
        public string textureProperty = "_BaseMap";

        private Material material;
        private Vector2 offset;
        private int propertyId;

        private void Awake()
        {
            material = GetComponent<Renderer>().material;
            propertyId = Shader.PropertyToID(textureProperty);
            offset = material.GetTextureOffset(propertyId);
        }

        private void Update()
        {
            offset += speed * Time.deltaTime;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);
            material.SetTextureOffset(propertyId, offset);
        }

        private void OnDestroy()
        {
            Destroy(material);
        }
    }
}
