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
    private Renderer Renderer
    {
        get
        {
            if (r == null)
                r = GetComponent<Renderer>();
            return r;
        }
    }
    private Material Material
    {
        get
        {
            if (m == null)
                m = new Material(materialPrototype);
            return m;
        }
    }
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
        Renderer.enabled = _texture.Exists;
        Material.mainTexture = _texture.Texture;
        float aspectRatio = _texture.AspectRatio;
        Material.SetVector("_MainTex_ST", new Vector4(_texture.Rect.width, _texture.Rect.height, _texture.Rect.x, _texture.Rect.y));
        if (aspectRatio < 1)
            transform.localScale = new Vector3(aspectRatio, 1, 1);
        else if (aspectRatio > 1)
            transform.localScale = new Vector3(1, 1 / aspectRatio, 1);
        else
            transform.localScale = Vector3.one; // Square texture
    }

    // Update is called once per frame
    void Update()
    {

    }
}
