using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallRouteSignalPatrolVerification
    {
        [MenuItem("Tetherfall/Verify Route Signal Patrols")]
        public static void VerifyRouteSignalPatrols()
        {
            var root = new GameObject("Route Signal Patrol Verification");
            try
            {
                var game = root.AddComponent<TetherfallGame>();
                var cooledSignal = RouteEvent.Create(
                    game,
                    CreateDefinition("test.route.cooled", new Vector3(60f, 0f, 0f), false));
                RestoreRouteEvents(
                    game,
                    CreateSave(
                        cooledSignal.EventId,
                        completionCount: 2,
                        nextAvailableUtcTicks: DateTime.UtcNow.AddMinutes(4).Ticks));

                Assert(!IsRepeatRouteEventAvailable(game, cooledSignal), "Saved route cooldown was not restored.");
                Assert(cooledSignal.CanInteract(game), "Cooling repeat signal did not expose its status prompt.");
                Assert(
                    cooledSignal.InteractionPrompt.Contains("returns"),
                    "Cooling repeat signal did not show a remaining cooldown.");

                var incompleteQuest = GetQuestLog(game);
                incompleteQuest.Add(new QuestRecord
                {
                    id = "signalbreak",
                    required = 3,
                    current = 1,
                    completed = false
                });

                var cooledStorySignal = RouteEvent.Create(
                    game,
                    CreateDefinition("test.route.story_cooled", new Vector3(2f, 0f, 0f), true));
                var pendingStorySignal = RouteEvent.Create(
                    game,
                    CreateDefinition("test.route.story_pending", new Vector3(28f, 0f, 0f), true));
                RestoreRouteEvents(
                    game,
                    CreateSave(
                        cooledStorySignal.EventId,
                        completionCount: 1,
                        nextAvailableUtcTicks: 0L));

                Assert(
                    game.GetNextRouteEventTarget() == pendingStorySignal.transform,
                    "Signalbreak guidance selected a repeat instead of an unfinished story signal.");

                RestoreRouteEvents(game, CreateLegacySave(cooledStorySignal.EventId));
                Assert(
                    IsRepeatRouteEventAvailable(game, cooledStorySignal),
                    "Legacy completed signal did not migrate to an immediately available patrol.");

                Debug.Log("Route signal patrol verification passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static RouteEventDefinition CreateDefinition(string id, Vector3 position, bool contributesToSignalbreak)
        {
            return new RouteEventDefinition
            {
                id = id,
                displayName = id,
                position = position,
                signalColor = Color.cyan,
                waves = new[] { new[] { "enemy.hollow_raider" } },
                contributesToSignalbreak = contributesToSignalbreak,
                repeatable = true,
                cooldownSeconds = 600f
            };
        }

        private static TetherfallSaveData CreateSave(
            string eventId,
            int completionCount,
            long nextAvailableUtcTicks)
        {
            var data = new TetherfallSaveData();
            data.completedRouteEventIds.Add(eventId);
            data.routeEvents.Add(new RouteEventSaveData
            {
                id = eventId,
                completionCount = completionCount,
                nextAvailableUtcTicks = nextAvailableUtcTicks
            });
            return data;
        }

        private static TetherfallSaveData CreateLegacySave(string eventId)
        {
            var data = new TetherfallSaveData();
            data.completedRouteEventIds.Add(eventId);
            return data;
        }

        private static void RestoreRouteEvents(TetherfallGame game, TetherfallSaveData data)
        {
            var restore = typeof(TetherfallGame).GetMethod(
                "RestoreRouteEventState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (restore == null)
            {
                throw new InvalidOperationException("Route event restore method was unavailable.");
            }

            restore.Invoke(game, new object[] { data });
        }

        private static bool IsRepeatRouteEventAvailable(TetherfallGame game, RouteEvent routeEvent)
        {
            var availability = typeof(TetherfallGame).GetMethod(
                "IsRepeatRouteEventAvailable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (availability == null)
            {
                throw new InvalidOperationException("Route event availability method was unavailable.");
            }

            return (bool)availability.Invoke(game, new object[] { routeEvent });
        }

        private static List<QuestRecord> GetQuestLog(TetherfallGame game)
        {
            var field = typeof(TetherfallGame).GetField(
                "questLog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(game) is List<QuestRecord> questLog)
            {
                return questLog;
            }

            throw new InvalidOperationException("Quest log was unavailable.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
