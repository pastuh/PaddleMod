using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRTK;
using WaterBuoyancy;
using MelonLoader;
using System.Collections;
using HarmonyLib;

namespace PaddleMod
{
    public class PaddleModClass : MelonMod
    {

        [HarmonyPatch(typeof(PaddleVelocity))]
        public class PaddleVelocity_Patch
        {

            public static float MyForwardMultiplier = 0.9f;
            public static float MyRotationMultiplier = 0.4f;

            public static float maxDepthPosition = 0.16f;
            public static float maxYBoatSinkPosition; // original boatY - maxDepthY

            public static float depthAdjustSpeed = 0.4f;
            public static float waterYPosition;
            public static float originalYPosition;

            public static bool IsPaddleInWater;
            public static bool isLeftPaddleInWater = false;
            public static bool isRightPaddleInWater = false;
            public static bool isLastPaddleWasLeft = false;
            public static bool isLastPaddleWasRight = false;
            public static bool isLeftPaddleStop = false;
            public static bool isRightPaddleStop = false;

            public static bool isLoggedPaddleHitRc = false;
            public static bool isLoggedPaddleHitLc = false;

            public static Quaternion previousBoatRotation = Quaternion.identity;
            public static Quaternion originalBoatRotation;
            public static Vector3 previousBoatPosition;
            public static Vector3 originalBoatPosition;
            public static float boatRotationDelta = 0f;
            public static float boatSpeed = 0f;
            public static bool boatHasRotatedtoLeft = false;
            public static bool boatHasRotatedtoRight = false;
            public static bool boatHasRolledToLeft = false;
            public static bool boatHasRolledToRight = false;
            public static float boatRotationtHistoryRight;
            public static float boatRotationtHistoryLeft;
            public static bool isBoatBouncedLeft = false;
            public static bool isBoatBouncedRight = false;

            public static float maxRoll = 5f;
            public static float rollSpeed = 5f;

            private static VRTK_InteractableObject paddleController;
            private static Boat boat;


            [HarmonyPatch(typeof(PaddleVelocity), "Awake")] //Exists
            public static void Awake_Patch(PaddleVelocity __instance)
            {
                // Slow down boat
                __instance.ForwardMultiplier = MyForwardMultiplier;
                __instance.RotationMultiplier = MyRotationMultiplier;
            }

            [HarmonyPatch(typeof(PaddleVelocity), "Awake")] //Exists
            internal static class PaddleVelocity_Awake
            {
                public static void Postfix(PaddleVelocity __instance)
                {
                    IsPaddleInWater = false;
                    boat = __instance.GetComponentInParent<Boat>();

                    originalBoatRotation = boat.transform.rotation;
                    originalBoatPosition = boat.transform.position;
                    previousBoatPosition = boat.transform.position;

                    boatRotationtHistoryLeft = boat.transform.rotation.x;
                    boatRotationtHistoryRight = boat.transform.rotation.x;

                    waterYPosition = boat.transform.position.y;
                    maxYBoatSinkPosition = waterYPosition - maxDepthPosition;

                    MelonLogger.Msg($"Boat awake original Y: " + waterYPosition);
                    MelonLogger.Msg($"Boat awake max allowed Y: " + maxYBoatSinkPosition);
                }
            }


