﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.QuestGen;

namespace VEF.AnimalBehaviours
{



   
    [HarmonyPatch]  
    public static class VanillaExpandedFramework_QuestNode_GetPawnKind_SetVars_CanHandle_Patch
    {
        


        static MethodBase TargetMethod()
        {
            MethodBase method = typeof(QuestNode_GetPawnKind).GetNestedType("<>c__DisplayClass7_0", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod("<GetKindDef>g__CanHandle|1", BindingFlags.Instance | BindingFlags.NonPublic);
            return method;
        }

        public static void Postfix(PawnKindDef animal, ref bool __result)
        {

            
                if (StaticCollectionsClass.questDisabledAnimals.Contains(animal))
                {
                   
                    __result = false;
                }
            
            

        }

    }





}
