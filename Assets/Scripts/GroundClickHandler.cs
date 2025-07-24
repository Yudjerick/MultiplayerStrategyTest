using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GroundClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlayerActionController controller;
    [SerializeField] private PointerEventData.InputButton moveButton;
    [SerializeField] private PointerEventData.InputButton cancelButton;
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == moveButton) 
        {
            controller.GroundClickedWithMoveButton(eventData.pointerCurrentRaycast.worldPosition);
        }
        else if(eventData.button == cancelButton)
        {
            controller.GroundClickedWithCancelButton(eventData.pointerCurrentRaycast.worldPosition);
        }
    }
}
