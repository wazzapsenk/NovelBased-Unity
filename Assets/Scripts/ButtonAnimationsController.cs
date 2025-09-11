using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class ButtonAnimationsController : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler
{
    [SerializeField] private float scaleSize;
    [SerializeField] private float animationDuration;
    [SerializeField] private Ease ease;
    [SerializeField] private Transform buttonBackground;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }
    
    //TODO Tween oynatmadan önce "buttonBackground" üzerindeki oynayan tweenleri durdur.
    //TODO Buton sesleri için burayı kullanabiliriz. AudioController
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button.interactable) buttonBackground.DOScale(scaleSize, animationDuration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_button.interactable) buttonBackground.DOScale(1f, animationDuration).SetEase(ease);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_button.interactable) buttonBackground.DOScale(1f, animationDuration).SetEase(ease);
    }
}