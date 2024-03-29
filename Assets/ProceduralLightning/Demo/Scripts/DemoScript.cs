//
// Procedural Lightning for Unity
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using UnityEngine;
using System.Collections;

namespace DigitalRuby.ThunderAndLightning
{
    /// <summary>
    /// Main demo script to configure lightning
    /// </summary>
    public class DemoScript : MonoBehaviour
    {
        /// <summary>
        /// Thunder and lightning script
        /// </summary>
        public ThunderAndLightningScript ThunderAndLightningScript;

        /// <summary>
        /// Lightning bolt script
        /// </summary>
        public LightningBoltScript LightningBoltScript;

        /// <summary>
        /// Cloud paticle system
        /// </summary>
        public ParticleSystem CloudParticleSystem;

        /// <summary>
        /// Move speed
        /// </summary>
        public float MoveSpeed = 250.0f;

        /// <summary>
        /// Whether to enable mouse look
        /// </summary>
        public bool EnableMouseLook = true;

        private const float fastCloudSpeed = 50.0f;

        private float deltaTime;
        private float fpsIncrement;
        private string fpsText;

        private enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
        private RotationAxes axes = RotationAxes.MouseXAndY;
        private float sensitivityX = 15F;
        private float sensitivityY = 15F;
        private float minimumX = -360F;
        private float maximumX = 360F;
        private float minimumY = -60F;
        private float maximumY = 60F;
        private float rotationX = 0F;
        private float rotationY = 0F;
        private Quaternion originalRotation;

        private static readonly GUIStyle style = new GUIStyle();

        private void UpdateThunder()
        {
            if (ThunderAndLightningScript != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    ThunderAndLightningScript.CallNormalLightning();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    ThunderAndLightningScript.CallIntenseLightning();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    if (CloudParticleSystem != null)
                    {
                        var m = CloudParticleSystem.main;
                        m.simulationSpeed = (m.simulationSpeed == 1.0f ? fastCloudSpeed : 1.0f);
                    }
                }
            }
        }

        private void UpdateMovement()
        {
            float speed = MoveSpeed * LightningBoltScript.DeltaTime;

            if (Input.GetKey(KeyCode.W))
            {
                Camera.main.transform.Translate(0.0f, 0.0f, speed);
            }
            if (Input.GetKey(KeyCode.S))
            {
                Camera.main.transform.Translate(0.0f, 0.0f, -speed);
            }
            if (Input.GetKey(KeyCode.A))
            {
                Camera.main.transform.Translate(-speed, 0.0f, 0.0f);
            }
            if (Input.GetKey(KeyCode.D))
            {
                Camera.main.transform.Translate(speed, 0.0f, 0.0f);
            }
        }

        private void UpdateMouseLook()
        {
            if (!EnableMouseLook)
            {
                return;
            }
            else if (axes == RotationAxes.MouseXAndY)
            {
                // Read the mouse input axis
                rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

                rotationX = ClampAngle(rotationX, minimumX, maximumX);
                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

                transform.localRotation = originalRotation * xQuaternion * yQuaternion;
            }
            else if (axes == RotationAxes.MouseX)
            {
                rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                rotationX = ClampAngle(rotationX, minimumX, maximumX);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                transform.localRotation = originalRotation * xQuaternion;
            }
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion yQuaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
                transform.localRotation = originalRotation * yQuaternion;
            }
        }

        private void UpdateQuality()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                QualitySettings.SetQualityLevel(0);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                QualitySettings.SetQualityLevel(1);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                QualitySettings.SetQualityLevel(2);
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                QualitySettings.SetQualityLevel(3);
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                QualitySettings.SetQualityLevel(4);
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                QualitySettings.SetQualityLevel(5);
            }
        }

        private void UpdateOther()
        {
            deltaTime += (LightningBoltScript.DeltaTime - deltaTime) * 0.1f;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReloadCurrentScene();
            }
        }

        private void OnGUI()
        {
            int w = Screen.width;
            int h = Screen.height;
            int p = (int)(Screen.height * 0.08f);
            Rect rect = new Rect((int)(w * 0.01), h - p, w, (int)(p * 0.9));
            style.alignment = TextAnchor.LowerLeft;
            style.fontSize = p / 2;
            style.normal.textColor = Color.white;

            if ((fpsIncrement += LightningBoltScript.DeltaTime) > 1.0f)
            {
                fpsIncrement -= 1.0f;
                float msec = deltaTime * 1000.0f;
                float fps = 1.0f / deltaTime;
                fpsText = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            }

            GUI.Label(rect, fpsText, style);
        }

        private void Update()
        {
            UpdateThunder();
            UpdateMovement();
            UpdateMouseLook();
            UpdateQuality();
            UpdateOther();
        }

        private void Start()
        {
            originalRotation = transform.localRotation;

            if (CloudParticleSystem != null)
            {
                var m = CloudParticleSystem.main;
                m.simulationSpeed = fastCloudSpeed;
            }
        }

        /// <summary>
        /// Clamp an angle
        /// </summary>
        /// <param name="angle">Angle</param>
        /// <param name="min">Min angle</param>
        /// <param name="max">Max angle</param>
        /// <returns>Clamped angle</returns>
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
            {
                angle += 360F;
            }
            if (angle > 360F)
            {
                angle -= 360F;
            }

            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public static void ReloadCurrentScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}