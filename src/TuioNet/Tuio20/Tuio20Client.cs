﻿using System.Collections.Generic;
using System.Linq;
using OSC.NET;
using TuioNet.Common;

namespace TuioNet.Tuio20
{
    public class Tuio20Client
    {
        private readonly TuioReceiver _tuioReceiver;
        private readonly List<ITuio20Listener> _tuioListeners = new List<ITuio20Listener>();
        private readonly Dictionary<uint, Tuio20Object> _tuioObjects = new Dictionary<uint, Tuio20Object>();

        private OSCMessage _frmMessage;
        private readonly List<OSCMessage> _otherMessages = new List<OSCMessage>();
        
        private uint _bundleFrameId = 0;
        private uint _nextFrameId = 0;
        
        private uint _prevFrameId = 0;
        private TuioTime _prevFrameTime;

        /// <summary>
        /// The sensor dimension. The first two bytes represent the width. The last two byte represent the height.
        /// </summary>
        public uint SensorDimension { get; private set; } = 0;
        
        /// <summary>
        /// Provide additional information about the TUIO source.
        /// </summary>
        public string Source { get; private set; }
        public object TuioObjectLock { get; } = new object();
        
        /// <summary>
        /// Create new client for TUIO 2.0.
        /// </summary>
        /// <param name="connectionType">Type of the protocol which gets used to connect to the sender.</param>
        /// <param name="address">The IP address of the TUIO sender.</param>
        /// <param name="port">The port the client listen to for new TUIO messages. Default UDP port is 3333.</param>
        /// <param name="isAutoProcess">If set, the receiver processes incoming messages automatically. Otherwise the ProcessMessages() methods needs to be called manually.</param>
        public Tuio20Client(TuioConnectionType connectionType, string address = "0.0.0.0", int port = 3333, bool isAutoProcess = true)
        {
            _tuioReceiver = TuioReceiver.FromConnectionType(connectionType, address, port, isAutoProcess);
            _tuioReceiver.AddMessageListener("/tuio2/frm", OnFrm);
            _tuioReceiver.AddMessageListener("/tuio2/alv", OnAlv);
            _tuioReceiver.AddMessageListener("/tuio2/tok", OnOther);
            _tuioReceiver.AddMessageListener("/tuio2/ptr", OnOther);
            _tuioReceiver.AddMessageListener("/tuio2/bnd", OnOther);
            _tuioReceiver.AddMessageListener("/tuio2/sym", OnOther);
        }

        /// <summary>
        /// Establish a connection to the TUIO sender.
        /// </summary>
        public void Connect()
        {
            _prevFrameTime = new TuioTime(0, 0);
            _tuioReceiver.Connect();
        }

        /// <summary>
        /// Closes the connection to the TUIO sender.
        /// </summary>
        public void Disconnect()
        {
            _tuioReceiver.Disconnect();
        }
        
        /// <summary>
        /// Process the TUIO messages in the message queue and invoke callbacks of the associated message listener. Only needs to be called if isAutoProcess is set to false.
        /// </summary>
        public void ProcessMessages()
        {
            _tuioReceiver.ProcessMessages();
        }
        
        /// <summary>
        /// Returns all active TUIO tokens.
        /// </summary>
        /// <returns>A list of all active TUIO tokens.</returns>
        public List<Tuio20Token> GetTuioTokenList()
        {
            lock (TuioObjectLock)
            {
                var tuioTokenList = new List<Tuio20Token>();
                foreach (var entry in _tuioObjects)
                {
                    if (entry.Value.ContainsTuioToken())
                    {
                        tuioTokenList.Add(entry.Value.Token);
                    }
                }

                return tuioTokenList;
            }
        }

        /// <summary>
        /// Returns all active TUIO pointers.
        /// </summary>
        /// <returns>A list of all active TUIO pointers.</returns>
        public List<Tuio20Pointer> GetTuioPointerList()
        {
            lock (TuioObjectLock)
            {
                var tuioPointerList = new List<Tuio20Pointer>();
                foreach (var entry in _tuioObjects)
                {
                    if (entry.Value.ContainsTuioPointer())
                    {
                        tuioPointerList.Add(entry.Value.Pointer);
                    }
                }

                return tuioPointerList;
            }
        }