            [HarmonyPatch(typeof(PaddleVelocity), "FixedUpdate")] //Exists
            internal static class PaddleVelocity_FixedUpdate
            {
                public static void Postfix(PaddleVelocity __instance)
                {
                    // Constantly update, to get which controller is used
                    paddleController = __instance.GetComponentInParent<VRTK_InteractableObject>();
                    //MelonLogger.Msg($"GRABBED NAME: " + paddleController.name);                 

                    float currentYBoatPosition = boat.transform.position.y;
                    float changedYBoatPosition = waterYPosition - currentYBoatPosition;

                    // Make sure boat will not over sink (Even Paddle is in water)
                    if (currentYBoatPosition <= maxYBoatSinkPosition)
                    {
                        //MelonLogger.Msg($"Preventing sink current Y: " + currentYBoatPosition + " max Y: " + maxYBoatSinkPosition);
                        Vector3 newPos = new Vector3(boat.transform.position.x, waterYPosition, boat.transform.position.z);
                        boat.transform.position = Vector3.Lerp(boat.transform.position, newPos, Time.deltaTime * depthAdjustSpeed);
                    }
                    
                    if (!IsPaddleInWater)
                    {

                        // If paddle not used, go back to surface
                        if ((currentYBoatPosition < waterYPosition))
                        {
                            Vector3 newPos = new Vector3(boat.transform.position.x, waterYPosition, boat.transform.position.z);
                            boat.transform.position = Vector3.Lerp(boat.transform.position, newPos, Time.deltaTime * depthAdjustSpeed);
                            //MelonLogger.Msg($"Boat Rising, to: " + newPos.y);
                        }
                    }


                    /*if (boatHasRolledToLeft)
                    {
                        Quaternion target = Quaternion.Euler(originalBoatRotation.x, originalBoatRotation.y, originalBoatRotation.z);
                        boat.transform.rotation = Quaternion.Slerp(boat.transform.rotation, target, Time.deltaTime);

                        MelonLogger.Msg("Boat transform roll L: " + boat.transform.rotation + " Angle: " + Quaternion.Angle(boat.transform.rotation, originalBoatRotation));


                        if (Quaternion.Angle(boat.transform.rotation, target) <= 0.01f)
                        {
                            MelonLogger.Msg("Boat transform rolled to L: " + boat.transform.rotation + " Angle: " + Quaternion.Angle(boat.transform.rotation, originalBoatRotation));

                            boatHasRolledToLeft = false;
                            isBoatBouncedLeft = true;

                        }
                    }*/

                    /*
                    if (boatHasRotatedtoRight)
                    {
                        float rotationAmount = Mathf.Lerp(0, 4.0f, Time.deltaTime * 0.6f); // positive rotation
                        Quaternion targetRotation = Quaternion.AngleAxis(rotationAmount, Vector3.right);
                        boat.transform.rotation = Quaternion.Slerp(boat.transform.rotation, targetRotation, Time.deltaTime);

                        if (Quaternion.Angle(boat.transform.rotation, targetRotation) <= 5f)
                        {
                            Quaternion returnRotation = Quaternion.Slerp(boat.transform.rotation, originalBoatRotation, Time.deltaTime * 2);
                            boat.transform.rotation = returnRotation;
                            if (Quaternion.Angle(boat.transform.rotation, originalBoatRotation) <= 1f)
                            {
                                boatHasRotatedtoRight = false;
                                isBoatBouncedRight = true;
                            }
                        }
                    }
                    */


                    /*if (paddleController.name == "controller_paddle_L" && boatHasRotatedtoLeft && !IsPaddleInWater)
                    {
                        MelonLogger.Msg("Rotate back L");
                        float returnRotationAmount = Mathf.Lerp(0, (boatRotationtHistoryLeft * -1), Time.deltaTime * 0.6f);
                        boat.transform.Rotate(returnRotationAmount, 0, 0);

                        if (boat.transform.rotation.x <= 0.0f)
                        {
                            MelonLogger.Msg("Rotated fully back L");
                            boatHasRotatedtoLeft = false;
                        }
                    }*/

                    /*if (paddleController.name == "controller_paddle_R" && boatHasRotatedtoRight && !IsPaddleInWater)
                    {
                        MelonLogger.Msg("Rotate back R");
                        float returnRotationAmount = Mathf.Lerp(0, (boatRotationtHistoryLeft * -1), Time.deltaTime * 0.6f);
                        boat.transform.Rotate(returnRotationAmount, 0, 0);

                        if (boat.transform.rotation.x >= 0.0f)
                        {
                            MelonLogger.Msg("Rotated fully back R");
                            boatHasRotatedtoRight = false;
                        }
                    }*/

                    // If paddle not grabbed OR Paddle not in water no need calculations
                    if (!paddleController.IsGrabbed(null) || !IsPaddleInWater)
                    {
                        /*
                        if ((boat.transform.rotation.x >= 0.0f || boat.transform.rotation.x <= 0.0f) && (boatHasRotatedtoRight || boatHasRotatedtoLeft))
                        {
                            MelonLogger.Msg("Reset rotation");
                            boat.transform.rotation = Quaternion.Slerp(boat.transform.rotation, Quaternion.Euler(0.0f, 0, 0), Time.time * 0.6f);
                        }
                        */

                        return;
                    }

                    if (IsPaddleInWater)
                    {

                        //MelonLogger.Msg("Boat rotation X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);



                        /*if (paddleController.name == "controller_paddle_L" && isLeftPaddleInWater && !boatHasRolledToLeft)
                        {
                            float currentX = boat.transform.eulerAngles.x;
                            float targetX = currentX + maxRoll;
                            if (targetX > currentX + 5)
                            {
                                targetX = currentX + 5;
                            }

                            Quaternion target = Quaternion.Euler(targetX, boat.transform.eulerAngles.y, boat.transform.eulerAngles.z);
                            boat.transform.rotation = Quaternion.Slerp(boat.transform.rotation, target, Time.deltaTime);

                            MelonLogger.Msg("paddle roll L, target: " + target + " Angle: " + Quaternion.Angle(boat.transform.rotation, target) + " rotation: " + boat.transform.rotation);

                            if (Quaternion.Angle(boat.transform.rotation, target) <= 0.01f)
                            {
                                MelonLogger.Msg("paddle rolled fully L, target: " + target + " Angle: " + Quaternion.Angle(boat.transform.rotation, target) + " rotation: " + boat.transform.rotation);
                                boatHasRolledToLeft = true;
                            }
                        }*/


                        /*
                        if (paddleController.name == "controller_paddle_R" && isRightPaddleInWater)
                        {
                            float targetRotation = -5.0f;
                            float rotationSpeed = 0.6f;
                            float currentRotation = boat.transform.rotation.eulerAngles.x;

                            if (currentRotation > targetRotation)
                            {
                                float rotationAmount = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
                                boat.transform.rotation = Quaternion.AngleAxis(rotationAmount, Vector3.right);
                            }

                            if (currentRotation <= targetRotation)
                            {
                                MelonLogger.Msg("R rotated fully. X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);
                                boatHasRotatedtoRight = true;
                            }
                        }
                        */


                        /*
                        if (paddleController.name == "controller_paddle_R" && isRightPaddleInWater)
                        {
                            if (boat.transform.rotation.x >= -0.04f)
                            {
                                //MelonLogger.Msg("Rotating to L " + boat.transform.rotation.x);
                                float rotationAmount = Mathf.Lerp(0, -4.0f, Time.deltaTime * 0.6f);
                                //boat.transform.Rotate(rotationAmount, 0, 0);

                                Quaternion targetRotation = Quaternion.Euler(rotationAmount, 0, 0);
                                boat.transform.rotation = targetRotation;
                            }

                            if (boat.transform.rotation.x <= -0.04f)
                            {
                                MelonLogger.Msg("Rotated fully to R " + boat.transform.rotation + " w: " + boat.transform.rotation.w);
                                boatHasRotatedtoRight = true;
                            }
                        }
                        */


                        /*Vector3 velocity = boat.transform.InverseTransformDirection(__instance.GetComponent<Rigidbody>().velocity);
                        velocity.y = 0f;
                        float x = velocity.x;*/
                        //MelonLogger.Msg($"Boat TEST velocity X: " + velocity.x + " Y: " + velocity.y + " Z: " + velocity.z);
                        // Koks Z velocity?


                        //Vector3 forward = boat.transform.forward;
                        //Vector3 up = boat.transform.up;
                        //MelonLogger.Msg("Boat forward direction: " + forward);


                        /*if( (!isLoggedPaddleHitLc && !isLoggedPaddleHitRc) )
                        {
                            Quaternion currentRotation = boat.transform.rotation;
                            if (previousBoatRotation != Quaternion.identity)
                            {
                                boatRotationDelta = Quaternion.Angle(previousBoatRotation, currentRotation);
                                MelonLogger.Msg("Boat rotating: " + boatRotationDelta);
                            }
                            previousBoatRotation = currentRotation;
                        }*/


                        // paddleController.name == "controller_paddle_L" && paddleController.IsGrabbed()

                        if (isLeftPaddleInWater && isLastPaddleWasRight)
                        {
                            //MelonLogger.Msg("Stop hard rotation at L : " + boatRotationDelta);
                            boatRotationDelta = 0;

                            isLeftPaddleStop = true;
                            isLastPaddleWasRight = false;
                        }

                        if (isRightPaddleInWater && isLastPaddleWasLeft)
                        {
                            //MelonLogger.Msg("Stop hard rotation at R : " + boatRotationDelta);
                            boatRotationDelta = 0;

                            isRightPaddleStop = true;
                            isLastPaddleWasLeft = false;
                        }


                        // At Left side rotate boat
                        /*if (paddleController.name == "controller_paddle_L" && isLeftPaddleStop && isLeftPaddleInWater && !boatHasRotatedtoLeft)
                        {
                            MelonLogger.Msg("L rotating. X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);
                            float rotationAmount = Mathf.Lerp(0, 4.0f, Time.deltaTime * 0.6f);
                            boat.transform.Rotate(rotationAmount, 0, 0);

                            if (boat.transform.rotation.x >= 0.04f)
                            {
                                MelonLogger.Msg("L rotated fully. X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);
                                boatRotationtHistoryLeft = boat.transform.rotation.x;
                                boatHasRotatedtoLeft = true;
                            }
                        }*/

                        // At Right side rotate boat
                        /*if (paddleController.name == "controller_paddle_R" && isRightPaddleStop && isRightPaddleInWater && !boatHasRotatedtoRight)
                        {
                            MelonLogger.Msg("R rotating. X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);
                            float rotationAmount = Mathf.Lerp(0, -4.0f, Time.deltaTime * 0.6f);
                            boat.transform.Rotate(rotationAmount, 0, 0);

                            if (boat.transform.rotation.x <= -0.04f)
                            {
                                MelonLogger.Msg("R rotated fully. X: " + boat.transform.rotation.x + " Y: " + boat.transform.rotation.y + " Z: " + boat.transform.rotation.z + " W: " + boat.transform.rotation.w + " Eu: " + boat.transform.rotation.eulerAngles.x);
                                boatRotationtHistoryRight = boat.transform.rotation.x;
                                boatHasRotatedtoRight = true;
                            }
                        }*/


                        if ( (isLeftPaddleInWater || isRightPaddleInWater) && 
                             (boatRotationDelta > 6.00f) && (boatSpeed > 6.0f) &&
                             ((waterYPosition - changedYBoatPosition) > maxYBoatSinkPosition)
                           )
                        {
                            Vector3 newPos = new Vector3(boat.transform.position.x, maxYBoatSinkPosition, boat.transform.position.z);
                            boat.transform.position = Vector3.Lerp(boat.transform.position, newPos, Time.deltaTime * depthAdjustSpeed);

                            //MelonLogger.Msg($"Boat Drowning, to: " + newPos.y);
                            //MelonLogger.Msg($"Current drawned boat Y:: " + boat.transform.position.y);
                            //MelonLogger.Msg("Boat sinking at rotation: " + boatRotationDelta);

                            /////////// Tilt boat a little
                        }
                    }

                }
            }

