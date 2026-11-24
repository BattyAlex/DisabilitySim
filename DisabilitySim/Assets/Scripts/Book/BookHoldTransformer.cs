using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace DisabilitySim
{
   
    public class BookHoldTransformer : XRBaseGrabTransformer
    {
        static readonly Vector3 k_HoldPoint = new Vector3(0f, -0.025f, -0.25f);

        const float k_ReadingDistance = 0.5f;

        const float k_TurnSpeed = 10f;

        Vector3 m_HoldPoint;
        Vector3 m_Push;
        Quaternion m_Rotation;

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);

            var book = grabInteractable.transform;
            var interactor = grabInteractable.interactorsSelecting[0];
            m_Rotation = book.rotation;
            m_HoldPoint = book.InverseTransformPoint(interactor.GetAttachTransform(grabInteractable).position);
            m_Push = Vector3.zero;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic && updatePhase != XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
                return;

            var hand = grabInteractable.interactorsSelecting[0].GetAttachTransform(grabInteractable).position;
            var head = Camera.main;

            if (head != null && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                var blend = 1f - Mathf.Exp(-k_TurnSpeed * Time.deltaTime);
                var eyes = head.transform;
                var center = hand - m_Rotation * m_HoldPoint;

                var depth = Vector3.Dot(center - eyes.position, eyes.forward);
                var push = eyes.forward * (k_ReadingDistance - depth);

                m_Rotation = Quaternion.Slerp(m_Rotation, GetReadingRotation(center + push, eyes), blend);
                m_HoldPoint = Vector3.Lerp(m_HoldPoint, k_HoldPoint, blend);
                m_Push = Vector3.Lerp(m_Push, push, blend);
            }

            targetPose.rotation = m_Rotation;
            targetPose.position = hand - m_Rotation * m_HoldPoint + m_Push;
        }

        static Quaternion GetReadingRotation(Vector3 center, Transform head)
        {
            var toHead = (head.position - center).normalized;

            var flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            var pageUp = Vector3.ProjectOnPlane(Vector3.up + flatForward * 0.2f, toHead);
            if (pageUp.sqrMagnitude < 0.0001f)
                pageUp = Vector3.ProjectOnPlane(head.up, toHead);

            return Quaternion.LookRotation(pageUp, toHead);
        }
    }
}
