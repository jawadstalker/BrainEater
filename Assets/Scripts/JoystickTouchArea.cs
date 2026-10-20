
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class JoystickTouchArea : OnScreenControl,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField] private string controlPath =
        "<Gamepad>/leftStick";

    protected override string controlPathInternal
    {
        get => controlPath;
        set => controlPath = value;
    }

    [SerializeField] private RectTransform touchArea;
    [SerializeField] private RectTransform knob;
    [SerializeField] private float movementRadius = 50f;

    private Vector2 input;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchArea, eventData.position,
            eventData.pressEventCamera, out Vector2 localPoint))
            return;

        // Offset from the center of the touch area.
        Vector2 offset = localPoint - touchArea.rect.center;

        // Clamp knob movement to the joystick radius.
        input = Vector2.ClampMagnitude(
            offset / movementRadius, 1f);

        // Move the visible knob.
        knob.anchoredPosition =
            input * movementRadius;

        // Send the Vector2 to the Input System.
        SendValueToControl(input);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
        SendValueToControl(input);
    }
}
