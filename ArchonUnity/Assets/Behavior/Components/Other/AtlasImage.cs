using Assets.Behavior.TransferTypes;
using UnityEngine;

public class AtlasImage : MonoBehaviour
{
    public AtlasTexture Texture
    {
        get => _texture;
        set
        {
            if (value == _texture)
                return;
            _texture = value;
            UpdateTexture();
        }
    }
    private AtlasTexture _texture;
    public Material materialPrototype;
    private Renderer r;
    private Material m;
    // Start is called before the first frame update
    void Start()
    {
        r = GetComponent<Renderer>();
        m = new Material(materialPrototype);
        r.material = m;
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        r.enabled = _texture.Texture != null;
        m.mainTexture = _texture.Texture;
        m.SetVector("_MainTex_ST", new Vector4(_texture.Rect.width, _texture.Rect.height, _texture.Rect.x, _texture.Rect.y));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
