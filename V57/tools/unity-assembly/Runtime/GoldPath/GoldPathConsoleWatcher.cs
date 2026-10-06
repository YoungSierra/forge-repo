using System;
using System.Collections.Generic;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Counts Error/Exception/Assert log messages (any thread) while a gold path runs.
    /// The driver itself reports step failures as warnings so they do not inflate this count.
    /// </summary>
    public sealed class GoldPathConsoleWatcher : IDisposable
    {
        #region Fields

        private const int MaxSamples = 20;

        private readonly object _lock = new object();
        private readonly List<string> _samples = new List<string>();
        private int _errorCount;
        private bool _listening;

        #endregion

        #region Public Methods

        public int ErrorCount
        {
            get
            {
                lock (_lock)
                {
                    return _errorCount;
                }
            }
        }

        public string[] GetSamples()
        {
            lock (_lock)
            {
                return _samples.ToArray();
            }
        }

        public void Start()
        {
            if (_listening)
            {
                return;
            }

            Application.logMessageReceivedThreaded += OnLogMessage;
            _listening = true;
        }

        public void Stop()
        {
            if (!_listening)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= OnLogMessage;
            _listening = false;
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region Event Handlers

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            lock (_lock)
            {
                _errorCount++;
                if (_samples.Count < MaxSamples)
                {
                    _samples.Add($"{type}: {condition}");
                }
            }
        }

        #endregion
    }
}
