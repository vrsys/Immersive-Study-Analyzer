using System.Collections;
using System.IO;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Button))]
public class RadialRenderer : MonoBehaviourPun, IPointerEnterHandler, IPointerExitHandler
{
    public Image _iconImage;
    private TextMeshProUGUI _text;

    private RadialMenuManager _manager;
    private RadialMenuItem _item;
    private Button _button;
    private RectTransform _itemRect;
    private RectTransform _rectTransform;
    private Image _image;
    private bool init = false;
    private bool textureInit = true;
    private int _itemId;
    private int _totalItemCount;
    private int _lastItemCount;
    private float _radius;
    private float _borderRadius;

    // Start is called before the first frame update
    void Start()
    {
    }

    public IEnumerator InitializeTextures(string dirPath, string filePath, int width, int height, float angleWidth, Vector2 CO)
    {
              
        Texture2D tex = null;
        
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            tex.LoadImage(fileData);
            yield return null;
        }

        if (tex == null)
        {
            tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float yPos = y - (width / 2);
                    float xPos = x - (height / 2);
                    Vector2 pos = new Vector2(xPos, yPos);
                    float distance = pos.magnitude;
                    Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                    if (distance <= _radius && distance >= _borderRadius)
                    {
                        float a = Mathf.Abs(Mathf.Atan2(CO.y * pos.x - CO.x * pos.y, CO.x * pos.x + CO.y * pos.y));
                        if (a <= angleWidth / 2.0f)
                        {
                            color = Color.white;
                            color.a = 1.0f;
                        }
                        
                    }

                    tex.SetPixel(x, y, color);
                }
                
                yield return null;
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            //tex.alphaIsTransparency = true;
            tex.Apply();
            
            byte[] bytes = tex.EncodeToPNG();
            if(!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(filePath, bytes);

            yield return null;
        }

        var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit: 100,
            extrude: 0, meshType: SpriteMeshType.FullRect);
        sprite.name = "RadialPart Sprite: " + _itemId;

        _image.name = "RadialPart Image: " + _itemId;
        _image.sprite = sprite;
        _image.color = _item.color;
        _image.alphaHitTestMinimumThreshold = 0.9f;
        textureInit = true;
    }
    
    public void Initialize(RadialMenuItem item, Material uiMaterial)
    {
        _item = item;
        _itemRect = _item.GetComponent<RectTransform>();
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _manager = _item.GetManager();
        _itemId = _item.GetItemId();
        _totalItemCount = _item.GetItemCount();

        if (uiMaterial != null)
        {
            _image.material = uiMaterial;
            //_text.material = uiMaterial;
            _iconImage.material = uiMaterial;
        }
        

        if (_item != null)
            init = true;

        _radius = _item.GetRadius();
        _borderRadius = _item.GetBorderRadius();

        _text.text = _item.GetDescription();
        _text.enabled = false;
        //_rectTransform.localPosition = -_itemRect.localPosition;

        float distanceInPix = Mathf.Sqrt(2 * _radius * _radius);
        Vector2 origin = new Vector2(0, distanceInPix);

        float angleWidth = _item.GetAngleWidth();
        float angle = _item.GetAngle();

        float xCO = Mathf.Cos(angle) * origin.x - Mathf.Sin(angle) * origin.y;
        float yCO = Mathf.Sin(angle) * origin.x + Mathf.Cos(angle) * origin.y;
        Vector2 CO = new Vector2(xCO, yCO);

        textureInit = false;
        _lastItemCount = _totalItemCount;
        
        int width = Mathf.CeilToInt(_radius * 2);
        int height = Mathf.CeilToInt(_radius * 2);

        string fileName = _radius.ToString("N4") + "_" + _borderRadius.ToString("N4") + "_" + angle.ToString("N4") + "_" + angleWidth.ToString("N4");
        var dirPath = Application.persistentDataPath + "/Textures/";
        var filePath = dirPath + fileName + ".png";

        _manager.AddCoroutine(InitializeTextures(dirPath, filePath, width, height, angleWidth, CO));

        _rectTransform.sizeDelta = Vector2.one * (2.0f * _radius);
        _rectTransform.localScale = Vector3.one;
        _rectTransform.localPosition = Vector3.zero;
        
        if(photonView != null && photonView.IsMine)
            _button.onClick.AddListener(() => _item.GetOnClickEvent().Invoke());
    }

    public void SetColor(Color color)
    {
        if(_image == null)
            _image = GetComponent<Image>();
        
        if(_image != null)
            _image.color = color;
    }

    public void SetButtonState(bool state)
    {
        if(_button != null)
            _button.interactable = state;
        else 
            Debug.LogError("Button is null!");
    }
    
    public void SetIcon(Sprite icon, float angle, int iconSize)
    {
        _iconImage.sprite = icon;

        float distanceInPix = _radius / 2 + _borderRadius / 2;
        Vector2 origin = new Vector2(0, distanceInPix);

        float x = Mathf.Cos(angle) * origin.x - Mathf.Sin(angle) * origin.y;
        float y = Mathf.Sin(angle) * origin.x + Mathf.Cos(angle) * origin.y;

        _iconImage.GetComponent<RectTransform>().localPosition = new Vector2(x, y);
        _iconImage.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
        if (_iconImage.transform.childCount > 0)
        {
            foreach (Transform t in _iconImage.transform)
            {
                t.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
            }
        }
    }

    public TextMeshProUGUI GetTextMesh()
    {
        return _text;
    }
    
    public void SetText(string t)
    {
        if(_text != null)
            _text.text = t;
    }

    public void SetTextState(bool state)
    {
        if(_text != null && _item.visualizeText)
            _text.enabled = state;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_text != null && _item.visualizeText)
            _text.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_text != null && _item.visualizeText)
            _text.enabled = false;
    }
}