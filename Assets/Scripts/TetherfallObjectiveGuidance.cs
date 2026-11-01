using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private ObjectiveBeacon objectiveBeacon;
        private Transform objectiveTarget;
        private float nextObjectiveGuidanceAt;

        private void InitializeObjectiveGuidance()
        {
            RefreshObjectiveGuidance();
        }

        private void UpdateObjectiveGuidance()
        {
            if (Time.time < nextObjectiveGuidanceAt)
            {
                return;
            }

            nextObjectiveGuidanceAt = Time.time + 0.3f;
            RefreshObjectiveGuidance();
        }

        private void RefreshObjectiveGuidance()
        {
            objectiveTarget = GetObjectiveTarget();
            if (objectiveTarget == null)
            {
                if (objectiveBeacon != null)
                {
                    objectiveBeacon.SetTarget(null);
                }

                return;
            }

            if (objectiveBeacon == null)
            {
                objectiveBeacon = ObjectiveBeacon.Create(this);
            }

            objectiveBeacon.SetTarget(objectiveTarget);
        }

        private Transform GetObjectiveTarget()
        {
            var bramblet = FindPactCreature("Bramblet");
            if (bramblet == null || !bramblet.IsPacted)
            {
                return bramblet?.transform;
            }

            if (party.Count == 0)
            {
                return FindCompanion("Sera Venn")?.transform;
            }

            var bountyTarget = GetActiveBountyTarget();
            if (bountyTarget != null)
            {
                return bountyTarget;
            }

            var cityActivityTarget = GetActiveCityActivityTarget();
            if (cityActivityTarget != null)
            {
                return cityActivityTarget;
            }

            if (gatheredNodes < 8)
            {
                return null;
            }

            if (!HasClearedMilestoneEncounter(HollowReedMilestoneId))
            {
                return FindCampByPersistenceId(HollowReedMilestoneId)?.transform;
            }

            if (ActiveCamp == null)
            {
                return null;
            }

            if (!hearthgateRestored)
            {
                return finalHearthgate != null ? finalHearthgate.transform : null;
            }

            if (settlementTier < 3)
            {
                return settlement != null ? settlement.transform : null;
            }

            if (!IsWeaponUnlocked(WeaponDiscipline.Riftbow) &&
                !IsWeaponUnlocked(WeaponDiscipline.Threadstaff))
            {
                return GetAlderwakeCraftingStationTarget();
            }

            if (activatedDungeonPylons < 3)
            {
                if (PlayerTransform != null &&
                    PlayerTransform.position.z > 176f &&
                    skyvault != null)
                {
                    var pylon = skyvault.GetGuidanceTarget();
                    if (pylon != null)
                    {
                        return pylon;
                    }
                }

                return GetSkyvaultGateTarget();
            }

            if (!dungeonBossDefeated)
            {
                if (PlayerTransform != null &&
                    PlayerTransform.position.z > 176f &&
                    skyvault != null)
                {
                    var bossTarget = skyvault.GetGuidanceTarget();
                    if (bossTarget != null)
                    {
                        return bossTarget;
                    }
                }

                return activeDungeonBoss != null ? activeDungeonBoss.transform : null;
            }

            var fenlightTarget = GetFenlightObjectiveTarget();
            if (fenlightTarget != null)
            {
                return fenlightTarget;
            }

            var modularDungeonTarget = GetModularDungeonObjectiveTarget();
            if (modularDungeonTarget != null)
            {
                return modularDungeonTarget;
            }

            var cinderwoldTarget = GetCinderwoldArcObjectiveTarget();
            if (cinderwoldTarget != null)
            {
                return cinderwoldTarget;
            }

            if (!IsQuestComplete("signalbreak"))
            {
                return GetNextRouteEventTarget() ?? GetNextFieldCacheTarget();
            }

            if (RecoveredFieldCaches < FieldCacheCatalog.All.Count)
            {
                return GetNextFieldCacheTarget();
            }

            return GetNextRouteEventTarget();
        }

        private EnemyCamp FindCampByPersistenceId(string persistenceId)
        {
            foreach (var camp in enemyCamps)
            {
                if (camp != null && camp.PersistenceId == persistenceId)
                {
                    return camp;
                }
            }

            return null;
        }

        private string GetObjectiveNavigationText()
        {
            if (objectiveTarget == null || PlayerTransform == null)
            {
                return $"Pacts {pacts.Count}   Camps {clearedCamps}";
            }

            var direction = objectiveTarget.position - PlayerTransform.position;
            direction.y = 0f;
            var distance = direction.magnitude;
            if (distance <= 9f)
            {
                return "Threadglass beacon nearby";
            }

            var bearing = CardinalDirection(direction);
            return $"Beacon {Mathf.CeilToInt(distance)}m {bearing}";
        }

        private static string CardinalDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                return string.Empty;
            }

            var angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (angle >= -22.5f && angle < 22.5f)
            {
                return "N";
            }

            if (angle >= 22.5f && angle < 67.5f)
            {
                return "NE";
            }

            if (angle >= 67.5f && angle < 112.5f)
            {
                return "E";
            }

            if (angle >= 112.5f && angle < 157.5f)
            {
                return "SE";
            }

            if (angle >= 157.5f || angle < -157.5f)
            {
                return "S";
            }

            if (angle >= -157.5f && angle < -112.5f)
            {
                return "SW";
            }

            if (angle >= -112.5f && angle < -67.5f)
            {
                return "W";
            }

            return "NW";
        }
    }

    public sealed class ObjectiveBeacon : MonoBehaviour
    {
        private TetherfallGame game;
        private Transform target;
        private Transform ring;
        private Transform crown;
        private Light beaconLight;
        private float phase;

        public static ObjectiveBeacon Create(TetherfallGame game)
        {
            var root = new GameObject("Threadglass objective beacon");
            var beacon = root.AddComponent<ObjectiveBeacon>();
            beacon.game = game;
            beacon.phase = 0.7f;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Objective beacon ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            ring.transform.localScale = new Vector3(0.78f, 0.025f, 0.78f);
            ring.GetComponent<Renderer>().material = game.CreateTransparentMaterial(new Color(0.3f, 0.92f, 0.78f, 0.48f), 0.6f);
            Object.Destroy(ring.GetComponent<Collider>());
            beacon.ring = ring.transform;

            beacon.crown = new GameObject("Objective beacon crown").transform;
            beacon.crown.SetParent(root.transform, false);
            beacon.crown.localPosition = new Vector3(0f, 3.25f, 0f);
            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shard.name = "Objective beacon shard";
                shard.transform.SetParent(beacon.crown, false);
                var angle = index * Mathf.PI * 2f / 3f;
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.34f, 0f, Mathf.Sin(angle) * 0.34f);
                shard.transform.localRotation = Quaternion.Euler(18f, index * 120f, 16f);
                shard.transform.localScale = new Vector3(0.07f, 0.45f, 0.07f);
                shard.GetComponent<Renderer>().material = game.CreateGlowMaterial(new Color(0.34f, 0.96f, 0.82f), 1.75f, 0.7f);
                Object.Destroy(shard.GetComponent<Collider>());
            }

            beacon.beaconLight = root.AddComponent<Light>();
            beacon.beaconLight.type = LightType.Point;
            beacon.beaconLight.color = new Color(0.29f, 0.88f, 0.76f);
            beacon.beaconLight.range = 7.5f;
            beacon.beaconLight.intensity = 0.85f;
            beacon.beaconLight.shadows = LightShadows.None;
            root.SetActive(false);
            return beacon;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            gameObject.SetActive(target != null);
        }

        private void Update()
        {
            if (target == null || game == null || game.PlayerTransform == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var playerDistance = Vector3.Distance(game.PlayerTransform.position, target.position);
            var visible = playerDistance > 8f;
            ring.gameObject.SetActive(true);
            crown.gameObject.SetActive(visible);
            beaconLight.enabled = visible;

            transform.position = target.position;
            crown.localPosition = new Vector3(0f, 3.25f + Mathf.Sin(Time.time * 2.2f + phase) * 0.2f, 0f);
            crown.Rotate(0f, 55f * Time.deltaTime, 0f, Space.Self);
            beaconLight.intensity = 0.8f + Mathf.Sin(Time.time * 2.5f + phase) * 0.16f;
        }
    }
}
