using System.Collections.Generic;
using UnityEngine;
using Enemies.Core;
namespace Enemies.States
{
    // Base state class
    public abstract class EnemyStateBase
    {
        protected EnemyController enemy;
        protected EnemyStateMachine stateMachine;
        
        public EnemyStateBase(EnemyController enemy, EnemyStateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }
        
        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void ExitState();
        public abstract EnemyState GetStateType();
    }

}