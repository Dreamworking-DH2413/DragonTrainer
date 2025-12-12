using UnityEngine;

namespace AztechGames
{
    public class PhysicOnGlider : MonoBehaviour
    {
        [Header("Main Physics Variables")] 
        [Tooltip("Air density in kg/m³")]
        public float airDensity = 1.225f;

        [Tooltip("Wing area in square meters")]
        public float wingArea = 27.87f;

        [Tooltip("Coefficient of lift")]
        public float liftCoefficient = 1.8f;

        [Tooltip("Coefficient of drag")]
        public float dragCoefficient = 0.5f;

        [Tooltip("Temperature in degrees Celsius")]
        public float temperature = 59f;

        private float IAS = 0f;
        private float altitude = 0f;
        private Vector3 windVelocity;
     
        [HideInInspector]
        public Rigidbody _rb;

        private GliderEngine_Controller _gliderEngineController;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _gliderEngineController = GetComponent<GliderEngine_Controller>();
        }

        /// <summary>
        /// Calculates the true airspeed of the glider.
        /// </summary>
        /// <returns>True airspeed in meters per second.</returns>
        public float TrueAirSpeed()
        {
            IAS = _rb.linearVelocity.magnitude;
            altitude = transform.position.y;
            float tas = IAS / (Mathf.Pow(1f + altitude / 44330f, 5.255f) * Mathf.Sqrt((temperature + 273.15f) / 288.15f));
            return tas;
        }
        
        /// <summary>
        /// Calculates the airspeed of the glider, accounting for wind direction.
        /// </summary>
        /// <returns>Airspeed in meters per second.</returns>
        public float AirSpeed()
        {
            Vector3 relativeWind  = windVelocity - _rb.linearVelocity;
            Quaternion rotation = Quaternion.Inverse(transform.rotation);
            Vector3 relativeWindLocal = rotation * relativeWind;
            float airSpeed = Mathf.Sqrt(Mathf.Pow(TrueAirSpeed(), 2) + Mathf.Pow(relativeWindLocal.magnitude, 2));
            return airSpeed;
        }
        
        /// <summary>
        /// Calculates the lift force on the glider.
        /// </summary>
        /// <returns>Lift force in Newtons.</returns>
        public float CalculateLift()
        {
            // Calculate angle of attack relative to velocity direction, not world forward!
            // This ensures lift works correctly regardless of which compass direction we're flying
            Vector3 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;
            
            if (speed < 0.1f)
                return 0f;
            
            // Project velocity onto the plane perpendicular to the right wing to get pitch-relative AOA
            Vector3 velocityDir = velocity.normalized;
            float angleOfAttack = Vector3.Angle(velocityDir, transform.forward);
            
            // Clamp AOA to reasonable range (beyond 90° means we're going backwards)
            angleOfAttack = Mathf.Min(angleOfAttack, 90f);
            
            float radianOfAngleDegree = Mathf.Deg2Rad * angleOfAttack;
            var lift = 0.5f * airDensity * TrueAirSpeed() * wingArea * liftCoefficient * Mathf.Cos(radianOfAngleDegree);
            return lift;
        }
        
        /// <summary>
        /// Calculates the drag force on the glider.
        /// </summary>
        /// <returns>Drag force in Newtons.</returns>
        public float CalculateDrag()
        {
            // Calculate angle of attack relative to velocity direction, not world forward!
            Vector3 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;
            
            if (speed < 0.1f)
                return 0f;
                
            Vector3 velocityDir = velocity.normalized;
            float angleOfAttack = Vector3.Angle(velocityDir, transform.forward);
            
            // Clamp AOA to reasonable range
            angleOfAttack = Mathf.Min(angleOfAttack, 90f);
            
            float radianOfAngleDegree = Mathf.Deg2Rad * angleOfAttack;
            var drag = 0.5f * airDensity * AirSpeed() * wingArea * dragCoefficient * Mathf.Pow(Mathf.Cos(radianOfAngleDegree), 2);
            return drag;
        }
        
        /// <summary>
        /// Applies lift and drag forces to simulate glider physics.
        /// </summary>
        void CalculateDradAndLift()
        {
            float liftForce = CalculateLift();
            float dragForce = CalculateDrag();
            
            Vector3 dragDirection = -_rb.linearVelocity.normalized;
            Vector3 drag = dragDirection * dragForce;
            _rb.AddForce(drag);
            
            Vector3 liftVector = transform.up * liftForce;
            _rb.AddForce(liftVector);
        }
        
        private void Update()
        {
            _rb.AddForce(transform.forward * _gliderEngineController.Thrust, ForceMode.Acceleration);
            CalculateDradAndLift();
        }
    }
}