        /// <summary>
        /// Returns all active TUIO bounds.
        /// </summary>
        /// <returns>A list of all active TUIO bounds.</returns>
        public List<Tuio20Bounds> GetTuioBoundsList()
        {
            lock (TuioObjectLock)
            {
                var tuioBoundsList = new List<Tuio20Bounds>();
                foreach (var entry in _tuioObjects)
                {
                    if (entry.Value.ContainsTuioBounds())
                    {
                        tuioBoundsList.Add(entry.Value.Bounds);
                    }
                }

                return tuioBoundsList;
            }
        }

        /// <summary>
        /// Returns all active TUIO marker symbols.
        /// </summary>
        /// <returns>A list of all active TUIO marker symbols.</returns>
        public List<Tuio20Symbol> GetTuioSymbolList()
        {
            lock (TuioObjectLock)
            {
                var tuioSymbolList = new List<Tuio20Symbol>();
                foreach (var entry in _tuioObjects)
                {
                    if (entry.Value.ContainsTuioSymbol())
                    {
                        tuioSymbolList.Add(entry.Value.Symbol);
                    }
                }

                return tuioSymbolList;
            }
        }
        
        public void AddTuioListener(ITuio20Listener tuio20Listener)
        {
            _tuioListeners.Add(tuio20Listener);
        }

        public void RemoveTuioListener(ITuio20Listener tuio20Listener)
        {
            _tuioListeners.Remove(tuio20Listener);
        }

        public void RemoveAllTuioListeners()
        {
            _tuioListeners.Clear();
        }
        
        private void OnFrm(OSCMessage oscMessage)
        {
            _bundleFrameId = (uint)(int)oscMessage.Values[0];

            if (_bundleFrameId > _nextFrameId)
            {
                _otherMessages.Clear();
                _nextFrameId = _bundleFrameId;
                _frmMessage = oscMessage;
            }
        }
        
        private void OnOther(OSCMessage oscMessage)
        {
            if (_bundleFrameId != _nextFrameId)
            {
                return;
            }

            _otherMessages.Add(oscMessage);
        }

