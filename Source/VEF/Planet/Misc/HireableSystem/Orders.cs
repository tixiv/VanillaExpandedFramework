using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;

namespace VEF.Planet
{
    public class Orders
    {
        public enum Commands
        {
            FormCaravan,
            GiveToCaravan,
            AttackAndDropAtEdge,
            AttackAndDropInCenter,
            SiteDropAtEdge,
            SiteDropInCenter,
            LandInExistingMap,
            ConvertSavegame,
        }

        public Commands Command;
        public IntVec3? Cell;
        public int WorldTile;
        public WorldObject WorldObject;

        public static Orders LandInExistingMap(WorldObject worldObject, IntVec3? cell = null)
        {
            return new Orders
            {
                Command = Commands.LandInExistingMap,
                WorldObject = worldObject,
                Cell = cell
            };
        }

        public static Orders WithWorldObject(Commands command, WorldObject worldObject)
        {
            return new Orders
            {
                Command = command,
                WorldObject = worldObject
            };
        }

        public static Orders FormCaravan(int worldTile)
        {
            return new Orders
            {
                Command = Commands.FormCaravan,
                WorldTile = worldTile
            };
        }

        public static Orders ConvertSavegame()
        {
            return new Orders
            {
                Command = Commands.ConvertSavegame
            };
        }
    }
}
