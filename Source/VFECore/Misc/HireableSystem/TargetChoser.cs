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
        private Map originalMap;
        private int MaxLaunchDistance = 1000;
        private List<IThingHolder> fakePods = [];

        private Action<string, int, IntVec3, WorldObject> action;
        private Action finishedAction;

        private bool alreadyFinished;

        public static TargetChoser instanceDuringWorldTargeter;

        public TargetChoser(Map originalMap)
        {
            this.originalMap = originalMap;
            fakePods.Add(new FakePod());
        }

        private void targetChosen(string arrivalAction, int worldTile, IntVec3 cell = default, WorldObject worldObject = null)
        {
            action(arrivalAction, worldTile, cell, worldObject);

            TargetingFinished();
        }

        private void TargetingFinished()
        {
            // Please no callbacks anymore
            instanceDuringWorldTargeter = null;

            if (!alreadyFinished)
            {
                alreadyFinished = true;

                // We go to the colony again so it is clear that we are still at the coms console
                CameraJumper.TryHideWorld();
                finishedAction();
            }
        }

        public void TargetingFinishedCallback()
        {
            Log.Message("WorldTargeting finished");

            TargetingFinished();
        }

        private IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile)
        {
            if (!Find.World.Impassable(tile) && !Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile))
            {
                yield return new FloatMenuOption("FormCaravanHere".Translate(), delegate
                {
                    targetChosen("FormCaravan", tile);
                });
            }

            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects)
            {
                if (wo.Tile != tile)
                    continue;

                if (wo is Settlement settlement && !settlement.HasMap && TransportPodsArrivalAction_AttackSettlement.CanAttack(fakePods, settlement))
                {
                    yield return new FloatMenuOption("AttackAndDropAtEdge".Translate(settlement.Label), delegate
                    {
                        targetChosen("AttackAndDropAtEdge", tile, worldObject: wo);
                    });
                    yield return new FloatMenuOption("AttackAndDropInCenter".Translate(settlement.Label), delegate
                    {
                        targetChosen("AttackAndDropInCenter", tile, worldObject: wo);
                    });
                }
                if (wo is Site site && TransportPodsArrivalAction_VisitSite.CanVisit(fakePods, site))
                {
                    yield return new FloatMenuOption("DropAtEdge".Translate(site.Label), delegate
                    {
                        targetChosen("SiteDropAtEdge", tile, worldObject: wo);
                    });
                    yield return new FloatMenuOption("DropInCenter".Translate(site.Label), delegate
                    {
                        targetChosen("SiteDropInCenter", tile, worldObject: wo);
                    });
                }
                if (wo is MapParent mapParent && mapParent.HasMap && TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(fakePods, mapParent))
                {
                    yield return new FloatMenuOption("LandInExistingMap".Translate(mapParent.Label), delegate ()
                    {
                        instanceDuringWorldTargeter = null; // we are map targeting now
                        Current.Game.CurrentMap = mapParent.Map;
                        CameraJumper.TryHideWorld();
                        TargetingParameters targetParams = TargetingParameters.ForDropPodsDestination();
                        void action(LocalTargetInfo x)
                        {
                            targetChosen("LandInExistingMap", tile, x.Cell, wo);
                        }

                        void actionWhenFinished()
                        {
                            if (Find.Maps.Contains(originalMap))
                            {
                                Current.Game.CurrentMap = originalMap;
                            }
                            TargetingFinished();
                        }

                        Find.Targeter.BeginTargeting(targetParams, action, null, actionWhenFinished, CompLaunchable.TargeterMouseAttachment, true);
                    });
                }
                if (wo is Caravan caravan && TransportPodsArrivalAction_GiveToCaravan.CanGiveTo(fakePods, caravan))
                {
                    yield return new FloatMenuOption("GiveToCaravan".Translate(caravan.Label), delegate
                    {
                        targetChosen("GiveToCaravan", tile, worldObject: wo);
                    });
                }
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

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            int num = Find.WorldGrid.TraversalDistanceBetween(this.originalMap.Tile, target.Tile);
            if (this.MaxLaunchDistance > 0 && num > this.MaxLaunchDistance)
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
                }
                return false;
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

        public void StartChoosingDestination(Action<string, int, IntVec3, WorldObject> action, Action finishedAction)
        {
            int originTile = this.originalMap.Tile;
            this.action = action;
            this.finishedAction = finishedAction;

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(originalMap.Parent), CameraJumper.MovementMode.Pan);
            Find.WorldSelector.ClearSelection();

            this.alreadyFinished = false;
            instanceDuringWorldTargeter = this;

            Find.WorldTargeter.BeginTargeting(this.ChoseWorldTarget, canTargetTiles: true, mouseAttachment: CompLaunchable.TargeterMouseAttachment, onUpdate: delegate
            {
                GenDraw.DrawWorldRadiusRing(originTile, this.MaxLaunchDistance);

            }, extraLabelGetter: (GlobalTargetInfo target) => TargetingLabelGetter(target, originTile));
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), "StopTargeting")]
    public class WorldTargeter_StopTargeting_Patch
    {
        public static void Postfix(WorldTargeter __instance)
        {
            if (TargetChoser.instanceDuringWorldTargeter != null)
            {
                TargetChoser.instanceDuringWorldTargeter.TargetingFinishedCallback();
            }
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
