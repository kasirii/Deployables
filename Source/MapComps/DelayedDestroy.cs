using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Deployables
{
    public class DelayedDestroy : MapComponent
    {
        private static readonly List<Thing> toDestroy = new List<Thing>();
        private static readonly List<Thing> toKill = new List<Thing>();
        public DelayedDestroy(Map map) : base(map) { }

        public static void Destroy(Thing t)
        {
            toDestroy.Add(t);
        }
        public static void Kill(Thing t)
        {
            toKill.Add(t);
        }

        public override void MapComponentTick()
        {
            if (toDestroy.Count > 0)
            {
                foreach(var t in toDestroy.ToList())
                {
                    if (t.DestroyedOrNull()) continue;
                    t.Destroy(DestroyMode.Vanish);
                }
                toDestroy.Clear();

            }
            if (toKill.Count > 0)
            {
                foreach (var t in toKill.ToList())
                {
                    if (t.DestroyedOrNull()) continue;
                    t.Kill();
                }
                toKill.Clear();
            }
        }
    }
}