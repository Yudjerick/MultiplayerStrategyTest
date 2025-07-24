using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerActionController : NetworkBehaviour
{
    public static PlayerActionController Singleton;
    public NetworkVariable<ulong> CurrentPlayerId { get; private set; } = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> NextPlayerId { get; private set; } = new NetworkVariable<ulong>();
    public NetworkVariable<int> MovementRemaining { get; private set; } = new NetworkVariable<int>();
    public NetworkVariable<int> AttackRemaining { get; private set; } = new NetworkVariable<int>();
    public NetworkVariable<int> Timer { get; private set; } = new NetworkVariable<int>();

    public NetworkVariable<int> TurnCount { get; private set; } = new NetworkVariable<int>();

    [SerializeField] private int maxTurnDuration;
    [SerializeField] private Unit selectedUnit;
    [SerializeField] private bool readyToMove;
    [SerializeField] private bool readyToAttack;
    [SerializeField] private List<Unit> validAttackTargets;


    [SerializeField] private PathVisualizer pathVisualizer;
    [SerializeField] private AttackRangeVizualizer attackRangeVizualizer;

    [SerializeField] private float moveConfirmClickAcceptanceRadius;
    private Vector3 moveTargetPosition;
    
    private bool recievePlayerInput = true;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TurnCount.Value = 1;
            AttackRemaining.Value = 1;
            MovementRemaining.Value = 1;
            Timer.Value = maxTurnDuration;
        }
        Singleton = this;
        CurrentPlayerId.OnValueChanged += (prev, curr) => HandleNewTurn();
        HandleNewTurn();
    }

    [Rpc(SendTo.Server)]
    public void StartTimerServerRpc()
    {
        InvokeRepeating(nameof(UpdateTimer), 1f, 1f);
    }

    private void UpdateTimer()
    {
        if (IsServer)
        {
            if (Timer.Value > 0)
            {
                Timer.Value--;
            }
            else
            {
                NextTurnServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void NextTurnServerRpc()
    {
        TurnCount.Value++;
        AttackRemaining.Value = 1;
        MovementRemaining.Value = 1;
        Timer.Value = maxTurnDuration;
        var buff = CurrentPlayerId.Value;
        CurrentPlayerId.Value = NextPlayerId.Value;
        NextPlayerId.Value = buff;
    }

    private void HandleNewTurn()
    {
        pathVisualizer.ErasePathes();
        attackRangeVizualizer.EraseRange();
        selectedUnit?.SetSelected(false);
        readyToAttack = false;
        readyToMove = false;
        recievePlayerInput = CurrentPlayerId.Value == NetworkManager.LocalClientId;

    }

    private void EndTurnIfOutOfActions()
    {
        if(MovementRemaining.Value == 0 && AttackRemaining.Value == 0)
        {
            NextTurnServerRpc();
        }
    }

    public void GroundClickedWithMoveButton(Vector3 point)
    {
        if(!recievePlayerInput || selectedUnit is null)
        {
            return;
        }
        if (!readyToMove)
        {
            BuildPath(selectedUnit, point);
        }
        else
        {
            if (Vector3.Distance(point, moveTargetPosition) < moveConfirmClickAcceptanceRadius)
            {
                TryMoveServerRpc(selectedUnit.NetworkObject, moveTargetPosition);
            }
            else
            {
                BuildPath(selectedUnit, point);
            }
        }
    }

    public void GroundClickedWithCancelButton(Vector3 point)
    {
        if (!recievePlayerInput)
        {
            return;
        }
        if(selectedUnit is not null)
        {
            GetReadyToAtack();
        }
    }

    private bool BuildPath(Unit unit, Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
        {
            
            Pathfinder pathfinder = new Pathfinder();
            bool isPathValid = pathfinder.TryCalculatePath(unit.transform.position, point,
                selectedUnit.Speed, out List<Vector3> path);
            if (isPathValid)
            {
                readyToAttack = false;
                readyToMove = true;
                moveTargetPosition = hit.position;
                pathVisualizer.DrawValidPath(path);
                attackRangeVizualizer.DrawRange(point, unit.AttackRange);
                UpdateValidTargets(new Vector3(point.x, unit.transform.position.y, point.z));
            }
            return isPathValid;
        }
        return false;
    }

    public void UnitClickedWithSelectButton(Unit unit)
    {
        if (!recievePlayerInput || selectedUnit == unit)
        {
            return;
        }
        if(unit.OwnerId.Value == CurrentPlayerId.Value)
        {
            Select(unit);
        }
    }

    public void UnitClickedWithAttackButton(Unit unit)
    {
        if(unit.OwnerId.Value != CurrentPlayerId.Value)
        {
            if (AttackRemaining.Value > 0 && readyToAttack && validAttackTargets.Contains(unit))
            {
                TryAttackServerRpc(selectedUnit.NetworkObject, unit.NetworkObject);
            }
        }
    }

    private void Select(Unit unit)
    {
        selectedUnit?.SetSelected(false);
        selectedUnit = unit;
        GetReadyToAtack();
        unit.SetSelected(true);
    }

    [Rpc(SendTo.Server)]
    private void TryAttackServerRpc(NetworkObjectReference attackerRef, NetworkObjectReference targetRef)
    {
        if( attackerRef.TryGet(out var attacker) && targetRef.TryGet(out var target) && AttackRemaining.Value > 0)
        {
            target.Despawn(true);
            AttackRemaining.Value--;
            /*Unit unit = target.GetComponent<Unit>();
            if(unit.OwnerId.Value == CurrentPlayerId.Value)
            {
                UpdateValidTargets(attacker.transform.position);
                if (validAttackTargets.Contains(unit))
                {
                    target.Despawn();
                    AttackRemaining.Value--;
                }
            }*/
        }    
    }

    [Rpc(SendTo.Server)]
    private void TryMoveServerRpc(NetworkObjectReference unitRef, Vector3 point)
    {
        if (unitRef.TryGet(out var unitObj) && MovementRemaining.Value > 0)
        {
            Unit unit = unitObj.GetComponent<Unit>();
            Select(unit);
            OnMoveStartClientRpc();
            unit.StartCoroutine(unit.MoveTo(point, () => {
                OnMoveCompleteClientRpc();
            }));
            MovementRemaining.Value--;
            /*Unit unit = unitObj.GetComponent<Unit>();
            if(unit.OwnerId.Value == CurrentPlayerId.Value && BuildPath(unit.GetComponent<Unit>(), point))
            {
                OnMoveStartClientRpc();
                selectedUnit.StartCoroutine(selectedUnit.MoveTo(point, () => {
                    OnMoveCompleteClientRpc();
                }));
                MovementRemaining.Value--;
            }*/
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnMoveStartClientRpc()
    {
        recievePlayerInput = false;
        pathVisualizer.ErasePathes();
        attackRangeVizualizer.EraseRange();
        UnhighlightTargets();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnMoveCompleteClientRpc()
    {
        recievePlayerInput = true;
        GetReadyToAtack();
    }

    private void UpdateValidTargets(Vector3 attackingUnitPosition)
    {
        UnhighlightTargets();
        validAttackTargets.Clear();
        var unitsCollidersInRange = Physics.OverlapSphere(attackingUnitPosition, selectedUnit.AttackRange).Where(col => col.CompareTag("Unit"));
        List<Collider> uncoveredColliders = new List<Collider>();
        foreach (var collider in unitsCollidersInRange)
        {
            Physics.Raycast(attackingUnitPosition, collider.transform.position - attackingUnitPosition, out RaycastHit hit);
            if (hit.collider == collider)
            {
                uncoveredColliders.Add(collider);
            }
        }
        validAttackTargets = uncoveredColliders
            .Select(col => col.GetComponent<Unit>())
            .Where(unit => unit.OwnerId.Value != CurrentPlayerId.Value)
            .ToList();
        HighlightValidTargets();
    }

    private void UnhighlightTargets()
    {
        foreach (var unit in validAttackTargets)
        {
            unit?.SetAsAttackTarget(false);
        }
    }

    private void HighlightValidTargets()
    {
        foreach (var unit in validAttackTargets)
        {
            unit.SetAsAttackTarget(true);
        }
    }

    private void GetReadyToAtack()
    {
        if(selectedUnit == null)
        {
            return;
        }
        pathVisualizer.ErasePathes();
        readyToMove = false;
        readyToAttack = true;
        UpdateValidTargets(selectedUnit.transform.position);
        attackRangeVizualizer.DrawRange(selectedUnit.transform.position, selectedUnit.AttackRange);
    }
}
