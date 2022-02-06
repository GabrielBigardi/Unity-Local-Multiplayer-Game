﻿using UnityEngine;

public class PlayerAirState : IState
{
    private readonly StateMachine _stateMachine;
    private readonly PlayerEntity _playerEntity;

    public PlayerAirState(StateMachine stateMachine, PlayerEntity playerEntity)
    {
        _stateMachine = stateMachine;
        _playerEntity = playerEntity;
    }

    public void Tick()
    {
        _playerEntity.JumpCheck();
        _playerEntity.Shoot();
        _playerEntity.FlipBasedOnInput(false);
    }

    public void FixedTick()
    {
        _playerEntity.SetVelocityX(_playerEntity.core.playerInputHandler.mov.x * (_playerEntity.core.playerInputHandler.holdingRun ? _playerEntity.data.airSpeed * _playerEntity.data.runSpeedMultiplier : _playerEntity.data.airSpeed) * Time.deltaTime);
    }

    public void OnEnter()
    {
        _playerEntity.currentState = "Air";
        _playerEntity.PlayAnimation("Air");
        _playerEntity.core.currentInWaterTime = 0f;
        _playerEntity.core.currentDrowningTime = 0f;
    }

    public void OnExit()
    {

    }
}