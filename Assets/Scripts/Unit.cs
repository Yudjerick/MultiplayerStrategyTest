using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Unit : NetworkBehaviour, IPointerClickHandler
{
    public NetworkVariable<ulong> OwnerId { get; set; } = new NetworkVariable<ulong>();

    public NetworkVariable<Color> Color { get; set; } = new NetworkVariable<Color>();

    [SerializeField] private HighlightController highlightController;
    [SerializeField] private float moveFinishedTolerance;
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public float AttackRange { get; private set; }

    [SerializeField] private PointerEventData.InputButton selectButton;
    [SerializeField] private PointerEventData.InputButton attackButton;

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.enabled = true;
        agent.enabled = false;
        GetComponent<MeshRenderer>().material.color = Color.Value;
    }

    public IEnumerator MoveTo(Vector3 target, Action onComplete)
    {
        agent.enabled = true;
        agent.destination = target;
        yield return new WaitUntil(() => IsMovingComplete());
        agent.enabled = false;
        onComplete();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == selectButton)
        {
            PlayerActionController.Singleton.UnitClickedWithSelectButton(this);
        }
        else if(eventData.button == attackButton)
        {
            PlayerActionController.Singleton.UnitClickedWithAttackButton(this);
        }
    }

    public void SetSelected(bool value)
    {
        if (!IsSpawned)
        {
            return;
        }
        highlightController?.SetSelectionHighlight(value);
        obstacle.carving = !value;
    }

    public void SetAsAttackTarget(bool value)
    {
        if (!IsSpawned)
        {
            return;
        }
        highlightController?.SetAttackHighlight(value);
    }

    private bool IsMovingComplete()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
