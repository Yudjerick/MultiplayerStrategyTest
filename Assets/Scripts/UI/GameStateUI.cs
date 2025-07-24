using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameStateUI : NetworkBehaviour
{
    [SerializeField] private PlayerActionController model;
    [Header("UI Elements")]
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text moveText;
    [SerializeField] private TMP_Text turnTimerText;
    [SerializeField] private TMP_Text turnLabelText;
    [SerializeField] private TMP_Text turnCountText;
    [SerializeField] private Button endTurnButton;

    public override void OnNetworkSpawn()
    {
        model.AttackRemaining.OnValueChanged += (prev, curr) => attackText.text = "Attack: " + curr;
        model.MovementRemaining.OnValueChanged += (prev, curr) => moveText.text = "Move: " + curr;
        model.CurrentPlayerId.OnValueChanged += (prev, curr) => {
            turnLabelText.text = NetworkManager.LocalClientId == curr ? "YOUR TURN" : "ENEMY TURN";
            endTurnButton.gameObject.SetActive(NetworkManager.LocalClientId == curr);
            print(curr);
            print(NetworkManager.LocalClientId);
        };
        model.Timer.OnValueChanged += (prev, curr) => turnTimerText.text = model.Timer.Value.ToString();
        model.TurnCount.OnValueChanged += (prev, curr) => turnCountText.text = curr.ToString(); 

        SetInitialUIState();
        endTurnButton.onClick.AddListener(() => PlayerActionController.Singleton.NextTurnServerRpc());
    }

    private void SetInitialUIState()
    {
        attackText.text = "Attack: " + model.AttackRemaining.Value;
        moveText.text = "Move: " + model.MovementRemaining.Value;
        turnLabelText.text = NetworkManager.LocalClientId == model.CurrentPlayerId.Value ? "YOUR TURN" : "ENEMY TURN";
        endTurnButton.gameObject.SetActive(NetworkManager.LocalClientId == model.CurrentPlayerId.Value);
        turnTimerText.text = model.Timer.Value.ToString();
    }
}
