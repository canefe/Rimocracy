﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimocracy
{
    public class Consideration
    {
        public static readonly Consideration always = new Consideration();

        public static readonly Consideration never = new Consideration() { inverted = true };

        static string indent = "";

        const string indentSymbol = "  ";

        public string label;
        public float support;

        bool inverted = false;

        List<Consideration> all = new List<Consideration>();
        List<Consideration> any = new List<Consideration>();

        SuccessionDef succession;
        bool? leaderExists;
        TermDuration termDuration = TermDuration.Undefined;
        bool? campaigning;
        ValueOperations governance;
        ValueOperations regime;
        ValueOperations population;
        ValueOperations daysOfFood;
        List<TechLevel> techLevels = new List<TechLevel>();
        string modActive;
        string decision;

        bool? isLeader;
        bool? isTarget;
        TraitDef trait;
        List<SkillOperations> skills = new List<SkillOperations>();
        bool? isCapableOfViolence;
        ValueOperations medianOpinionOfMe;
        ValueOperations age;
        ValueOperations titleSeniority;
        bool? primaryIdeoligion;
        string meme;
        string precept;
        ValueOperations ideoCertainty;

        bool? targetIsColonist;
        bool? targetIsLeader;
        bool? targetInAggroMentalState;
        bool? targetShareIdeoligion;
        TraitDef targetTrait;
        ValueOperations opinionOfTarget;
        ValueOperations medianOpinionOfTarget;
        ValueOperations targetAge;
        ValueOperations targetFactionGoodwill;

        /// <summary>
        /// Returns true if this requirement is default
        /// </summary>
        public bool IsTrivial =>
            !inverted
            && all.NullOrEmpty()
            && any.NullOrEmpty()
            && succession == null
            && leaderExists == null
            && termDuration == TermDuration.Undefined
            && campaigning == null
            && governance == null
            && regime == null
            && population == null
            && daysOfFood == null
            && techLevels.EnumerableNullOrEmpty()
            && modActive.NullOrEmpty()
            && decision == null
            && isLeader == null
            && isTarget == null
            && trait == null
            && skills.NullOrEmpty()
            && isCapableOfViolence == null
            && medianOpinionOfMe == null
            && age == null
            && titleSeniority == null
            && precept.NullOrEmpty()
            && ideoCertainty == null
            && targetIsColonist == null
            && targetIsLeader == null
            && targetInAggroMentalState == null
            && targetTrait == null
            && medianOpinionOfTarget == null
            && medianOpinionOfTarget == null
            && targetAge == null
            && targetFactionGoodwill == null
            && primaryIdeoligion == null;

        public static implicit operator bool(Consideration consideration) => consideration.IsSatisfied(target: Utility.RimocracyComp.Leader);

        public bool IsSatisfied(Pawn pawn = null, Pawn target = null)
        {
            bool res = true;

            if (succession != null)
                res &= Utility.RimocracyComp.SuccessionType.defName == succession.defName;
            if (res && termDuration != TermDuration.Undefined)
                res &= Utility.RimocracyComp.TermDuration == termDuration;
            if (res && leaderExists != null)
                res &= Utility.RimocracyComp.HasLeader == leaderExists;
            if (res && campaigning != null)
                res &= Utility.RimocracyComp.IsCampaigning == campaigning;
            if (res && governance != null)
                res &= governance.Compare(Utility.RimocracyComp.Governance);
            if (res && regime != null)
                res &= regime.Compare(Utility.RimocracyComp.RegimeFinal);
            if (res && population != null)
                res &= population.Compare(Utility.Population);
            if (res && daysOfFood != null)
                res &= daysOfFood.Compare(Utility.DaysOfFood);
            if (res && techLevels.Any())
                res &= techLevels.Contains(Find.FactionManager.OfPlayer.def.techLevel);
            if (res && !modActive.NullOrEmpty())
                res &= ModsConfig.IsActive(modActive.Contains('.') ? modActive : $"Ludeon.RimWorld.{modActive}");
            if (res && !decision.NullOrEmpty())
                res &= Utility.RimocracyComp.DecisionActive(decision);

            if (res && pawn != null)
            {
                if (isLeader != null)
                    res &= pawn.IsLeader() == isLeader;
                if (res && isTarget != null)
                    res &= (pawn == target) == isTarget;
                if (res && trait != null && pawn?.story?.traits != null)
                    res &= pawn.story.traits.HasTrait(trait);
                if (res && !skills.NullOrEmpty())
                    res &= skills.TrueForAll(so => so.Compare(pawn));
                if (res && isCapableOfViolence != null)
                    res &= pawn.WorkTagIsDisabled(WorkTags.Violent) != isCapableOfViolence;
                if (res && medianOpinionOfMe != null)
                    res &= medianOpinionOfMe.Compare(pawn.MedianCitizensOpinion());
                if (res && age != null && pawn?.ageTracker != null)
                    res &= age.Compare(pawn.ageTracker.AgeBiologicalYears);
                if (res && titleSeniority != null && pawn?.royalty != null)
                    res &= titleSeniority.Compare(pawn.GetTitleSeniority());
            }

            if (res && target != null)
            {
                if (targetIsColonist != null)
                    res &= target.IsColonist == targetIsColonist;
                if (res && targetIsLeader != null)
                    res &= target.IsLeader() == targetIsLeader;
                if (res && targetInAggroMentalState != null)
                    res &= target.InAggroMentalState == targetInAggroMentalState;
                if (res && targetTrait != null && target.story?.traits != null)
                    res &= target.story.traits.HasTrait(targetTrait);
                if (res && opinionOfTarget != null && pawn != null)
                    res &= opinionOfTarget.Compare(pawn.GetOpinionOf(target));
                if (res && medianOpinionOfTarget != null)
                    res &= medianOpinionOfTarget.Compare(target.MedianCitizensOpinion());
                if (res && targetAge != null && target.ageTracker != null)
                    res &= targetAge.Compare(target.ageTracker.AgeBiologicalYears);
                if (res && targetFactionGoodwill != null && target.Faction != null && !target.Faction.IsPlayer)
                    res &= targetFactionGoodwill.Compare(target.Faction.PlayerGoodwill);
                if (res && targetShareIdeoligion != null && target.Ideo != null)
                    res &= (Utility.RimocracyComp.DecisionActive(DecisionDef.StateIdeoligion) ? (target.Ideo == Utility.NationPrimaryIdeo) : true) == targetShareIdeoligion;
            }

            Ideo ideo = pawn?.Ideo ?? Utility.NationPrimaryIdeo;
            if (ideo != null)
            {
                if (res && !meme.NullOrEmpty())
                    res &= ideo.memes.Any(m => m.defName == meme);
                if (res && !precept.NullOrEmpty())
                    res &= ideo.PreceptsListForReading.Any(p => p.def.defName == precept);
                if (res && ideoCertainty != null && pawn?.ideo != null)
                    res &= ideoCertainty.Compare(pawn.ideo.Certainty);
                if (res && primaryIdeoligion != null && ideo != null)
                    res &= (ideo == Utility.NationPrimaryIdeo) == primaryIdeoligion;
            }

            if (res && !all.NullOrEmpty())
                res &= all.All(r => r.IsSatisfied(pawn, target));
            if (res && !any.NullOrEmpty())
                res &= any.Any(r => r.IsSatisfied(pawn, target));
            return res ^ inverted;
        }

        public (float support, TaggedString explanation) GetSupportAndExplanation(Pawn pawn, Pawn target)
        {
            float s = GetSupport(pawn, target);
            return (s, s != 0 ? $"{label.Formatted(pawn.Named("PAWN"), target.Named("TARGET")).Resolve().CapitalizeFirst()}: {s.ToStringWithSign("0")}" : null);
        }

        public float GetSupport(Pawn pawn, Pawn target = null)
        {
            if (!IsSatisfied(pawn, target))
                return 0;
            float s = support;

            governance?.TransformValue(Utility.RimocracyComp.Governance, ref s);
            regime?.TransformValue(Utility.RimocracyComp.RegimeFinal, ref s);
            population?.TransformValue(Utility.Population, ref s);
            daysOfFood?.TransformValue(Utility.DaysOfFood, ref s);
            foreach (SkillOperations so in skills)
                so.TransformValue(pawn, ref s);
            medianOpinionOfMe?.TransformValue(pawn.MedianCitizensOpinion(), ref s);
            if (pawn?.ageTracker != null)
                age?.TransformValue(pawn.ageTracker.AgeBiologicalYears, ref s);
            if (pawn?.royalty != null)
                titleSeniority?.TransformValue(pawn.GetTitleSeniority(), ref s);
            if (ModsConfig.IdeologyActive && pawn?.ideo != null)
                ideoCertainty?.TransformValue(pawn.ideo.Certainty, ref s);
            if (target != null)
            {
                opinionOfTarget?.TransformValue(pawn.GetOpinionOf(target), ref s);
                medianOpinionOfTarget?.TransformValue(target.MedianCitizensOpinion(), ref s);
                if (target.ageTracker != null)
                    targetAge?.TransformValue(target.ageTracker.AgeBiologicalYears, ref s);
                if (target.Faction != null && !target.Faction.IsPlayer)
                    targetFactionGoodwill?.TransformValue(target.Faction.PlayerGoodwill, ref s);
            }
            return s;
        }

        public string ToString(string pawn = null, string target = null)
        {
            void AddIndent() => indent += indentSymbol;

            void RemoveIndent() => indent = indent.Remove(0, indentSymbol.Length);

            string res = "";

            void AddLine(string text) => res += $"{indent}{text}\n";

            if (pawn.NullOrEmpty())
                pawn = "the pawn";
            if (target.NullOrEmpty())
                target = "the target";

            if (inverted)
            {
                AddLine("The following must be FALSE:");
                AddIndent();
            }

            if (succession != null)
                AddLine($"Succession law: {succession.LabelCap}");
            if (leaderExists != null)
                AddLine($"Leader {((bool)leaderExists ? "exists" : "doesn't exist")}");
            if (termDuration != TermDuration.Undefined)
                AddLine($"Term duration: {termDuration}");
            if (campaigning != null)
                AddLine($"Campaign is {((bool)campaigning ? "on" : "off")}");
            if (governance != null)
                AddLine(governance.ToString("Governance", "P0"));
            if (regime != null)
                AddLine(regime.ToString("Regime (democracy)", "P0"));
            if (population != null)
                AddLine(population.ToString("Population"));
            if (daysOfFood != null)
                AddLine(daysOfFood.ToString("Days worth of food"));
            if (techLevels.Any())
                AddLine($"Tech level: {techLevels.Select(techLevel => techLevel.ToStringHuman()).ToCommaList()}");
            if (!modActive.NullOrEmpty())
                AddLine($"DLC/mod active: {modActive}");
            if (!decision.NullOrEmpty())
                AddLine($"{GenText.SplitCamelCase(decision)} is active");

            if (isLeader != null)
                AddLine($"{pawn.CapitalizeFirst()} is {((bool)isLeader ? $"" :"not ")}the leader");
            if (isTarget != null)
                AddLine($"{pawn.CapitalizeFirst()} is {((bool)isTarget ? $"" : $"not ")}the target");
            if (trait != null)
                AddLine($"{pawn.CapitalizeFirst()} has trait {trait}");
            if (!skills.EnumerableNullOrEmpty())
                foreach (SkillOperations skill in skills)
                    AddLine(skill.ToString(skill.skill.LabelCap));
            if (isCapableOfViolence != null)
                AddLine($"{pawn.CapitalizeFirst()} is {((bool)isCapableOfViolence ? $"" : "in")}capable of violence");
            if (medianOpinionOfMe != null)
                AddLine(medianOpinionOfMe.ToString($"Median citizens' opinion of {pawn}"));
            if (age != null)
                AddLine(age.ToString($"{pawn.CapitalizeFirst()}'s age"));
            if (titleSeniority != null)
                AddLine(titleSeniority.ToString($"{pawn.CapitalizeFirst()}'s title seniority"));
            if (ModsConfig.IdeologyActive)
            {
                if (!meme.NullOrEmpty())
                {
                    MemeDef m = DefDatabase<MemeDef>.GetNamed(meme, false);
                    if (m != null)
                        AddLine($"Ideoligion has '{m.LabelCap}' meme");
                    else Utility.Log($"No MemeDef {meme} found for a consideration.", LogLevel.Error);
                }
                if (!precept.NullOrEmpty())
                {
                    PreceptDef p = DefDatabase<PreceptDef>.GetNamed(precept, false);
                    if (p != null)
                        AddLine($"Ideoligion has '{p.issue.LabelCap}: {p.LabelCap}' precept");
                    else Utility.Log($"No PreceptDef {precept} found for a consideration.", LogLevel.Error);
                }
                if (ideoCertainty != null)
                    AddLine(ideoCertainty.ToString($"{pawn.CapitalizeFirst()}'s certainty in their ideoligion", "P0"));
                if (primaryIdeoligion != null)
                    AddLine($"{pawn.CapitalizeFirst()} {((bool)primaryIdeoligion ? "shares" : "doesn't share")} the primary ideoligion");
            }

            if (targetIsColonist != null)
                AddLine($"{target.CapitalizeFirst()} is {((bool)targetIsColonist ? "" : "not ")}a colonist");
            if (targetIsLeader != null)
                AddLine($"{target.CapitalizeFirst()} is {((bool)targetIsLeader ? "" : "not ")}the leader");
            if (targetInAggroMentalState != null)
                AddLine($"{target.CapitalizeFirst()} is {((bool)targetInAggroMentalState ? "" : "not ")}in an aggressive mental break");
            if (targetTrait != null)
                AddLine($"{target.CapitalizeFirst()} has trait {targetTrait}");
            if (opinionOfTarget != null)
                AddLine($"{opinionOfTarget.ToString($"{pawn.CapitalizeFirst()}'s opinion of {target}")}");
            if (medianOpinionOfTarget != null)
                AddLine(medianOpinionOfTarget.ToString($"Median citizens' opinion of {target}"));
            if (targetAge != null)
                AddLine(targetAge.ToString($"{target.CapitalizeFirst()}'s age"));
            if (targetFactionGoodwill != null)
                AddLine(targetFactionGoodwill.ToString($"Goodwill of {target.CapitalizeFirst()}'s faction"));

            if (!all.NullOrEmpty())
            {
                AddLine("All of the following:");
                AddIndent();
                foreach (Consideration r in all)
                    res += $"{r.ToString(pawn, target)}\n";
                RemoveIndent();
            }
            if (!any.NullOrEmpty())
            {
                AddLine("Any of the following:");
                AddIndent();
                foreach (Consideration r in any)
                    res += $"{r.ToString(pawn, target)}\n";
                RemoveIndent();
            }
            if (inverted)
                RemoveIndent();
            return res.TrimEndNewlines();
        }
    }
}
