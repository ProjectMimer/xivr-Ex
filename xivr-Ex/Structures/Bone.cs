using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Logging;
using FFXIVClientStructs.Havok;
using System.Runtime.InteropServices;

namespace xivr.Structures
{
    public unsafe class Bone
    {
        private float Deg2Rad = MathF.PI / 180.0f;
        private float Rad2Deg = 180.0f / MathF.PI;
        public BoneList boneKey = BoneList._root_;
        public short id = -1;
        public short parentId = -1;
        public hkQsTransformf transform = new hkQsTransformf();
        public hkQsTransformf reference = new hkQsTransformf();
        public hkQsTransformf localBase = new hkQsTransformf();
        public Bone? parent = null;
        public Dictionary<int, Bone> children = new Dictionary<int, Bone>();
        public Matrix4x4 boneMatrix = Matrix4x4.Identity;
        public Matrix4x4 boneMatrixI = Matrix4x4.Identity;
        public Matrix4x4 localMatrix = Matrix4x4.Identity;
        public Matrix4x4 localMatrixI = Matrix4x4.Identity;
        public Vector3 boneStart = new Vector3();
        public Vector3 boneFinish = new Vector3();
        public bool useReference = false;
        public bool updatePosition = false;
        public bool updateRotation = false;
        public bool updateScale = false;
        public bool disableParent = false;
        public bool isSet = false;

        public Bone() { }

        public Bone(BoneList bKey, short kId, short pId, Bone? pBone, hkQsTransformf hkqTransform, hkQsTransformf hkqReference)
        {
            boneKey = bKey;
            id = kId;
            parentId = pId;
            transform = hkqTransform;
            reference = hkqReference;
            isSet = true;
            parent = pBone;
            if (parent != null)
                parent.children.Add(kId, this);
            CalculateMatrix();
        }

        public void CalculateMatrix(bool runChild = false)
        {
            localMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation.Convert());
            localMatrix.Translation = transform.Translation.Convert();
            localMatrix.SetScale(transform.Scale);
            Matrix4x4.Invert(localMatrix, out localMatrixI);

            boneMatrix = localMatrix * ((parent != null) ? parent.boneMatrix : Matrix4x4.Identity);
            Matrix4x4.Invert(boneMatrix, out boneMatrixI);
            
            boneStart = (parent != null) ? parent.boneFinish : Vector3.Zero;
            boneFinish = boneMatrix.Translation;

            localBase.Translation = boneMatrix.Translation.Convert();
            localBase.Rotation = Quaternion.CreateFromRotationMatrix(boneMatrix).Convert();
            localBase.Scale = transform.Scale;
            
            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.CalculateMatrix(runChild);
        }

        public void SetTransformFromLocalBase(bool runChild = false) 
        {
            Matrix4x4 local = boneMatrix;
            if (parent != null)
                local *= parent.boneMatrixI;
            transform.Translation = local.Translation.Convert();
            transform.Rotation = Quaternion.CreateFromRotationMatrix(local).Convert();
            CalculateMatrix();

            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.SetTransformFromLocalBase(runChild);
        }

        public void Inverse()
        {
            updatePosition = true;
            transform.Translation.X = transform.Translation.X * -1;
            transform.Translation.Y = transform.Translation.Y * -1;
            transform.Translation.Z = transform.Translation.Z * -1;
            transform.Translation.W = 0;

            Quaternion q = new Quaternion(transform.Rotation.X, transform.Rotation.W, transform.Rotation.Z, transform.Rotation.W);
            q = Quaternion.Inverse(q);
            updateRotation = true;
            //transform.Rotation.X = q.X;
            //transform.Rotation.Y = q.Y;
            //transform.Rotation.Z = q.Z;
            //transform.Rotation.W = q.W;
        }

        public void InverseChildren()
        {
            Inverse();
            foreach (KeyValuePair<int, Bone> child in children)
                child.Value.InverseChildren();
        }

