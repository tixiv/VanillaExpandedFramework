﻿using Verse;

namespace VEF.Weapons
{
    class SpinningLaserGunTurret : SpinningLaserGunBase
    {
        internal Building_LaserGun turret;

        public override void UpdateState()
        {
            if (turret == null) return;

            switch (state)
            {
                case State.Idle:
                    if (turret.BurstWarmupTicksLeft > 0)
                    {
                        state = State.Spinup;
                        ReachRotationSpeed(def.rotationSpeed, turret.BurstWarmupTicksLeft);
                    }
                    break;
                case State.Spinup:
                    if (turret.BurstWarmupTicksLeft == 0 || turret.AttackVerb.state == VerbState.Bursting)
                    {
                        state = State.Spinning;
                    }
                    break;
                case State.Spinning:
                    if (turret.AttackVerb.state != VerbState.Bursting)
                    {
                        state = State.Idle;
                        int ticks = turret.BurstCooldownTicksLeft;
                        ReachRotationSpeed(0, ticks == -1 ? 30 : ticks);
                    }
                    break;
            }
        }
    }
}
