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
    [SerializeField] private PathVisualizer pathVisualizer;
    [SerializeField] private AttackRangeVizualizer attackRangeVizualizer;
    [SerializeField] private float moveConfirmClickAcceptanceRadius;

    private Vector3 _moveTargetPosition;
    private bool _recievePlayerInput = true;
    private Unit _selectedUnit;
    private bool _readyToMove;
    private bool _readyToAttack;
    private List<Unit> _validAttackTargets = new List<Unit>();

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
        UnhighlightTargets();
        pathVisualizer.ErasePathes();
        attackRangeVizualizer.EraseRange();
        _selectedUnit?.SetSelected(false);
        _readyToAttack = false;
        _readyToMove = false;
        _recievePlayerInput = CurrentPlayerId.Value == NetworkManager.LocalClientId;
    }
    public void GroundClickedWithMoveButton(Vector3 point)
    {
        if(!_recievePlayerInput || _selectedUnit is null)
        {
            return;
        }
        if (!_readyToMove)
        {
            BuildPath(_selectedUnit, point);
        }
        else
        {
            if (Vector3.Distance(point, _moveTargetPosition) < moveConfirmClickAcceptanceRadius)
            {
                TryMoveServerRpc(_selectedUnit.NetworkObject, _moveTargetPosition);
            }
            else
            {
                BuildPath(_selectedUnit, point);
            }
        }
    }

    public void GroundClickedWithCancelButton(Vector3 point)
    {
        if (!_recievePlayerInput)
        {
            return;
        }
        if(_selectedUnit is not null)
        {
            GetReadyToAtack(_selectedUnit.transform.position);
        }
    }

    private bool BuildPath(Unit unit, Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
        {
            
            Pathfinder pathfinder = new Pathfinder();
            bool isPathValid = pathfinder.TryCalculatePath(unit.transform.position, point,
                _selectedUnit.Speed, out List<Vector3> path);
            if (isPathValid)
            {
                _readyToAttack = false;
                _readyToMove = true;
                _moveTargetPosition = hit.position;
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
        if (!_recievePlayerInput || _selectedUnit == unit)
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
            if (AttackRemaining.Value > 0 && _readyToAttack && _validAttackTargets.Contains(unit))
            {
                TryAttackServerRpc(_selectedUnit.NetworkObject, unit.NetworkObject);
            }
        }
    }

    private void Select(Unit unit)
    {
        _selectedUnit?.SetSelected(false);
        _selectedUnit = unit;
        GetReadyToAtack(unit.transform.position);
        unit.SetSelected(true);
    }

    [Rpc(SendTo.Server)]
    private void TryAttackServerRpc(NetworkObjectReference attackerRef, NetworkObjectReference targetRef)
    {
        if( attackerRef.TryGet(out var attacker) && targetRef.TryGet(out var target) && AttackRemaining.Value > 0)
        {
            target.Despawn(true);
            AttackRemaining.Value--;
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
                OnMoveCompleteClientRpc(unit.transform.position);
            }));
            MovementRemaining.Value--;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnMoveStartClientRpc()
    {
        _recievePlayerInput = false;
        pathVisualizer.ErasePathes();
        attackRangeVizualizer.EraseRange();
        UnhighlightTargets();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnMoveCompleteClientRpc(Vector3 attackerPosition)
    {
        _recievePlayerInput = true;
        GetReadyToAtack(attackerPosition);
    }

    private void UpdateValidTargets(Vector3 attackingUnitPosition)
    {
        UnhighlightTargets();
        _validAttackTargets.Clear();
        var unitsCollidersInRange = Physics.OverlapSphere(attackingUnitPosition, _selectedUnit.AttackRange).Where(col => col.CompareTag("Unit"));
        List<Collider> uncoveredColliders = new List<Collider>();
        foreach (var collider in unitsCollidersInRange)
        {
            Physics.Raycast(attackingUnitPosition, collider.transform.position - attackingUnitPosition, out RaycastHit hit);
            if (hit.collider == collider)
            {
                uncoveredColliders.Add(collider);
            }
        }
        _validAttackTargets = uncoveredColliders
            .Select(col => col.GetComponent<Unit>())
            .Where(unit => unit.OwnerId.Value != CurrentPlayerId.Value)
            .ToList();
        HighlightValidTargets();
    }

    private void UnhighlightTargets()
    {
        foreach (var unit in _validAttackTargets)
        {
            unit?.SetAsAttackTarget(false);
        }
    }

    private void HighlightValidTargets()
    {
        foreach (var unit in _validAttackTargets)
        {
            unit.SetAsAttackTarget(true);
        }
    }

    private void GetReadyToAtack(Vector3 attakerPosition)
    {
        if(_selectedUnit == null)
        {
            return;
        }
        pathVisualizer.ErasePathes();
        _readyToMove = false;
        _readyToAttack = true;
        UpdateValidTargets(attakerPosition);
        attackRangeVizualizer.DrawRange(attakerPosition, _selectedUnit.AttackRange);
    }
}
