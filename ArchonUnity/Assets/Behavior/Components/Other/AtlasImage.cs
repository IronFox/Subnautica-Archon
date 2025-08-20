using Assets.Behavior.TransferTypes;
using Behavior.Util.Log;
using UnityEngine;

public class AtlasImage : MonoBehaviour
{
    public Sprite Texture
    {
        get => _texture;
        set
        {
            if (value == _texture)
                return;
            using (var log = new LogContext(nameof(AtlasImage)+'.'+nameof(Texture), value ? value.texture.NiceName() : "null"))
            {
                _texture = value;
                UpdateTexture();
            }
        }
    }
    private Sprite _texture;
    public Material materialPrototype;
    private Renderer r;
    private Renderer Renderer
    {
        get
        {
            if (!r)
            {
                r = GetComponent<Renderer>();
                r.material = Material;
            }

            return r;
        }
    }
    private Material Material
    {
        get
        {
            if (!m)
            {
                using (var log = new LogContext(nameof(AtlasImage) + '.' + nameof(Material)))
                {
                    log.Write($"Instantiating new material from {materialPrototype.NiceName()}");
                    m = new Material(materialPrototype);
                    Renderer.material = m;
                }
            }

            return m;
        }
    }
    private Material m;
    // Start is called before the first frame update
    void Awake()
    {
        using (var log = new LogContext(nameof(AtlasImage) + '.' + nameof(Awake), this.NiceName()))
        {
            _ = Renderer;
            UpdateTexture();
        }
    }

    private void UpdateTexture()
    {
        using (var log = new LogContext(nameof(AtlasImage)+'.'+nameof(UpdateTexture), this.NiceName()))
        {
            if (_texture)
                log.Write($"Assigning texture {_texture.texture.NiceName()}");
            else
                log.Write($"Assigning null texture");
            Renderer.enabled = _texture && _texture.texture;
            log.Write($"Renderer.enabled = {Renderer.enabled}, Updating material {Renderer.material.NiceName()}");
            if (_texture && _texture.texture)
            {
                Renderer.material.mainTexture = _texture.texture;
                log.Write($"Decoding aspectRatio from {_texture.rect} (texture size is {_texture.texture.width}*{_texture.texture.height})");
                float aspectRatio = (float)_texture.rect.width / _texture.rect.height;
                var st = new Vector4(
                    _texture.rect.width / _texture.texture.width,
                    _texture.rect.height / _texture.texture.height,
                    _texture.rect.x / _texture.texture.width,
                    _texture.rect.y / _texture.texture.height
                );
                log.Write($"Set texture scale to {st}");
                Renderer.material.SetVector("_MainTex_ST", st);
                if (aspectRatio < 1)
                    transform.localScale = new Vector3(aspectRatio, 1, 1);
                else if (aspectRatio > 1)
                    transform.localScale = new Vector3(1, 1 / aspectRatio, 1);
                else
                    transform.localScale = Vector3.one; // Square texture
                log.Write($"Set scale to {transform.localScale}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
