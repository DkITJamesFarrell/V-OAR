using System;
using System.Collections.Generic;
using UnityEngine;
using static PlayerController;

// Code adapted from the Unity Standard Assets WaypointProgressTracker class
namespace UnityStandardAssets.Utility
{
    public class RouteFollower : MonoBehaviour
    {
        [SerializeField] public Route route;
        [SerializeField] public Route[] routes;
        [HideInInspector] public Route.RouteType routeType; //Here simply to dictate type of route

        [SerializeField] private float translationalVelocity = 5f;
        [SerializeField] private float translationVelocityFactor = .1f;

        [SerializeField] private float rotationalVelocity = 10;
        [SerializeField] private float rotationalVelocityFactor = .2f;
        [SerializeField] private float minimumThreshold = 1f;

        [SerializeField] private Transform target;

        [HideInInspector] public int straightRouteTargetPoint;
        [HideInInspector] public int lastIndex;
    
        [HideInInspector] public float progressAlongRoute;
        [HideInInspector] public int numberOfLaps;
        [HideInInspector] public int currentLap;
        
        [HideInInspector] public Route.RoutePoint targetPoint { get; private set; }
        [HideInInspector] public Route.RoutePoint speedPoint { get; private set; }
        [HideInInspector] public Route.RoutePoint progressPoint { get; private set; }

        private PlayerController playerController;
        private GhostController ghostController;
        private Vector3 previousPosition;

        private bool halfPointReached;
        private float speed;

        private void Awake()
        {
            routes = FindObjectsOfType<Route>();
            route = routes[0];
        }
        