        private void OnAlv(OSCMessage oscMessage)
        {
            if (_frmMessage == null || _bundleFrameId != _nextFrameId)
            {
                return;
            }

            var frameId = (uint)(int)_frmMessage.Values[0];
            var frameTime = (OscTimeTag)_frmMessage.Values[1];
            var dim = (uint)(int)_frmMessage.Values[2];
            var source = (string)_frmMessage.Values[3];
            TuioTime currentFrameTime = TuioTime.FromOscTime(frameTime);
            if (frameId >= _prevFrameId || frameId == 0 || 
                (currentFrameTime - _prevFrameTime).GetTotalMilliseconds() >= 1000)
            {
                SensorDimension = dim;
                Source = source;
                HashSet<uint> currentSIds = new HashSet<uint>(_tuioObjects.Keys);
                HashSet<uint> aliveSIds = new HashSet<uint>();
                foreach (var sId in oscMessage.Values)
                {
                    aliveSIds.Add((uint)(int) sId);
                }
                HashSet<uint> newSIds = new HashSet<uint>(aliveSIds.Except(currentSIds));
                HashSet<uint> removedSIds = new HashSet<uint>(currentSIds.Except(aliveSIds));
                HashSet<Tuio20Object> addedTuioObjects = new HashSet<Tuio20Object>();
                HashSet<Tuio20Object> updatedTuioObjects = new HashSet<Tuio20Object>();
                HashSet<Tuio20Object> removedTuioObjects = new HashSet<Tuio20Object>();
                lock (TuioObjectLock)
                {
                    foreach (var sId in newSIds)
                    {
                        var tuioObject = new Tuio20Object(_prevFrameTime, sId);
                        _tuioObjects[sId] = tuioObject;
                    }
                    foreach (var sId in removedSIds)
                    {
                        var tuioObject = _tuioObjects[sId];
                        removedTuioObjects.Add(tuioObject);
                        tuioObject.Remove(currentFrameTime);
                        _tuioObjects.Remove(sId);
                    }

                    foreach (var otherOscMessage in _otherMessages)
                    {
                        if (otherOscMessage.Address == "/tuio2/tok")
                        {
                            var sId = (uint)(int)otherOscMessage.Values[0];
                            if (aliveSIds.Contains(sId))
                            {
                                var tuId = (uint)(int)otherOscMessage.Values[1];
                                var componentId = (uint)(int)otherOscMessage.Values[2];
                                var posX = (float)otherOscMessage.Values[3];
                                var posY = (float)otherOscMessage.Values[4];
                                var angle = (float)otherOscMessage.Values[5];
                                float velocityX = 0, velocityY = 0, angleVelocity = 0, acceleration = 0, rotationAcceleration = 0;
                                if (otherOscMessage.Values.Count > 6)
                                {
                                    velocityX = (float)otherOscMessage.Values[6];
                                    velocityY = (float)otherOscMessage.Values[7];
                                    angleVelocity = (float)otherOscMessage.Values[8];
                                    acceleration = (float)otherOscMessage.Values[9];
                                    rotationAcceleration = (float)otherOscMessage.Values[10];
                                }

                                var tuioObject = _tuioObjects[sId];
                                if (tuioObject.Token == null)
                                {
                                    addedTuioObjects.Add(tuioObject);
                                    tuioObject.SetTuioToken(new Tuio20Token(_prevFrameTime, tuioObject, tuId, componentId, posX,
                                        posY, angle, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration));
                                }
                                else
                                {
                                    if (tuioObject.Token.HasChanged(tuId, componentId, posX,
                                        posY, angle, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration))
                                    {
                                        updatedTuioObjects.Add(tuioObject);
                                        tuioObject.Token.Update(_prevFrameTime, tuId, componentId, posX,
                                            posY, angle, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration);
                                    }
                                }
                            }
                        }
                        else if (otherOscMessage.Address == "/tuio2/ptr")
                        {
                            var sId = (uint)(int)otherOscMessage.Values[0];
                            if (aliveSIds.Contains(sId))
                            {
                                var tuId = (uint)(int)otherOscMessage.Values[1];
                                var componentId = (uint)(int)otherOscMessage.Values[2];
                                var posX = (float)otherOscMessage.Values[3];
                                var posY = (float)otherOscMessage.Values[4];
                                var angle = (float)otherOscMessage.Values[5];
                                var shear = (float)otherOscMessage.Values[6];
                                var radius = (float)otherOscMessage.Values[7];
                                var press = (float)otherOscMessage.Values[8];
                                float velocityX = 0, velocityY = 0, pressureVelocity = 0, acceleration = 0, pressureAcceleration = 0;
                                if (otherOscMessage.Values.Count > 9)
                                {
                                    velocityX = (float)otherOscMessage.Values[6];
                                    velocityY = (float)otherOscMessage.Values[7];
                                    pressureVelocity = (float)otherOscMessage.Values[8];
                                    acceleration = (float)otherOscMessage.Values[9];
                                    pressureAcceleration = (float)otherOscMessage.Values[10];
                                }

                                var tuioObject = _tuioObjects[sId];
                                if (tuioObject.Pointer == null)
                                {
                                    addedTuioObjects.Add(tuioObject);
                                    tuioObject.SetTuioPointer(new Tuio20Pointer(_prevFrameTime, tuioObject, tuId, componentId, 
                                        posX, posY, angle, shear, radius, press, velocityX, velocityY, pressureVelocity, acceleration, pressureAcceleration));
                                }
                                else
                                {
                                    if (tuioObject.Pointer.HasChanged(tuId, componentId, posX,
                                        posY, angle, shear, radius, press, velocityX, velocityY, pressureVelocity, acceleration, pressureAcceleration))
                                    {
                                        updatedTuioObjects.Add(tuioObject);
                                        tuioObject.Pointer.Update(_prevFrameTime, tuId, componentId, posX,
                                            posY, angle, shear, radius, press, velocityX, velocityY, pressureVelocity, acceleration, pressureAcceleration);
                                    }
                                }
                            }
                        }
                        else if (otherOscMessage.Address == "/tuio2/bnd")
                        {
                            var sId = (uint)(int)otherOscMessage.Values[0];
                            if (aliveSIds.Contains(sId))
                            {
                                var posX = (float)otherOscMessage.Values[1];
                                var posY = (float)otherOscMessage.Values[2];
                                var angle = (float)otherOscMessage.Values[3];
                                var width = (float)otherOscMessage.Values[4];
                                var height = (float)otherOscMessage.Values[5];
                                var area = (float)otherOscMessage.Values[6];
                                float velocityX = 0, velocityY = 0, angleVelocity = 0, acceleration = 0, rotationAcceleration = 0;
                                if (otherOscMessage.Values.Count > 7)
                                {
                                    velocityX = (float)otherOscMessage.Values[7];
                                    velocityY = (float)otherOscMessage.Values[8];
                                    angleVelocity = (float)otherOscMessage.Values[9];
                                    acceleration = (float)otherOscMessage.Values[10];
                                    rotationAcceleration = (float)otherOscMessage.Values[11];
                                }

                                var tuioObject = _tuioObjects[sId];
                                if (tuioObject.Bounds == null)
                                {
                                    addedTuioObjects.Add(tuioObject);
                                    tuioObject.SetTuioBounds(new Tuio20Bounds(_prevFrameTime, tuioObject, posX,
                                        posY, angle, width, height, area, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration));
                                }
                                else
                                {
                                    if (tuioObject.Bounds.HasChanged(posX,
                                        posY, angle, width, height, area, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration))
                                    {
                                        updatedTuioObjects.Add(tuioObject);
                                        tuioObject.Bounds.Update(_prevFrameTime, posX,
                                            posY, angle, width, height, area, velocityX, velocityY, angleVelocity, acceleration, rotationAcceleration);
                                    }
                                }
                            }
                        }
                        else if (otherOscMessage.Address == "/tuio2/sym")
                        {
                            var sessionId = (uint)(int)otherOscMessage.Values[0];
                            if (aliveSIds.Contains(sessionId))
                            {
                                var tuId = (uint)(int)otherOscMessage.Values[1];
                                var componentId = (uint)(int)otherOscMessage.Values[2];
                                string group = (string)otherOscMessage.Values[3];
                                string data = (string)otherOscMessage.Values[4];

                                var tuioObject = _tuioObjects[sessionId];
                                if (tuioObject.Symbol == null)
                                {
                                    addedTuioObjects.Add(tuioObject);
                                    tuioObject.SetTuioSymbol(new Tuio20Symbol(_prevFrameTime, tuioObject, tuId, componentId, 
                                        group, data));
                                }
                                else
                                {
                                    if (tuioObject.Symbol.HasChanged(tuId, componentId, group,
                                        data))
                                    {
                                        updatedTuioObjects.Add(tuioObject);
                                        tuioObject.Symbol.Update(_prevFrameTime, tuId, componentId, group,
                                            data);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var tuioObject in addedTuioObjects)
                {
                    foreach (var tuioListener in _tuioListeners)
                    {
                        tuioListener.TuioAdd(tuioObject);
                    }
                }

                foreach (var tuioObject in updatedTuioObjects)
                {
                    foreach (var tuioListener in _tuioListeners)
                    {
                        tuioListener.TuioUpdate(tuioObject);
                    }
                }

                foreach (var tuioObject in removedTuioObjects)
                {
                    foreach (var tuioListener in _tuioListeners)
                    {
                        tuioListener.TuioRemove(tuioObject);
                    }
                }
                
                foreach (var tuioListener in _tuioListeners)
                {
                    tuioListener.TuioRefresh(_prevFrameTime);
                }
            }
            _prevFrameTime = currentFrameTime;
            _prevFrameId = frameId;
            _frmMessage = null;
        }
    }
}