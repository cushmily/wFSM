﻿using System;
using System.Collections.Generic;

namespace wLib.Fsm
{
    public abstract class StateBase : IState
    {
        #region IState Implementation

        public IState Parent { get; set; }

        public float ElapsedTime { get; private set; }

        public Dictionary<string, IState> Children { get; } = new Dictionary<string, IState>();

        public Stack<IState> ActiveStates { get; } = new Stack<IState>();

        public virtual void Enter()
        {
            OnEnter?.Invoke();

            ElapsedTime = 0f;
        }

        public virtual void Update(float deltaTime)
        {
            // Only update the latest state
            if (ActiveStates.Count > 0)
            {
                ActiveStates.Peek().Update(deltaTime);
                return;
            }

            OnUpdate?.Invoke(deltaTime);

            ElapsedTime += deltaTime;

            // Check if condition meets
            foreach (var conditionPair in _conditions)
            {
                if (conditionPair.Key.Invoke()) { conditionPair.Value?.Invoke(); }
            }
        }

        public virtual void Exit()
        {
            OnExit?.Invoke();
        }

        public virtual void ChangeState(string name)
        {
            IState result;
            if (!Children.TryGetValue(name, out result))
            {
                throw new ApplicationException($"Child state [{name}] not found.");
            }

            if (ActiveStates.Count > 0) { PopState(); }

            PrivatePushState(result);
        }

        public void PushState(string name)
        {
            IState result;
            if (!Children.TryGetValue(name, out result))
            {
                throw new ApplicationException($"Child state [{name}] not found.");
            }

            PrivatePushState(result);
        }

        public void PopState()
        {
            PrivatePopState();
        }

        public void TriggerEvent(string id)
        {
            TriggerEvent(id, EventArgs.Empty);
        }

        public void TriggerEvent(string id, EventArgs eventArgs)
        {
            if (ActiveStates.Count > 0)
            {
                ActiveStates.Peek().TriggerEvent(id, eventArgs);
                return;
            }

            Action<EventArgs> action;
            if (!_events.TryGetValue(id, out action))
            {
                throw new ApplicationException($"Event [{id}] not exits.");
            }

            action?.Invoke(eventArgs);
        }

        #endregion

        #region Actions

        public event Action OnEnter;
        public event Action OnExit;
        public event Action<float> OnUpdate;

        #endregion

        #region Runtime

        private readonly Dictionary<string, Action<EventArgs>> _events = new Dictionary<string, Action<EventArgs>>();
        private readonly Dictionary<Func<bool>, Action> _conditions = new Dictionary<Func<bool>, Action>();

        #endregion

        #region Private Operations

        private void PrivatePopState()
        {
            var result = ActiveStates.Pop();
            result.Exit();
        }

        private void PrivatePushState(IState state)
        {
            ActiveStates.Push(state);
            state.Enter();
        }

        #endregion

        #region Helper

        public void AddChild(string name, IState state)
        {
            if (!Children.ContainsKey(name))
            {
                Children.Add(name, state);
                state.Parent = this;
            }
            else { throw new ApplicationException($"Child state already exists: {name}"); }
        }

        public void SetEnterAction(Action onEnter)
        {
            OnEnter = onEnter;
        }

        public void SetExitAction(Action onExit)
        {
            OnExit = onExit;
        }

        public void SetUpdateAction(Action<float> onUpdate)
        {
            OnUpdate = onUpdate;
        }

        public void AddEvent(string id, Action<EventArgs> action)
        {
            if (!_events.ContainsKey(id)) { _events.Add(id, action); }
            else { throw new ApplicationException($"Event already exists: {id}"); }
        }

        public void AddEvent<TArgs>(string id, Action<TArgs> action) where TArgs : EventArgs
        {
            if (!_events.ContainsKey(id)) { _events.Add(id, arg => { action.Invoke((TArgs) arg); }); }
            else { throw new ApplicationException($"Event already exists: {id}"); }
        }

        public void AddCondition(Func<bool> predicate, Action action)
        {
            _conditions.Add(predicate, action);
        }

        #endregion
    }
}