        private void Start()
        {
            if (GetComponent<PlayerController>() != null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (GetComponent<GhostController>() != null)
            {
                ghostController = GetComponent<GhostController>();
            }

            previousPosition = transform.position;

            // we use a transform to represent the point to aim for, and the point which
            // is considered for upcoming changes-of-speed. This allows this component
            // to communicate this information to the AI without requiring further dependencies.

            // you can manually create a transform and assign it to this component *and* the AI,
            // then this component will update it, and the AI can read it.
            if (target == null)
            {
                target = new GameObject(name + " Waypoint Target").transform;
            }

            Reset();
        }

        private void FixedUpdate()
        {
            CheckIfLapComplete();

            if (playerController != null && playerController.Paused()) return;

            if (ghostController != null && ghostController.Paused()) return;

            if (route == null) return;
            
            if (route.GetRoutePoint(0).Equals(null)) return;
             
            // determine the position we should currently be aiming for
            // (this is different to the current progress position, it is a a certain amount ahead along the route)
            // we use lerp as a simple way of smoothing out the speed over time.
            if (Time.fixedDeltaTime > 0)
            {
                UpdatePosition();

                if (routeType == Route.RouteType.LoopedTrack && !halfPointReached)
                {
                    // Check if half point reached
                    CheckIfHalfPointReached();
                }
                //else if(routeType == Route.RouteType.LinearTrack)
                //{

                //}
            }
        }

        public void Reset()
        {
            progressAlongRoute = 0;
            currentLap = 1;
            speed = 0;

            ///////////////////////////////////////if (progressStyle == ProgressStyle.PointToPoint)
            //{
            //    target.position = route.Waypoints[(progressAlongRoute)].position;
            //    target.rotation = route.Waypoints[progressAlongRoute].rotation;
            //}
        }

        public void UpdateVelocity(float velocity)
        {
            this.translationalVelocity = velocity;
        }

        public void UpdateRoute(Route route, int numberOfLaps)
        {
            this.route = route;
            this.numberOfLaps = numberOfLaps;
            this.routeType = route.routeType;
 
            Reset();
            UpdatePosition();
        }

        public void UpdateLastNodeIndex(int lastNodeIndex)
        {
            lastIndex = lastNodeIndex;
            straightRouteTargetPoint = lastNodeIndex;
        }

        private float translation;
        private Quaternion rotation;

        public void UpdatePosition()
        {
            // Calculate speed
            speed = Mathf.Lerp(speed, (previousPosition - transform.position).magnitude / Time.fixedDeltaTime, Time.fixedDeltaTime);

            // Calculate translation
            translation = progressAlongRoute + translationalVelocity + translationVelocityFactor * speed;

            // Calculate rotation
            rotation = Quaternion.LookRotation(
                    route.GetRoutePoint(progressAlongRoute + rotationalVelocity + rotationalVelocityFactor * speed).direction
                );

            // Update position
            target.position = route.GetRoutePoint(translation).position;

            // Update rotation
            target.rotation = rotation;

            // Calculate progress along route
            progressPoint = route.GetRoutePoint(progressAlongRoute);
            Vector3 progressDelta = progressPoint.position - transform.position;
            if (Vector3.Dot(progressDelta, progressPoint.direction) < 0)
            {
                progressAlongRoute += progressDelta.magnitude * 0.5f;
            }

            previousPosition = transform.position;
        }

        public bool CheckIfPointReached(Transform point)
        {
            if (playerController != null)
            {
                // Only check for players who are currently participating in a race or time trial
                if (playerController.state != PlayerState.ParticipatingInRace && playerController.state != PlayerState.ParticipatingInTrial) return false;

                // Return true if distance to point is less than the minimum threshold
                return (Vector3.Distance(transform.position, point.position) < minimumThreshold);
            }

            else if (ghostController != null)
            {
                // Return true if distance to point is less than the minimum threshold
                return (Vector3.Distance(transform.position, point.position) < minimumThreshold);
            }

            return false;
        }

        private void CheckIfHalfPointReached()
        {
            if (playerController != null)
            {

                // If player participating in race
                if (playerController.state.Equals(PlayerState.ParticipatingInRace))
                {
                    // True if the player is near the halfway point
                    halfPointReached = (
                        Vector3.Distance(
                            transform.position,
                            route.waypointList.items[route.waypointList.items.Length / 2].position
                        ) < minimumThreshold
                    );
                }

                //// If player participating in race
                //if (playerController.state.Equals(PlayerState.ParticipatingInRace))
                //{
                //    // True if the player is near the halfway point
                //    halfPointReached = (
                //        Vector3.Distance(
                //            transform.position, 
                //            playerController.race.route.waypointList.items[playerController.race.route.waypointList.items.Length / 2].position
                //        ) < minimumThreshold
                //    );
                //}

                //// Else if player participating in trial
                //else if (playerController.state.Equals(PlayerState.ParticipatingInTrial))
                //{
                //    // True if the player is near the halfway point
                //    halfPointReached = (
                //        Vector3.Distance(
                //            transform.position,
                //            playerController.trial.route.waypointList.items[playerController.trial.route.waypointList.items.Length / 2].position
                //        ) < minimumThreshold
                //    );
                //}
            }

            else if (ghostController != null)
            {
                // True if the ghost is near the halfway point
                halfPointReached = (
                    Vector3.Distance(
                        transform.position,
                        ghostController.trial.route.waypointList.items[ghostController.trial.route.waypointList.items.Length / 2].position
                    ) < minimumThreshold
                );
            }
        }

        private void CheckIfLapComplete()
        {
            // If half point reached
            if (halfPointReached)
            {
                float distance = 0;

                if (playerController != null)
                {
                    // If player participating in race
                    if (playerController.state.Equals(PlayerState.ParticipatingInRace))
                    {
                        // Calculate distance to finish line
                        // Assumes finish line is at the the start point
                        distance =
                            Vector3.Distance(
                                transform.position,
                                playerController.race.route.waypointList.items[0].position
                            );
                    }

                    // Else if player participating in trial
                    else if (playerController.state.Equals(PlayerState.ParticipatingInTrial))
                    {
                        // Calculate distance to finish line
                        // Assumes finish line is at the the start point
                        distance =
                            Vector3.Distance(
                                transform.position,
                                playerController.trial.route.waypointList.items[0].position
                            );
                    }
                }

                if (ghostController != null)
                {
                    // Calculate distance to finish line
                    // Assumes finish line is at the the start point
                    distance =
                        Vector3.Distance(
                            transform.position,
                            ghostController.trial.route.waypointList.items[0].position
                        );
                }

                // If within range of finish line
                if (distance < minimumThreshold)
                {
                    // Update lap count
                    UpdateLapCount();
                }
            }
        }

        #region Old Code Straight Route Lap Check
        //private void CheckIfLapComplete()
        //{
        //    if (routeType == Route.RouteType.LinearTrack)
        //    {
        //        CheckIfStraightRouteLapComplete();
        //    }
        //    else if (routeType == Route.RouteType.LoopedTrack)
        //    {
        //        CheckLoopedRouteLapComplete();
        //    }
        //}

        //private void CheckIfStraightRouteLapComplete()
        //{
        //    if (target.GetComponent<PlayerController>().participatingInRace == false && target.GetComponent<PlayerController>().participatingInRace == false) return;

        //    float distance;
        //    if (target.GetComponent<PlayerController>().participatingInRace == true)
        //    {
        //        distance = Vector3.Distance(transform.position, currentRace.track[straightRouteTargetPoint].position);
        //    }
        //    else
        //    {
        //        distance = Vector3.Distance(transform.position, currentTimeTrial.track[straightRouteTargetPoint].position);
        //    }

        //    if (distance < pointToPointThreshold)
        //    {
        //        UpdateEventLapCount();
        //    }
        //}

        //private void CheckLoopedRouteLapComplete()
        //{
        //    // Precaution to avoid this firing off instantly on start of race
        //    if (halfPointOftrackReached == true)
        //    {
        //        float distance;
        //        if (target.GetComponent<PlayerController>().participatingInRace == true)
        //        {
        //            distance = Vector3.Distance(transform.position, currentRace.track[lastIndex].position);
        //        }
        //        else
        //        {
        //            distance = Vector3.Distance(transform.position, currentTimeTrial.track[lastIndex].position);
        //        }

        //        if (distance < pointToPointThreshold)
        //        {
        //            halfPointOftrackReached = false;
        //            UpdateEventLapCount();
        //        }
        //    }
        //}
        #endregion

        private void UpdateLapCount()
        {
            // Reset variable
            halfPointReached = false;

            // If laps remaining
            if (currentLap < numberOfLaps)
            {
                // Update lap count
                currentLap++;
            }

            // Otherwise
            else
            {
                // Complete event
                CompleteEvent();
                Reset();
            }
        }

        private void CompleteEvent()
        {
            if (playerController != null)
            {
                if (playerController.state.Equals(PlayerState.ParticipatingInRace))
                {
                    playerController.state = PlayerState.AtRaceFinishLine;
                    playerController.race.PlayerCompletedRace(playerController);
                }
                else if (playerController.state.Equals(PlayerState.ParticipatingInTrial))
                {
                    playerController.state = PlayerState.CompletedTimeTrial;
                }
            }

            else if (ghostController != null)
            {
                ghostController.Pause();
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireSphere(route.GetRoutePosition(progressAlongRoute), 3);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(target.position, target.position + target.forward);
            }
        }
    }
}
