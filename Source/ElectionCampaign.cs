﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimocracy
{
    public class ElectionCampaign : IExposable
    {
        // How often campaign updates take place; must be multiple of 250
        internal const int CampaignInterval = 2000;

        Pawn candidate;
        SkillDef focusSkill;
        List<Pawn> supporters = new List<Pawn>();

        public Pawn Candidate => candidate;

        public SkillDef FocusSkill
        {
            get => focusSkill;
            set => focusSkill = value;
        }

        public List<Pawn> Supporters
        {
            get => supporters;
            set => supporters = value;
        }

        public ElectionCampaign()
        { }

        public ElectionCampaign(Pawn candidate, SkillDef focusSkill)
        {
            this.candidate = candidate;
            FocusSkill = focusSkill;
            if (candidate != null)
                Supporters.Add(candidate);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref candidate, "candidate");
            Scribe_Defs.Look(ref focusSkill, "focusSkill");
            Scribe_Collections.Look(ref supporters, "supporters", LookMode.Reference);
        }

        public void RareTick()
        {
            if (Find.TickManager.TicksAbs % CampaignInterval != 0)
                return;

            // List of pawns that will leave the campaign
            List<Pawn> defectors = new List<Pawn>();
            // List of pawns that will join the campaign
            HashSet<Pawn> recruits = new HashSet<Pawn>();

            // Preparing a list of potential targets for swaying with randomized weights
            Dictionary<Pawn, float> potentialTargets = Utility.Citizens
                    .Where(p =>
                    !Utility.RimocracyComp.CampaigningCandidates.Contains(p)
                    && !p.InMentalState
                    && !p.Downed
                    && p.needs.mood.thoughts.memories.NumMemoriesOfDef(RimocracyDefOf.PoliticalSympathy) < RimocracyDefOf.PoliticalSympathy.stackLimit)
                    .ToDictionary(p => p, p => Rand.Range(0, ElectionUtility.VoteWeight(p, Candidate) + 100));

            if (Settings.DebugLogging && potentialTargets.Count > 0)
                Utility.Log($"Potential targets for {Candidate}:\r\n{potentialTargets.Select(kvp => $"- {kvp.Key}\t{kvp.Value:N0}").ToLineList()}");

            foreach (Pawn pawn in Supporters.Where(pawn => !pawn.InMentalState && !pawn.Downed))
            {
                // Checking if this pawn should become a defector
                if (pawn != Candidate)
                {
                    float defectionChance = 1 - ElectionUtility.VoteWeight(pawn, candidate) / 100;
                    if (!pawn.IsCitizen() || Rand.Chance(defectionChance) || Utility.RimocracyComp.CampaigningCandidates.MaxBy(p => ElectionUtility.VoteWeight(pawn, p)) != candidate)
                    {
                        Utility.Log($"{pawn} is no longer a core supporter for {candidate}. Their defection chance was {defectionChance:P1}.");
                        defectors.Add(pawn);
                        continue;
                    }
                }

                // Picking a target pawn to sway
                Pawn targetPawn = potentialTargets
                    .Where(kvp => kvp.Key != pawn && kvp.Key.MapHeld == pawn.MapHeld)
                    .MaxByWithFallback(kvp => kvp.Value)
                    .Key;

                if (targetPawn == null)
                    continue;
                Utility.Log($"{pawn} is trying to sway {targetPawn}.");
                float swayChance = pawn.GetStatValue(StatDefOf.SocialImpact) * Settings.SwayChanceFactor * 0.1f;
                Utility.Log($"Sway chance: {swayChance:P1}.");
                if (Rand.Chance(swayChance))
                {
                    Utility.Log("Sway successful!");
                    targetPawn.needs.mood.thoughts.memories.TryGainMemory(RimocracyDefOf.PoliticalSympathy, Candidate);
                    pawn.records.Increment(RimocracyDefOf.VotersSwayed);

                    if (!Utility.RimocracyComp.Campaigns.Any(ec => ec.Supporters.Contains(targetPawn)) && !recruits.Contains(targetPawn))
                    {
                        // If the target pawn is not already a core supporter of any candidate, try to recruit them to the campaign
                        float recruitChance = (ElectionUtility.VoteWeight(targetPawn, Candidate) / 100 - 1) * pawn.GetStatValue(StatDefOf.NegotiationAbility) * Settings.RecruitmentChanceFactor;
                        Utility.Log($"Chance of recruitment: {recruitChance:P1}");
                        if (Rand.Chance(recruitChance))
                        {
                            Utility.Log($"{pawn} successfully recruited {targetPawn} to support {Candidate}.");
                            recruits.Add(targetPawn);
                            pawn.records.Increment(RimocracyDefOf.SupportersRecruited);
                            Messages.Message($"{pawn} recruited {targetPawn} as a supporter of {Candidate}", new LookTargets(targetPawn), MessageTypeDefOf.NeutralEvent);
                        }
                    }
                    else Messages.Message($"{pawn} swayed {targetPawn} in favor of {Candidate}", new LookTargets(targetPawn), MessageTypeDefOf.NeutralEvent);
                }
            }

            // Removing campaign defectors and adding new recruits
            foreach (Pawn p in defectors)
                Supporters.Remove(p);
            Supporters.AddRange(recruits);
        }

        public TaggedString ToTaggedString() =>
            $"{Candidate.NameShortColored}, {FocusSkill?.LabelCap ?? "no"} focus{(Supporters.Count > 1 ? $", {(Supporters.Count - 1).ToStringCached()} core supporters" : "")}";

        public override string ToString() =>
            ToTaggedString().RawText.StripTags();
    }
}