            [HarmonyPatch(typeof(PaddleVelocity), "OnTriggerEnter")] //Exists
            internal static class PaddleVelocity_OnTriggerEnter
            {
                public static void Postfix(PaddleVelocity __instance, Collider other)
                {
                    if (other.gameObject.GetComponent<WaterVolume>() != null)
                    {
                        string paddleName = __instance.GetComponentInParent<VRTK_InteractableObject>().name;

                        // Store the current position of the boat
                        Vector3 currentBoatPosition = boat.transform.position;
                        Quaternion currentBoatRotation = boat.transform.rotation;

                        // Calculate the distance moved
                        //float distanceMoved = Vector3.Distance(currentBoatPosition, previousBoatPosition);
                        // Calculate the speed of the boat
                        //float speed = distanceMoved / Time.deltaTime;
                        //MelonLogger.Msg("Real boat speed: " + speed);

                        // (NOSE ANGLE) Determine the direction of the boat's movement 
                        //Vector3 movementDirection = Vector3.Normalize(currentBoatPosition - previousBoatPosition);
                        //float dotProduct = Vector3.Dot(boat.transform.forward, movementDirection);

                        // When paddle_L enters water..
                        if (paddleName == "controller_paddle_L")
                        {

                            // Boat speed based on Paddle velocity
                            Vector3 boatVelocity = boat.transform.InverseTransformDirection(__instance.GetComponent<Rigidbody>().velocity);
                            boatSpeed = boatVelocity.magnitude;
                            //MelonLogger.Msg("Boat speed Lc: " + boatSpeed);

                            //MelonLogger.Msg("Boat nose Lc " + dotProduct);

                            MelonLogger.Msg($"Water enter Lc");
                            isLeftPaddleInWater = true;
                        }

                        // When paddle_R enters water..
                        if (paddleName == "controller_paddle_R")
                        {

                            // Boat speed based on Paddle velocity
                            Vector3 boatVelocity = boat.transform.InverseTransformDirection(__instance.GetComponent<Rigidbody>().velocity);
                            boatSpeed = boatVelocity.magnitude;
                            //MelonLogger.Msg("Boat speed Rc: " + boatSpeed);

                            //MelonLogger.Msg("Boat nose Rc " + dotProduct);

                            MelonLogger.Msg($"Water enter Rc");
                            isRightPaddleInWater = true;
                        }


                        if (previousBoatRotation != Quaternion.identity)
                        {
                            float tempRotationData = Quaternion.Angle(previousBoatRotation, currentBoatRotation);

                            if (tempRotationData != 0)
                            {
                                boatRotationDelta = tempRotationData;

                                if (paddleName == "controller_paddle_L" && !isLoggedPaddleHitLc) 
                                { 
                                    //MelonLogger.Msg("Boat rotating Lc: " + boatRotationDelta);
                                    isLoggedPaddleHitLc = true;
                                    isLeftPaddleStop = false;
                                }

                                if (paddleName == "controller_paddle_R" && !isLoggedPaddleHitRc)
                                {
                                    //MelonLogger.Msg("Boat rotating Rc: " + boatRotationDelta);
                                    isLoggedPaddleHitRc = true;
                                    isRightPaddleStop = false;
                                }
                            }
                        }

                        // Store current position as previous position for next FixedUpdate call
                        previousBoatPosition = currentBoatPosition;
                        previousBoatRotation = currentBoatRotation;

                        IsPaddleInWater = true;
                    }
                }

            }


            [HarmonyPatch(typeof(PaddleVelocity), "OnTriggerExit")] //Exists
            internal static class PaddleVelocity_OnTriggerExit
            {
                public static void Postfix(PaddleVelocity __instance, Collider other)
                {
                    if (other.gameObject.GetComponent<WaterVolume>() != null)
                    {
                        string paddleName = __instance.GetComponentInParent<VRTK_InteractableObject>().name;

                        // When paddle_L exit water..
                        if (paddleName == "controller_paddle_L")
                        {
                            MelonLogger.Msg($"Exit by Lc");
                            isLastPaddleWasLeft = true;
                            isLeftPaddleInWater = false;
                            isLoggedPaddleHitLc = false;
                            isBoatBouncedLeft = false;
                        }

                        // When paddle_R exit water..
                        if (paddleName == "controller_paddle_R")
                        {
                            MelonLogger.Msg($"Exit by Rc");
                            isLastPaddleWasRight = true;
                            isRightPaddleInWater = false;
                            isLoggedPaddleHitRc = false;
                            isBoatBouncedRight = false;
                        }

                        IsPaddleInWater = false;
                    }
                }
            }

        }
    }
}
