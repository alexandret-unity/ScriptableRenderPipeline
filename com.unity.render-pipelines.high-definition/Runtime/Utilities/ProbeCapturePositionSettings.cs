using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Settings to use when capturing a probe.</summary>
    [Serializable]
    public struct ProbeCapturePositionSettings
    {
        /// <summary>Default value.</summary>
        public static readonly ProbeCapturePositionSettings @default = new ProbeCapturePositionSettings(
            Vector3.zero, Quaternion.identity,
            Vector3.zero, Quaternion.identity
        );

        /// <summary>The proxy position.</summary>
        public Vector3 proxyPosition;
        /// <summary>The proxy rotation.</summary>
        public Quaternion proxyRotation;
        /// <summary>
        /// The reference position.
        ///
        /// This additional information is used to compute the actual capture position. (<see cref="ProbeSettings.ProbeType"/>)
        /// </summary>
        public Vector3 referencePosition;
        /// <summary>
        /// The reference rotation.
        ///
        /// This additional information is used to compute the actual capture position. (<see cref="ProbeSettings.ProbeType"/>)
        /// </summary>
        public Quaternion referenceRotation;

        /// <summary>Create a new settings with only the probe transform.</summary>
        /// <param name="proxyPosition">The proxy position.</param>
        /// <param name="proxyRotation">The proxy rotation.</param>
        public ProbeCapturePositionSettings(
            Vector3 proxyPosition,
            Quaternion proxyRotation
        )
        {
            this.proxyPosition = proxyPosition;
            this.proxyRotation = proxyRotation;
            referencePosition = Vector3.zero;
            referenceRotation = Quaternion.identity;
        }

        /// <summary>Create new settings.</summary>
        /// <param name="proxyPosition">The proxy position.</param>
        /// <param name="proxyRotation">The proxy rotation.</param>
        /// <param name="referencePosition">The reference position.</param>
        /// <param name="referenceRotation">The reference rotation.</param>
        public ProbeCapturePositionSettings(
            Vector3 proxyPosition,
            Quaternion proxyRotation,
            Vector3 referencePosition,
            Quaternion referenceRotation
        )
        {
            this.proxyPosition = proxyPosition;
            this.proxyRotation = proxyRotation;
            this.referencePosition = referencePosition;
            this.referenceRotation = referenceRotation;
        }

        public static ProbeCapturePositionSettings ComputeFrom(HDProbe probe, Transform reference)
        {
            var referencePosition = Vector3.zero;
            var referenceRotation = Quaternion.identity;
            if (reference != null)
            {
                referencePosition = reference.position;
                referenceRotation = reference.rotation;
            }
            else
            {
                if (probe.probeType == ProbeSettings.ProbeType.PlanarProbe)
                {
                    var planar = (PlanarReflectionProbe)probe;
                    return ComputeFromMirroredReference(planar, planar.referencePosition);
                }
            }

            return ComputeFrom(probe, referencePosition, referenceRotation);
        }

        public static ProbeCapturePositionSettings ComputeFrom(
            HDProbe probe,
            Vector3 referencePosition, Quaternion referenceRotation
        )
        {
            var result = new ProbeCapturePositionSettings();
            var proxyTransform = probe.proxyVolume != null
                ? probe.proxyVolume.transform
                : probe.transform;
            result.proxyPosition = proxyTransform.position;
            result.proxyRotation = proxyTransform.rotation;
            result.referencePosition = referencePosition;
            result.referenceRotation = referenceRotation;
            return result;
        }

        public static ProbeCapturePositionSettings ComputeFromMirroredReference(
            HDProbe probe, Vector3 referencePosition
        )
        {
            var positionSettings = ComputeFrom(
                probe,
                referencePosition, Quaternion.identity
            );
            // Set proper orientation for the reference rotation
            var proxyMatrix = Matrix4x4.TRS(
                positionSettings.proxyPosition,
                positionSettings.proxyRotation,
                Vector3.one
            );
            var mirrorPosition = proxyMatrix.MultiplyPoint(probe.settings.proxySettings.mirrorPositionProxySpace);
            positionSettings.referenceRotation = Quaternion.LookRotation(mirrorPosition - positionSettings.referencePosition);
            return positionSettings;
        }

        public Hash128 ComputeHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();
            HashUtilities.QuantisedVectorHash(ref proxyPosition, ref h);
            HashUtilities.QuantisedVectorHash(ref referencePosition, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            var euler = proxyRotation.eulerAngles;
            HashUtilities.QuantisedVectorHash(ref euler, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            euler = referenceRotation.eulerAngles;
            HashUtilities.QuantisedVectorHash(ref euler, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            return h;
        }
    }
}
