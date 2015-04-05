﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy.Hooks;
using com.spacepuppy.Utils;

namespace com.spacepuppy
{

    /// <summary>
    /// This class is really only for internal use by com.spacepuppy, avoid using it outside of it.
    /// </summary>
    public class GameLoopEntry : Singleton
    {

        #region Events

        public static event System.EventHandler EarlyUpdate;
        public static event System.EventHandler OnUpdate;
        public static event System.EventHandler TardyUpdate;
        public static event System.EventHandler EarlyFixedUpdate;
        public static event System.EventHandler OnFixedUpdate;
        public static event System.EventHandler TardyFixedUpdate;
        public static event System.EventHandler EarlyLateUpdate;
        public static event System.EventHandler OnLateUpdate;
        public static event System.EventHandler TardyLateUpdate;

        #endregion

        #region Fields

        private static GameLoopEntry _instance;
        private static UpdateSequence _currentSequence;
        private static bool _applicationClosing;
        private static System.Action<bool> _internalEarlyUpdate;

        private UpdateEventHooks _updateHook;
        private TardyExecutionUpdateEventHooks _tardyUpdateHook;

        private System.Action _invoking;
        private object _invokeLock = new object();

        #endregion

        #region CONSTRUCTOR

        public static void Init()
        {
            if (_instance != null) return;

            _instance = Singleton.CreateSpecialInstance<GameLoopEntry>("SpacePuppy.GameLoopEntry");
            //_instance = Singleton.GetInstance<GameLoopEntry>();
        }

        protected override void Awake()
        {
            base.Awake();

            _updateHook = this.gameObject.AddComponent<UpdateEventHooks>();
            _tardyUpdateHook = this.gameObject.AddComponent<TardyExecutionUpdateEventHooks>();

            _updateHook.UpdateHook += _updateHook_Update;
            _tardyUpdateHook.UpdateHook += _tardyUpdateHook_Update;

            _updateHook.FixedUpdateHook += _updateHook_FixedUpdate;
            _tardyUpdateHook.FixedUpdateHook += _tardyUpdateHook_FixedUpdate;

            _updateHook.LateUpdateHook += _updateHook_LateUpdate;
            _tardyUpdateHook.LateUpdateHook += _tardyUpdateHook_LateUpdate;
        }

        /// <summary>
        /// A special static, register once, earlyupdate event hook that preceeds ALL other events. 
        /// This is used internally by some special static classes (namely SPTime) that needs extra 
        /// high precedence early access.
        /// </summary>
        /// <param name="d"></param>
        internal static void RegisterInternalEarlyUpdate(System.Action<bool> d)
        {
            _internalEarlyUpdate += d;
        }

        #endregion

        #region Properties

        public static GameLoopEntry Hook
        {
            get
            {
                if (_instance == null) GameLoopEntry.Init();
                return _instance;
            }
        }

        /// <summary>
        /// Returns which event sequence that code is currently operating as. 
        /// WARNING - during 'OnMouseXXX' messages this will report that we're in the FixedUpdate sequence. 
        /// This is because there's no end of FixedUpdate available to hook into, so it reports FixedUpdate 
        /// until Update starts, and 'OnMouseXXX' occurs in between those 2.
        /// </summary>
        public static UpdateSequence CurrentSequence { get { return _currentSequence; } }

        /// <summary>
        /// Returns true if the OnApplicationQuit message has been received.
        /// </summary>
        public static bool ApplicationClosing { get { return _applicationClosing; } }

        #endregion

        #region Methods

        public static void InvokeNextUpdate(System.Action action)
        {
            if(action == null) throw new System.ArgumentNullException("action");
            var h = Hook;
            lock (h._invokeLock)
            {
                h._invoking += action;
            }
        }

        #endregion

        #region Event Handlers

        private void OnApplicationQuit()
        {
            _applicationClosing = true;
        }

        //Update

        private void Update()
        {
            //Track entry into update loop
            _currentSequence = UpdateSequence.Update;

            if (_internalEarlyUpdate != null) _internalEarlyUpdate(false);
            if (EarlyUpdate != null) EarlyUpdate(this, System.EventArgs.Empty);
        }

        private void _updateHook_Update(object sender, System.EventArgs e)
        {
            if (OnUpdate != null) OnUpdate(this, e);

            if (_invoking != null)
            {
                System.Action act;
                lock(_invokeLock)
                {
                    act = _invoking;
                    _invoking = null;
                }
                act();
            }
        }

        private void _tardyUpdateHook_Update(object sender, System.EventArgs e)
        {
            if (TardyUpdate != null) TardyUpdate(this, e);
        }

        //Fixed Update

        private void FixedUpdate()
        {
            //Track entry into fixedupdate loop
            _currentSequence = UpdateSequence.FixedUpdate;

            if (_internalEarlyUpdate != null) _internalEarlyUpdate(true);
            if (EarlyFixedUpdate != null) EarlyFixedUpdate(this, System.EventArgs.Empty);
        }

        private void _updateHook_FixedUpdate(object sender, System.EventArgs e)
        {
            if (OnFixedUpdate != null) OnFixedUpdate(this, e);
        }

        private void _tardyUpdateHook_FixedUpdate(object sender, System.EventArgs e)
        {
            if (TardyFixedUpdate != null) TardyFixedUpdate(this, e);

            ////Track exit of fixedupdate loop
            //_currentSequence = UpdateSequence.None;
        }

        //LateUpdate

        private void LateUpdate()
        {
            _currentSequence = UpdateSequence.LateUpdate;
            if (EarlyLateUpdate != null) EarlyLateUpdate(this, System.EventArgs.Empty);
        }

        private void _updateHook_LateUpdate(object sender, System.EventArgs e)
        {
            if (OnLateUpdate != null) OnLateUpdate(this, e);
        }

        private void _tardyUpdateHook_LateUpdate(object sender, System.EventArgs e)
        {
            if (TardyLateUpdate != null) TardyLateUpdate(this, e);

            //Track exit of update loop
            _currentSequence = UpdateSequence.None;
        }

        #endregion

    }
}
