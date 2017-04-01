using Microsoft.Xna.Framework;
using StardewValley;
using StardewValleyMP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewValleyMP.States
{
    // These are used to monitor a specific type for significant changes, and notify
    // everyone else if so. They're used in LocationCache.

    public interface Monitor
    {
        void addCache(Vector2 pos, object obj);
        void removeCache(Vector2 pos);

        void recheck(Vector2 pos);
        void check();
    }

    // Originally I wrote these as individual functions inside LocationCache, a version for HoeDirt
    // and a version for FruitTree. Then I realized I would need to do the same sort of thing for
    // Objects needing updating, so I made this. That class was getting a bit cluttery.
    //
    // And once again, this would be much easier with templates :P
    // This having to pass function wrappers thing just for a constructor with parameters is so weird.
    public class SpecificMonitor< BASETYPE, TYPE, STATE, PACKET > : Monitor
        where BASETYPE : class
        where TYPE : class, BASETYPE
        where PACKET : Packet
        where STATE : State
    {
        protected readonly LocationCache loc;
        private Dictionary<Vector2, BASETYPE> container;
        protected Dictionary<Vector2, STATE> cache = new Dictionary<Vector2, STATE>();
        public readonly Func<TYPE, STATE> makeState;
        public readonly Func<GameLocation, Vector2, PACKET> makePacket;

        public SpecificMonitor(LocationCache theLoc, Dictionary<Vector2, BASETYPE> theContainer,
                        Func< TYPE, STATE > stateFunc, Func< GameLocation, Vector2, PACKET > packetFunc )
        {
            loc = theLoc;
            container = theContainer;
            makeState = stateFunc;
            makePacket = packetFunc;
        }

        public void addCache( Vector2 pos, object obj )
        {
            if ( obj == null || obj.GetType() != typeof( TYPE ) )
            {
                Log.error("Bad object given to addCache: " + obj + " (wanted " + typeof( TYPE ) + ")");
                return;
            }

            TYPE realObj = (TYPE)obj;
            if (cache.ContainsKey(pos))
                cache[pos] = makeState(realObj);
            else
                cache.Add(pos, makeState(realObj));
        }

        public void removeCache( Vector2 pos )
        {
            if ( cache.ContainsKey( pos ) )
                cache.Remove(pos);
        }

        public void recheck( Vector2 pos )
        {
            // C# is weird.
            STATE state = default(STATE);
            if (cache.ContainsKey(pos))
                state = cache[pos];
            BASETYPE tf = default(BASETYPE);
            if (container.ContainsKey(pos))
                tf = container[pos];

            if (tf == null || tf.GetType() != typeof(TYPE))
            {
                // This is based on the original usage when it was in LocationCache. The parts that
                // modify the container would modify the cache as well (and they still do this).
                // When I was working on this at first I had missed a couple spots and so it would
                // detect this stuff during check() and yell at me for changing things during
                // iteration.  So I'm leaving this here in case I break something again later on.
                Log.error("REMOVE SHOULDN'T HAPPEN " + new System.Diagnostics.StackTrace());
                cache.Remove(pos);
                return;
            }
            TYPE dirt = tf as TYPE;

            STATE newState = makeState( dirt );
            if (state == null)
            {
                Log.error("ADD SHOULDN'T HAPPEN " + new System.Diagnostics.StackTrace());
                cache.Add(pos, newState);
            }
            else
            {
                if (newState.isDifferentEnoughFromOldStateToSend(state))//if (state != newState)
                {
                    if (!loc.ignoreUpdates) Multiplayer.sendFunc(makePacket(loc.loc, pos));
                    cache[pos] = newState;
                }
            }
        }

        public void check()
        {
            foreach (Vector2 pos in cache.Keys.ToArray())
            {
                recheck(pos);
            }
        }
    }
}
