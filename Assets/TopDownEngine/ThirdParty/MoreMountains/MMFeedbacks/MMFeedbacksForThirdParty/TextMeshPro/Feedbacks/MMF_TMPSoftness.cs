﻿using MoreMountains.Tools;
using UnityEngine;
using System.Collections;
#if (MM_TEXTMESHPRO || MM_UGUI2)
using TMPro;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback lets you tweak the softness of a TMP text over time
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback lets you tweak the softness of a TMP text over time.")]
	#if (MM_TEXTMESHPRO || MM_UGUI2)
	[FeedbackPath("TextMesh Pro/TMP Softness")]
	#endif
	[MovedFrom(false, null, "MoreMountains.Feedbacks.TextMeshPro")]
	public class MMF_TMPSoftness : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		public static bool FeedbackTypeAuthorized = true;
		
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.TMPColor; } }
		public override string RequiresSetupText { get { return "This feedback requires that a TargetTMPText be set to be able to work properly. You can set one below."; } }
		#endif
		#if UNITY_EDITOR && (MM_TEXTMESHPRO || MM_UGUI2)
		public override bool EvaluateRequiresSetup() { return (TargetTMPText == null); }
		public override string RequiredTargetText { get { return TargetTMPText != null ? TargetTMPText.name : "";  } }
		#endif
		public override bool HasCustomInspectors => true;
        
		/// the duration of this feedback is the duration of the transition, or 0 if instant
		public override float FeedbackDuration { get { return (Mode == MMFeedbackBase.Modes.Instant) ? 0f : ApplyTimeMultiplier(Duration); } set { Duration = value; } }

		#if (MM_TEXTMESHPRO || MM_UGUI2)
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetTMPText = FindAutomatedTarget<TMP_Text>();

		[MMFInspectorGroup("Target", true, 12, true)]
		/// the TMP_Text component to control
		[Tooltip("the TMP_Text component to control")]
		public TMP_Text TargetTMPText;
		#endif

		[MMFInspectorGroup("Softness", true, 13)]
		/// whether or not values should be relative
		[Tooltip("whether or not values should be relative")]
		public bool RelativeValues = true;
		/// the selected mode
		[Tooltip("the selected mode")]
		public MMFeedbackBase.Modes Mode = MMFeedbackBase.Modes.OverTime;
		/// the duration of the feedback, in seconds
		[Tooltip("the duration of the feedback, in seconds")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float Duration = 0.5f;
		/// the curve to tween on
		[Tooltip("the curve to tween on")]
		public MMTweenType SoftnessCurve = new MMTweenType(new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.3f, 1f), new Keyframe(1, 0f)), "", "Mode", (int)MMFeedbackBase.Modes.OverTime);
		/// the value to remap the curve's 0 to
		[Tooltip("the value to remap the curve's 0 to")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapZero = 0f;
		/// the value to remap the curve's 1 to
		[Tooltip("the value to remap the curve's 1 to")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.OverTime)]
		public float RemapOne = 1f;
		/// the value to move to in instant mode
		[Tooltip("the value to move to in instant mode")]
		[MMFEnumCondition("Mode", (int)MMFeedbackBase.Modes.Instant)]
		public float InstantSoftness;
		/// if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over
		[Tooltip("if this is true, calling that feedback will trigger it, even if it's in progress. If it's false, it'll prevent any new Play until the current one is over")] 
		public bool AllowAdditivePlays = false;

		protected float _initialSoftness;
		protected Coroutine _coroutine;

		/// <summary>
		/// On init we grab our initial softness
		/// </summary>
		/// <param name="owner"></param>
		protected override void CustomInitialization(MMF_Player owner)
		{
			base.CustomInitialization(owner);

			if (!Active)
			{
				return;
			}
			#if (MM_TEXTMESHPRO || MM_UGUI2)
			if (TargetTMPText == null)
			{
				Debug.LogWarning("[TMP Softness Feedback] The TMP Softness feedback on "+Owner.name+" doesn't have a TargetTMPText, it won't work. You need to specify one in its inspector.");
				return;
			}
			_initialSoftness = TargetTMPText.fontMaterial.GetFloat(ShaderUtilities.ID_OutlineSoftness);
			#endif
		}

		/// <summary>
		/// On Play we animate our softness
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			#if (MM_TEXTMESHPRO || MM_UGUI2)
			if (TargetTMPText == null)
			{
				return;
			}

			if (Active && FeedbackTypeAuthorized)
			{
				switch (Mode)
				{
					case MMFeedbackBase.Modes.Instant:
						float newSoftness = NormalPlayDirection ? InstantSoftness : _initialSoftness;
						TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, InstantSoftness);
						TargetTMPText.UpdateMeshPadding();
						break;
					case MMFeedbackBase.Modes.OverTime:
						if (!AllowAdditivePlays && (_coroutine != null))
						{
							return;
						}
						if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
						_coroutine = Owner.StartCoroutine(ApplyValueOverTime());
						break;
				}
			}
			#endif
		}
		
		/// <summary>
		/// Stops the animation if needed
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			base.CustomStopFeedback(position, feedbacksIntensity);
			IsPlaying = false;
			if (_coroutine != null)
			{
				Owner.StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}

		protected virtual IEnumerator ApplyValueOverTime()
		{
			float journey = NormalPlayDirection ? 0f : FeedbackDuration;
			IsPlaying = true;
			while ((journey >= 0) && (journey <= FeedbackDuration) && (FeedbackDuration > 0))
			{
				float remappedTime = MMFeedbacksHelpers.Remap(journey, 0f, FeedbackDuration, 0f, 1f);

				SetValue(remappedTime);

				journey += NormalPlayDirection ? FeedbackDeltaTime : -FeedbackDeltaTime;
				yield return null;
			}
			SetValue(FinalNormalizedTime);
			_coroutine = null;
			IsPlaying = false;
			yield return null;
		}

		protected virtual void SetValue(float time)
		{
			#if (MM_TEXTMESHPRO || MM_UGUI2)
			float intensity = MMTween.Tween(time, 0f, 1f, RemapZero, RemapOne, SoftnessCurve);
			float newValue = intensity;
			if (RelativeValues)
			{
				newValue += _initialSoftness;
			}
			TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, newValue);
			TargetTMPText.UpdateMeshPadding();
			#endif
		}
		
		/// <summary>
		/// On restore, we put our object back at its initial position
		/// </summary>
		protected override void CustomRestoreInitialValues()
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}
			#if (MM_TEXTMESHPRO || MM_UGUI2)
			TargetTMPText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, _initialSoftness);
			TargetTMPText.UpdateMeshPadding();
			#endif
		}
		
		/// <summary>
		/// On Validate, we init our curves conditions if needed
		/// </summary>
		public override void OnValidate()
		{
			base.OnValidate();
			if (string.IsNullOrEmpty(SoftnessCurve.EnumConditionPropertyName))
			{
				SoftnessCurve.EnumConditionPropertyName = "Mode";
				SoftnessCurve.EnumConditions = new bool[32];
				SoftnessCurve.EnumConditions[(int)MMFeedbackBase.Modes.OverTime] = true;
			}
		}
	}
}