using CodeX.Core.Engine;
using CodeX.Games.MCLA.RSC5;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CodeX.Games.MCLA.Files
{
    //Plays one animation out of a .xapk onto whatever skeleton the target entity is carrying.
    //
    //An animation track names the bone it drives by id, not by index - the same id the skeleton
    //stores in its bone tag table (Rsc5SkeletonData.BonesMap), which is why a character animation
    //lines up with a character drawable it was never explicitly paired with. Measured on
    //drv_fc_003_set: 96 of the animation's 103 bone ids exist in the skeleton, the rest being face
    //DOFs the drawable has no bone for.
    //
    //Track ids follow the RAGE numbering the rest of the engine uses: 0 translation, 1 rotation,
    //2 scale, 5/6 the mover (root motion), 10 the cutscene camera's field of view.
    public class MclaAnimator : Animator
    {
        public XapkAnimation Animation;
        public bool Loop = true;
        public bool EnableRootMotion;
        public float Speed = 1.0f;

        //bone id -> bone, per skeleton. A character "_set" can hold more than one skeleton
        //(the _h head variants do), and all of them get posed by the same animation.
        private readonly Dictionary<Skeleton, Dictionary<int, Bone>> _binds = [];
        private XapkAnimation _boundAnim;

        public float CurrentFrame { get; private set; }
        public int TrackCount { get; private set; }        //bone tracks in the animation
        public int MatchedTrackCount { get; private set; } //...that the target skeleton has a bone for
        public Vector3 MoverPosition { get; private set; }
        public Quaternion MoverRotation { get; private set; } = Quaternion.Identity;
        public float CameraFov { get; private set; }
        public bool HasCamera { get; private set; }

        public MclaAnimator()
        {
        }

        public MclaAnimator(XapkAnimation animation)
        {
            Animation = animation;
        }

        public void Rewind()
        {
            CurrentTime = 0;
        }

        public override void Update(float elapsed)
        {
            if (Playing) CurrentTime += elapsed * Speed;
            ElapsedTime = elapsed;

            var anim = Animation;
            if (anim == null) return;

            var duration = anim.Duration > 0 ? anim.Duration : 1.0f;
            var t = (float)CurrentTime;
            if (Loop)
            {
                t %= duration;
                if (t < 0) t += duration;
            }
            else if (t > duration)
            {
                t = duration;
                Playing = false;
            }

            ApplyAll(anim, t);
        }

        //Pose the skeleton at a time in seconds, without touching playback state - the scrubber
        //and the single-frame case both come through here.
        public void ApplyTime(float seconds)
        {
            if (Animation == null) return;
            CurrentTime = seconds;
            ApplyAll(Animation, seconds);
        }

        private void ApplyAll(XapkAnimation anim, float t)
        {
            if (!ReferenceEquals(anim, _boundAnim))
            {
                _binds.Clear();
                _boundAnim = anim;
            }
            var single = Target?.Skeleton ?? Target?.Piece?.Skeleton;
            if (Targets == null || Targets.Length == 0)
            {
                if (single?.Bones != null) Apply(anim, single, t);
                return;
            }
            foreach (var target in Targets)
            {
                var skel = target?.Skeleton ?? target?.Piece?.Skeleton;
                if (skel?.Bones != null) Apply(anim, skel, t);
            }
        }

        //The mesh's bind pose is NOT the pose the bone hierarchy produces. crBoneData carries
        //m_GlobalOffset, the bone's absolute position in the pose the model was authored in - arms
        //down at the sides - while composing the hierarchy with each bone's default rotation gives
        //a T-pose. Measured on drv_mb_04_set: wrist_l sits at (0.05, -0.18, 0.92) per m_GlobalOffset
        //and the hand mesh occupies y -0.22..-0.14, z 0.74..1.04 - the same place; the hierarchy
        //puts that bone at (0.05, -0.56, 1.07) instead. Skinning against the hierarchy therefore
        //flings the arms and hands away from the body, which is exactly what it looked like.
        //
        //So while an animation is playing the bind matrices are swapped for ones built from
        //m_GlobalOffset, and put back when it stops - a model sitting still still renders through
        //CodeX's own bind pose.
        private readonly Dictionary<Skeleton, Matrix4x4[]> _originalBinds = [];

        private Dictionary<int, Bone> Bind(Skeleton skel)
        {
            if (_binds.TryGetValue(skel, out var map)) return map;
            map = [];
            var originals = new Matrix4x4[skel.Bones.Length];
            for (int i = 0; i < skel.Bones.Length; i++)
            {
                var bone = skel.Bones[i];
                if (bone == null) continue;
                var id = bone is Rsc5BoneData rb ? (int)rb.ID : bone.Index;
                map[id] = bone;
                originals[i] = bone.BindTransformInv;
                if (bone is Rsc5BoneData rbd)
                {
                    var g = new Vector3(rbd.AbsolutePosition.X, rbd.AbsolutePosition.Y, rbd.AbsolutePosition.Z);
                    bone.BindTransformInv = Matrix4x4.CreateTranslation(-g);
                }
            }
            _originalBinds[skel] = originals;
            _binds[skel] = map;
            skel.ResetBoneTransforms();
            return map;
        }

        //Put the skeleton back the way CodeX had it.
        public void Release()
        {
            foreach (var kvp in _originalBinds)
            {
                var skel = kvp.Key;
                var originals = kvp.Value;
                if (skel?.Bones == null) continue;
                for (int i = 0; i < skel.Bones.Length && i < originals.Length; i++)
                {
                    if (skel.Bones[i] != null) skel.Bones[i].BindTransformInv = originals[i];
                }
                skel.ResetBoneTransforms();
                skel.UpdateBoneTransforms();
            }
            _originalBinds.Clear();
            _binds.Clear();
            _boundAnim = null;
        }

        private void Apply(XapkAnimation anim, Skeleton skel, float t)
        {
            var bones = Bind(skel);
            var frames = Math.Max(1, anim.FrameCount - 1);
            var pos = anim.Duration > 0 ? (t / anim.Duration) * frames : 0.0f;
            pos = Math.Clamp(pos, 0, frames);
            var f0 = (int)pos;
            var f1 = Math.Min(f0 + 1, frames);
            var alpha = pos - f0;
            CurrentFrame = pos;

            HasCamera = false;
            MoverPosition = Vector3.Zero;
            MoverRotation = Quaternion.Identity;
            var tracks = 0;
            var matched = 0;

            foreach (var track in anim.Tracks)
            {
                tracks++;
                switch (track.TrackId)
                {
                    case XapkTrack.TrackCameraFov:
                        CameraFov = Lerp(track, f0, f1, alpha).X;
                        HasCamera = true;
                        continue;
                    case XapkTrack.TrackMoverTranslation:
                        MoverPosition = Lerp(track, f0, f1, alpha).AsVector3();
                        continue;
                    case XapkTrack.TrackMoverRotation:
                        MoverRotation = Normalize(Lerp(track, f0, f1, alpha));
                        continue;
                }

                if (bones.TryGetValue(track.BoneId, out var bone) == false || bone == null) continue;
                matched++;
                var v = Lerp(track, f0, f1, alpha);
                switch (track.TrackId)
                {
                    case XapkTrack.TrackTranslation:
                        //A cutscene animates the root bone in world space - the actor's position in
                        //the scene, thousands of units from the origin. Dropping that straight into
                        //the skeleton throws the model off the screen, so a root bone only gets the
                        //movement relative to where the animation started, and only when the user
                        //asked for root motion.
                        if (bone.Parent == null)
                        {
                            if (!EnableRootMotion) break;
                            bone.AnimTranslation = bone.Position + (v.AsVector3() - RootOrigin(track));
                            break;
                        }
                        bone.AnimTranslation = v.AsVector3();
                        break;
                    case XapkTrack.TrackRotation:
                        //Same story as the root translation: a cutscene stores the actor's world
                        //orientation on the root bone, roughly 180 degrees away from the bind pose
                        //here, which swings everything far from the hips - the body stays put and
                        //the head and hands fly off. Root bones only get rotation relative to the
                        //start of the animation, and only with root motion enabled.
                        if (bone.Parent == null)
                        {
                            if (!EnableRootMotion) break;
                            var delta = Normalize(v) * Quaternion.Inverse(RootRotationOrigin(track));
                            bone.AnimRotation = delta * bone.Rotation;
                            break;
                        }
                        bone.AnimRotation = Normalize(v);
                        break;
                    case 2: //scale
                        bone.AnimScale = v.AsVector3();
                        break;
                }
            }

            if (EnableRootMotion && bones.TryGetValue(0, out var root) && root != null
                && anim.Tracks.Exists(t => t.TrackId == XapkTrack.TrackMoverTranslation))
            {
                root.AnimTranslation += MoverPosition;
                root.AnimRotation = MoverRotation * root.AnimRotation;
            }

            TrackCount = tracks;
            MatchedTrackCount = matched;

            skel.AnimateRenderables = true;
            skel.UpdateBoneTransforms();
        }

        //Where the root track starts, so root motion can be played relative to it.
        private readonly Dictionary<XapkTrack, Vector3> _rootOrigins = [];
        private Vector3 RootOrigin(XapkTrack track)
        {
            if (_rootOrigins.TryGetValue(track, out var v)) return v;
            v = track.EvaluateVector4(0).AsVector3();
            _rootOrigins[track] = v;
            return v;
        }

        private readonly Dictionary<XapkTrack, Quaternion> _rootRotationOrigins = [];
        private Quaternion RootRotationOrigin(XapkTrack track)
        {
            if (_rootRotationOrigins.TryGetValue(track, out var q)) return q;
            q = Normalize(track.EvaluateVector4(0));
            _rootRotationOrigins[track] = q;
            return q;
        }

        private static Vector4 Lerp(XapkTrack track, int f0, int f1, float alpha)
        {
            var a = track.EvaluateVector4(f0);
            if (alpha <= 0.0f) return a;
            var b = track.EvaluateVector4(f1);
            return a + (b - a) * alpha;
        }

        private static Quaternion Normalize(Vector4 v)
        {
            var q = new Quaternion(v.X, v.Y, v.Z, v.W);
            var len = q.Length();
            return len > 0 ? Quaternion.Multiply(q, 1.0f / len) : Quaternion.Identity;
        }
    }

    internal static class XapkVectorExt
    {
        public static Vector3 AsVector3(this Vector4 v) => new(v.X, v.Y, v.Z);
    }
}
