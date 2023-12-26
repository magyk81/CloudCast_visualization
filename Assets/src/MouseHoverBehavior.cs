using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHoverBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private bool hovered = false;

    public bool Hovered {
        get => hovered;
        private set { hovered = value; }
    }

    public void OnPointerEnter(PointerEventData eventData) { Hovered = true; }

    public void OnPointerExit(PointerEventData eventData) { Hovered = false; }
}
