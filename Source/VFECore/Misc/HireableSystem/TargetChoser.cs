using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace VFECore.Misc.HireableSystem
{

    public class TargetChoser
    {
        private Map map;
        private int MaxLaunchDistance = 1000;
        private List<IThingHolder> fakePods = [];

        private Action<int, TransportPodsArrivalAction> action;

        public TargetChoser(Map currentMap)
        {
            map = currentMap;
            fakePods.Add(new FakePod());
        }

        private IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile)
        {
            bool anything = false;
            if (TransportPodsArrivalAction_FormCaravan.CanFormCaravanAt(fakePods, tile) && !Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile))
            {
                anything = true;
                yield return new FloatMenuOption("FormCaravanHere".Translate(), delegate
                {
                    action(tile, new TransportPodsArrivalAction_FormCaravan());
                });
            }

            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                if (worldObjects[i].Tile != tile)
                {
                    continue;
                }

                foreach (FloatMenuOption transportPodsFloatMenuOption in worldObjects[i].GetTransportPodsFloatMenuOptions(fakePods, new FakeCompLaunchable(action)))
                {
                    anything = true;
                    yield return transportPodsFloatMenuOption;
                }
            }

            if (!anything && !Find.World.Impassable(tile))
            {
                yield return new FloatMenuOption("TransportPodsContentsWillBeLost".Translate(), delegate
                {
                    action(tile, null);
                });
            }
        }

        public string TargetingLabelGetter(GlobalTargetInfo target, int tile)
        {
            if (!target.IsValid)
            {
                return null;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile, true, int.MaxValue);
            if (MaxLaunchDistance > 0 && num > MaxLaunchDistance)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }
            IEnumerable<FloatMenuOption> source = GetTransportPodsFloatMenuOptionsAt(target.Tile);
            if (!source.Any<FloatMenuOption>())
            {
                return string.Empty;
            }
            if (source.Count<FloatMenuOption>() == 1)
            {
                if (source.First<FloatMenuOption>().Disabled)
                {
                    GUI.color = ColorLibrary.RedReadable;
                }
                return source.First<FloatMenuOption>().Label;
            }
            MapParent mapParent;
            if ((mapParent = (target.WorldObject as MapParent)) != null)
            {
                return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
            }
            return "ClickToSeeAvailableOrders_Empty".Translate();
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target, int tile, int maxLaunchDistance, Action<int, TransportPodsArrivalAction> launchAction)
        {
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
            if (maxLaunchDistance > 0 && num > maxLaunchDistance)
            {
                Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            IEnumerable<FloatMenuOption> source = GetTransportPodsFloatMenuOptionsAt(target.Tile);
            if (!source.Any())
            {
                if (Find.World.Impassable(target.Tile))
                {
                    Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }

                launchAction(target.Tile, null);
                return true;
            }

            if (source.Count() == 1)
            {
                if (!source.First().Disabled)
                {
                    source.First().action();
                    return true;
                }

                return false;
            }

            Find.WindowStack.Add(new FloatMenu(source.ToList()));
            return false;
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            return ChoseWorldTarget(target, this.map.Tile, this.MaxLaunchDistance, action);
        }

        public void StartChoosingDestination(Action<int, TransportPodsArrivalAction> action)
        {
            int tile = this.map.Tile;
            this.action = action;

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(map.Parent), CameraJumper.MovementMode.Pan);
            Find.WorldSelector.ClearSelection();


            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompLaunchable.TargeterMouseAttachment, true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistance);

            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile), null);
        }
    }

    public class FakeCompLaunchable : CompLaunchable
    {
        private ThingWithComps fakeThing = new ThingWithComps();
        public Action<int, TransportPodsArrivalAction> action;
        public FakeCompLaunchable(Action<int, TransportPodsArrivalAction> action)
        {
            parent = fakeThing;
            this.action = action;
        }
    }


    [HarmonyPatch(typeof(CompLaunchable), "TryLaunch")]
    public class CompLaunchable_TryLaunch_Patch
    {
        public static bool Prefix(CompLaunchable __instance, int destinationTile, TransportPodsArrivalAction arrivalAction)
        {
            if (__instance is FakeCompLaunchable fakeCompLaunchable)
            {
                fakeCompLaunchable.action(destinationTile, arrivalAction);
                return false;
            }

            return true;
        }
    }

    public class FakePod : IThingHolder
    {
        ThingOwner innerContainer;

        public FakePod()
        {
            innerContainer = new ThingOwner<Thing>(this);

            // Fake a colonist inside the pod so we get the Caravan / Attck options

            innerContainer.TryAdd(PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true)));
        }

        public IThingHolder ParentHolder => null;

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren) { }
    }
}