        public void SetReference(bool calculateMatrix = true, bool runChild = false)
        {
            updatePosition = true;
            updateRotation = true;
            updateScale = true;

            transform.Translation = reference.Translation;
            transform.Rotation = reference.Rotation;
            transform.Scale = reference.Scale;

            if(calculateMatrix)
                CalculateMatrix();

            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.SetReference(calculateMatrix, runChild);
        }

        public void SetTransform(hkQsTransformf location, bool calculateMatrix = true, bool runChild = false)
        {
            updatePosition = true;
            updateRotation = true;
            updateScale = true;

            transform = location;

            if (calculateMatrix)
                CalculateMatrix();

            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.SetTransform(location, runChild);
        }

        public void SetTransform(Matrix4x4 location, bool calculateMatrix = true, bool runChild = false)
        {
            updatePosition = true;
            updateRotation = true;
            updateScale = true;

            transform.Translation = location.Translation.Convert();
            transform.Rotation = Quaternion.CreateFromRotationMatrix(location).Convert();
            transform.Scale = location.GetScale().Convert();

            if (calculateMatrix)
                CalculateMatrix();

            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.SetTransform(location, runChild);
        }

        public void SetScale(Vector3 scale, bool runChild = false)
        {
            updateScale = true;
            transform.Scale = scale.Convert();

            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.SetScale(scale, runChild);
        }

        public Matrix4x4 ToLocal(Matrix4x4 matrix)
        {
            if (parent != null)
                return parent.ToLocal(matrix * boneMatrixI);
            else
                return matrix * boneMatrixI;
        }

        public void Output(int indent = 0, bool runChild = false)
        {
            Matrix4x4 MatrixA = Matrix4x4.CreateFromQuaternion(transform.Rotation.Convert());
            Vector3 anglesR = xivr_hooks.GetAngles(MatrixA);
            ToEulerAngles(transform.Rotation.Convert(), out float pitch, out float yaw, out float roll);

            Quaternion q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);


            string spacer = new String(' ', indent * 2);
            
