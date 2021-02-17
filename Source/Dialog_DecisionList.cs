﻿using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Rimocracy
{
    /// <summary>
    /// Dialog window with the list of possible decisions
    /// </summary>
    public class Dialog_DecisionList : Window
    {
        Vector2 scrollPosition = new Vector2();
        Rect viewRect = new Rect();

        DecisionDef decisionToShowVoteDetails;

        public Dialog_DecisionList()
        {
            doCloseX = true;
            closeOnClickedOutside = true;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            viewRect.width = inRect.width - GenUI.ScrollBarWidth;
            Listing_Standard content = new Listing_Standard();
            content.BeginScrollView(inRect.AtZero(), ref scrollPosition, ref viewRect);

            content.Label($"Succession type: {Utility.RimocracyComp.SuccessionType.LabelCap}", tooltip: Utility.RimocracyComp.SuccessionType.description);
            content.Label($"Leader's term: {Utility.RimocracyComp.TermDuration}");

            if (Utility.RimocracyComp.Decisions.Count > 0)
            {
                content.Label("Active decisions:");
                foreach (Decision decision in Utility.RimocracyComp.Decisions)
                    content.Label($"- {decision.def.LabelCap}{(decision.def.Expiration != int.MaxValue ? $" (expires in {(decision.expiration - Find.TickManager.TicksAbs).ToStringTicksToPeriod()})" : "")}", tooltip: decision.def.description);
            }

            // Display regime type
            if (Utility.RimocracyComp.RegimeFinal != 0)
                content.Label($"The current regime is {Math.Abs(Utility.RimocracyComp.RegimeFinal).ToStringPercent()} {(Utility.RimocracyComp.RegimeFinal > 0 ? "democratic" : "authoritarian")}.");
            else content.Label("The current regime is neither democratic nor authoritarian.");

            content.GapLine();

            // Display decision categories and available decisions
            foreach (IGrouping<DecisionCategoryDef, DecisionDef> group in DefDatabase<DecisionDef>.AllDefs
                .Where(def => def.IsDisplayable)
                .GroupBy(def => def.category)
                .OrderBy(group => group.Key.displayOrder))
            {
                content.Gap();
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                content.Label(group.Key.label);
                Text.Font = GameFont.Small;

                foreach (DecisionDef d in group.OrderBy(def => def.displayPriorityInCategory))
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    content.Label(d.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;
                    content.Label(d.description);
                    if (d.governanceCost != 0)
                        content.Label($"Will reduce Governance by {d.GovernanceCost.ToStringPercent()}.");
                    if (d.regimeEffect != 0)
                        content.Label($"Will move the regime {Math.Abs(d.regimeEffect).ToStringPercent()} towards {(d.regimeEffect > 0 ? "democracy" : "authoritarianism")}.");
                    if (!d.effectRequirements.IsTrivial)
                        content.Label($"Requirements:\n{d.effectRequirements}");

                    DecisionVoteResults votingResult = d.GetVotingResults();
                    switch (d.enactment)
                    {
                        case DecisionEnactmentRule.Decree:
                            if (votingResult.Count > 0)
                                content.Label($"Leader's support: {votingResult.First().support.ToStringWithSign("0")}", tooltip: votingResult.First().explanation);
                            break;

                        case DecisionEnactmentRule.Law:
                        case DecisionEnactmentRule.Referendum:
                            if (content.ButtonTextLabeled($"Support: {votingResult.Yea} - {votingResult.Nay}", decisionToShowVoteDetails == d ? "Hide Details" : "Show Details"))
                                if (decisionToShowVoteDetails != d)
                                    decisionToShowVoteDetails = d;
                                else decisionToShowVoteDetails = null;
                            if (decisionToShowVoteDetails == d)
                                foreach (PawnDecisionOpinion opinion in votingResult)
                                    content.Label($"  {opinion.voter.NameShortColored}: {opinion.support.ToStringWithSign("0")}", tooltip: opinion.explanation);
                            break;
                    }

                    // Display Activate button for valid decisions
                    if (d.IsValid && d.IsPassed(votingResult))
                    {
                        if (content.ButtonText("Activate"))
                        {
                            Utility.Log($"Activating {d.defName}.");
                            if (d.allCitizensReact && d.enactment != DecisionEnactmentRule.Referendum)
                                votingResult = d.GetVotingResults(Utility.Citizens.ToList());
                            if (d.Activate())
                            {
                                Utility.Log($"Logging opinions for {votingResult.Count} citizens.");
                                foreach (PawnDecisionOpinion opinion in votingResult.Where(opinion => opinion.support != 0))
                                {
                                    Utility.Log($"{opinion.voter}'s opinion is {opinion.support}.");
                                    opinion.voter.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(RimocracyDefOf.DecisionMade, opinion.support > 0 ? 1 : 0));
                                }
                                Find.LetterStack.ReceiveLetter($"{d.LabelCap} Decision Taken", d.description, LetterDefOf.NeutralEvent, null);
                            }
                            else Messages.Message($"Could not take {d.label} decision: requirements are not met.", MessageTypeDefOf.NegativeEvent, false);
                            Close();
                        }
                    }
                    else content.Label("Requirements are not met.");
                    content.GapLine();
                }
            }
            content.EndScrollView(ref viewRect);
        }
    }
}
