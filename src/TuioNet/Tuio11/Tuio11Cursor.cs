﻿using System.Numerics;
using TuioNet.Common;

namespace TuioNet.Tuio11
{
    public class Tuio11Cursor : Tuio11Container
    {
        /// <summary>
        /// Individual cursor ID assigned to each TuioCursor.
        /// </summary>
        public uint CursorId { get; protected set; }
        
        public Tuio11Cursor(TuioTime startTime, uint sessionId, uint cursorId, Vector2 position, Vector2 velocity, float motionAccel) : base(startTime, sessionId, position, velocity, motionAccel)
        {
            CursorId = cursorId;
        }
        
        internal bool HasChanged(Vector2 position, Vector2 velocity, float motionAccel)
        {
            return !(Position == position && Velocity == velocity && MotionAccel == motionAccel);
        }

        internal void Update(TuioTime currentTime, Vector2 position, Vector2 velocity, float motionAccel)
        {
            var isCalculateSpeeds = (position.X != ((Tuio11Point)this).Position.X && velocity.X == 0) || (position.Y != ((Tuio11Point)this).Position.Y && velocity.Y == 0);
            UpdateContainer(currentTime, position, velocity, motionAccel, isCalculateSpeeds);
        }
    }
}