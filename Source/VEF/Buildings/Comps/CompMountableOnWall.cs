﻿using RimWorld;
using Verse;
using System.Collections.Generic;

namespace VEF.Buildings
{
    public class CompProperties_MountableOnWall : CompProperties
    {
        public CompProperties_MountableOnWall()
        {
            this.compClass = typeof(CompMountableOnWall);
        }
    }
    public class CompMountableOnWall : ThingComp
    {

    }
}
