using TMPro;
using UnityEngine;

public class ConsoleTextFade : MonoBehaviour, ILightListener
{
    public AnimationController sourceAnimation;
    public Vector2[] visibilityNodes;
    private TextMeshPro textMeshPro;
    public TranslationCode translationCode;
    // Start is called before the first frame update
    void Start()
    {
        textMeshPro = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!textMeshPro)
            return;
        textMeshPro.text = TranslationAdapter.GetTranslation(translationCode);
        if (!sourceAnimation || visibilityNodes == null)
            return;
        float progress = sourceAnimation.Progress;
        for (int i = 0; i + 1 < visibilityNodes.Length; i++)
        {
            if (progress >= visibilityNodes[i].x && progress <= visibilityNodes[i + 1].x)
            {
                float t = (progress - visibilityNodes[i].x) / (visibilityNodes[i + 1].x - visibilityNodes[i].x);
                float alpha = Mathf.Lerp(visibilityNodes[i].y, visibilityNodes[i + 1].y, t);
                {
                    Color color = textMeshPro.color;
                    color.a = alpha;
                    textMeshPro.color = color;
                }
                break;
            }
        }
    }

    public void SetInteriorLight(Color lightColor, Color stripColor, int minimumInteriorLightPriority)
    {
        if (!textMeshPro)
            return;
        textMeshPro.color = new Color(lightColor.r, lightColor.g, lightColor.b, textMeshPro.color.a);
    }
}
