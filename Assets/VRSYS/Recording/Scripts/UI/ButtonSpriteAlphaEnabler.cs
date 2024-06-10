using UnityEngine;
using UnityEngine.UI;

public class ButtonSpriteAlphaEnabler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().image.alphaHitTestMinimumThreshold = 0.3f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
