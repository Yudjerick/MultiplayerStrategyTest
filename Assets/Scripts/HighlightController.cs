using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HighlightController : NetworkBehaviour
{
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private GameObject attackIndicator;
    private GameObject _currentIndicator;
    public void SetSelectionHighlight(bool value)
    {
        if(value)
        {
            _currentIndicator = Instantiate(selectedIndicator, transform);
        }
        else
        {
            if(_currentIndicator is not null)
            {
                Destroy(_currentIndicator);
            }
        }
    }

    public void SetAttackHighlight(bool value)
    {
        if (value)
        {
            _currentIndicator = Instantiate(attackIndicator, transform);
        }
        else
        {
            if (_currentIndicator is not null)
            {
                Destroy(_currentIndicator);
            }
        }
    }
}