            PluginLog.Log($"{spacer} {anglesR.X * Rad2Deg} {anglesR.Y * Rad2Deg} {anglesR.Z * Rad2Deg} | {pitch * Rad2Deg} {yaw * Rad2Deg} {roll * Rad2Deg} | {q.X} {q.Y} {q.Z} {q.W}");
            PluginLog.Log($"{spacer} {parentId} | {(BoneListEn)boneKey} | {transform.Translation.X} {transform.Translation.Y} {transform.Translation.Z} | {transform.Rotation.X} {transform.Rotation.Y} {transform.Rotation.Z} {transform.Rotation.W}");
            if (runChild == true)
                foreach (KeyValuePair<int, Bone> child in children)
                    child.Value.Output(indent + 1, runChild);
        }

        public void OutputToParent(bool runParent = false)
        {
            Vector3 anglesR = xivr_hooks.GetAngles(boneMatrix);
            Vector3 anglesI = xivr_hooks.GetAngles(boneMatrixI);

            ToEulerAngles(transform.Rotation.Convert(), out float pitch, out float yaw, out float roll);

            Quaternion qV = FromToRotation(boneStart, boneFinish);
            ToEulerAngles(qV, out float pitchV, out float yawV, out float rollV);

            PluginLog.Log($"-{MathF.Round(pitchV * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(yawV * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(rollV * Rad2Deg, 5).ToString("0.00000")}");
            PluginLog.Log($"{parentId} | {(BoneListEn)boneKey} | localBase: {MathF.Round(anglesR.X * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(anglesR.Y * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(anglesR.Z * Rad2Deg, 5).ToString("0.00000")} | {MathF.Round(anglesI.X * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(anglesI.Y * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(anglesI.Z * Rad2Deg, 5).ToString("0.00000")}");
            PluginLog.Log($"Start: {MathF.Round(boneStart.X, 5).ToString("0.00000")} {MathF.Round(boneStart.Y, 5).ToString("0.00000")} {MathF.Round(boneStart.Z, 5).ToString("0.00000")} | Finish: {MathF.Round(boneFinish.X, 5).ToString("0.00000")} {MathF.Round(boneFinish.Y, 5).ToString("0.00000")} {MathF.Round(boneFinish.Z, 5).ToString("0.00000")}");
            PluginLog.Log($"{MathF.Round(localBase.Translation.X, 5).ToString("0.00000")} {MathF.Round(localBase.Translation.Y, 5).ToString("0.00000")} {MathF.Round(localBase.Translation.Z, 5).ToString("0.00000")} | {MathF.Round(localBase.Rotation.X, 5).ToString("0.00000")} {MathF.Round(localBase.Rotation.Y, 5).ToString("0.00000")} {MathF.Round(localBase.Rotation.Z, 5).ToString("0.00000")} {MathF.Round(localBase.Rotation.W, 5).ToString("0.00000")}");
            PluginLog.Log($"{MathF.Round(transform.Translation.X, 5).ToString("0.00000")} {MathF.Round(transform.Translation.Y, 5).ToString("0.00000")} {MathF.Round(transform.Translation.Z, 5).ToString("0.00000")} | {MathF.Round(transform.Rotation.X, 5).ToString("0.00000")} {MathF.Round(transform.Rotation.Y, 5).ToString("0.00000")} {MathF.Round(transform.Rotation.Z, 5).ToString("0.00000")} {MathF.Round(transform.Rotation.W, 5).ToString("0.00000")} | {MathF.Round(pitch * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(yaw * Rad2Deg, 5).ToString("0.00000")}, {MathF.Round(roll * Rad2Deg, 5).ToString("0.00000")}");
            PluginLog.Log($"-");
            if (parent != null && runParent)
                parent.OutputToParent(runParent);
        }



        public void ToEulerAngles(Quaternion q, out float pitch, out float yaw, out float roll)
        {
            float sqw = q.W * q.W;
            float sqx = q.X * q.X;
            float sqy = q.Y * q.Y;
            float sqz = q.Z * q.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = q.X * q.W - q.Y * q.Z;

            if (test > 0.49975f * unit)
            {   // singularity at north pole
                yaw = -2f * MathF.Atan2(q.Y, q.X);
                pitch = -MathF.PI / 2f;
                roll = 0;
                return;
            }
            if (test < -0.49975f * unit)
            {   // singularity at south pole
                yaw = 2f * MathF.Atan2(q.Y, q.X);
                pitch = MathF.PI / 2f;
                roll = 0;
                return;
            }

            Quaternion q1 = new Quaternion(q.W, q.Z, q.X, q.Y);
            yaw = -1 * MathF.Atan2(2f * (q1.X * q1.W + q1.Y * q1.Z), 1f - 2f * (q1.Z * q1.Z + q1.W * q1.W));   // Yaw
            pitch = -1 * MathF.Asin(2f * (q1.X * q1.Z - q1.W * q1.Y));                                         // Pitch
            roll = -1 * MathF.Atan2(2f * (q1.X * q1.Y + q1.Z * q1.W), 1f - 2f * (q1.Y * q1.Y + q1.Z * q1.Z));  // Roll
        }

        Quaternion FromToRotation(Vector3 aFrom, Vector3 aTo)
        {
            Vector3 axis = Vector3.Cross(aFrom, aTo);
            float angle = Angle(aFrom, aTo);
            return AngleAxis(Vector3.Normalize(axis), angle);
        }

        float Angle(Vector3 from, Vector3 to)
        {
            float kEpsilonNormalSqrt = 1e-15F;
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = (float)Math.Sqrt(from.LengthSquared() * to.LengthSquared());
            if (denominator < kEpsilonNormalSqrt)
                return 0F;

            float dot = Math.Clamp(Vector3.Dot(from, to) / denominator, -1f, 1f);
            return ((float)Math.Acos(dot)) * Rad2Deg;
        }

        Quaternion AngleAxis(Vector3 aAxis, float aAngle)
        {
            aAxis = Vector3.Normalize(aAxis);
            float rad = aAngle * Deg2Rad * 0.5f;
            aAxis *= MathF.Sin(rad);
            return new Quaternion(aAxis.X, aAxis.Y, aAxis.Z, MathF.Cos(rad));
        }
    }
}
