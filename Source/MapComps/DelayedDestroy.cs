using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Deployables
{
    public class DelayedDestroy : MapComponent
    {
        private static readonly List<Thing> toDestroy = new List<Thing>();
        public DelayedDestroy(Map map) : base(map) { }
        public static void Schedule(Thing t)
        {
            toDestroy.Add(t);
        }
        
        public override void MapComponentTick()
        {
            if (toDestroy.Count > 0)
            {
                foreach(var t in toDestroy)
                {
                    t.Destroy(DestroyMode.Vanish);
                }
                toDestroy.Clear();

            }
        }
    }
}