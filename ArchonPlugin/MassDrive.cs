using AVS.Engines;
using Subnautica_Archon.Modules;
using UnityEngine;

namespace Subnautica_Archon
{
    public class MassDrive : AbstractEngine
    {
        public MassDrive()
        {
            AngularDrag = 1;
        }

        protected override void ApplyDrag(Vector3 move)
        {
            //nope
        }

        public bool doNotAccelerate;
        public float overdriveActive;
        public Vector3 currentInput;
        public bool freeCamera;
        public float lastDrainPerSecond;
        public ArchonModule driveUpgrade;

        public override void Awake()
        {
            //WhistleFactor = 1.5f;
            base.Awake();
        }
        public override void Start()
        {
            base.Start();
            RB.angularDrag = 1;
            //RB.drag = 0;
        }
        public override void ControlRotation()
        {
        }

        public override void KillMomentum()
        {
            base.KillMomentum();
            RB.velocity = Vector3.zero;
        }

        protected override void MoveWithInput(Vector3 moveInput)
        {
            //default = 3/4 * 5 / 2.25 = 166%
            //lateral = 3/4 * 2 / 1.5 = 100%
            var baseSpeed = 0.5f;
            var baseLateral = 0.5f;
            //var speedBoost = DriveModule.GetSpeedBoost(driveUpgrade);

            //var boost = baseSpeed * speedBoost;
            //var lateralBoost = baseLateral * speedBoost;
            //Log.WriteLowFrequency(MyLogger.Channel.Two, $"MoveWithInput({moveInput})");
            currentInput = moveInput;
            moveInput = new Vector3(
                moveInput.x * (baseLateral /*+ lateralBoost * overdriveActive*/),
                moveInput.y * (baseLateral /*+ lateralBoost * overdriveActive*/),
                moveInput.z * (baseSpeed /*+ boost * overdriveActive*/));
            moveInput = GetEffectiveMoveInput(moveInput);
            RB.AddRelativeForce(moveInput, ForceMode.VelocityChange);
        }

        private Vector3 GetEffectiveMoveInput(Vector3 moveInput)
        {
            if (doNotAccelerate)
                return Vector3.zero;
            if (freeCamera)
                return new Vector3(0, 0, moveInput.z);
            return moveInput;
        }



        public override void DrainPower(Vector3 moveDirection)
        {
            if (!MV || !MV.PowerManager)
                return;
            moveDirection = GetEffectiveMoveInput(moveDirection);
            float energyNeeded = lastDrainPerSecond = M.Sqr(moveDirection) * (
                0.77f
                //+
                //1f * overdriveActive /** M.Sqr(BoostRelative)*/
                );

            var neededNow = energyNeeded * Time.fixedDeltaTime;
            var drained = Mathf.Abs(MV.PowerManager.TrySpendEnergy(neededNow));
            //insufficientPower = drained < neededNow * 0.8f; 
        }

    }

}