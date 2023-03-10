using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.CrestronSPlus.SPlusShims;
using ICD.Connect.Displays.SPlus.Devices.Simpl;
using ICD.Connect.Displays.SPlus.EventArgs;

namespace ICD.Connect.Displays.SPlus.Shims
{
	public delegate void SPlusDisplayShimPowerOnCallback();

	public delegate void SPlusDisplayShimPowerOffCallback();

	public delegate void SPlusDisplayShimSetActiveInputCallback(ushort activeInput);

	public abstract class AbstractSPlusDisplayShim<TOriginator> : AbstractSPlusDeviceShim<TOriginator>
		where TOriginator : class, ISimplDisplay
	{

		#region Callbacks

		[PublicAPI("S+")]
		public SPlusDisplayShimPowerOnCallback PowerOnCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayShimPowerOffCallback PowerOffCallback { get; set; }

		[PublicAPI("S+")]
		public SPlusDisplayShimSetActiveInputCallback SetActiveInputCallback { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the powered state.
		/// </summary>
		[PublicAPI("S+")]
		public void SetPowerFeedback(ushort power)
		{
				TOriginator originator = Originator;
				if (originator == null)
					return;

				originator.SetPowerFeedback(power.ToBool());
		}

		/// <summary>
		/// Gets the active input.
		/// </summary>
		[PublicAPI("S+")]
		public void SetActiveInputFeedback(ushort activeInput)
		{
			
				TOriginator originator = Originator;
				if (originator == null)
					return;

				// Input 0 turns into null
				int? input = (activeInput == 0) ? (int?)null : activeInput;

				originator.SetActiveInputFeedback(input);
		}

		#endregion

		#region Originator Callbacks

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(TOriginator originator)
		{
			base.Subscribe(originator);

			if (originator == null)
				return;

			originator.OnSetPower += OriginatorOnSetPower;
			originator.OnSetActiveInput += OriginatorOnSetActiveInput;
		}

		/// <summary>
		/// Unsubscribe from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(TOriginator originator)
		{
			base.Unsubscribe(originator);

			if (originator == null)
				return;

			originator.OnSetPower -= OriginatorOnSetPower;
			originator.OnSetActiveInput -= OriginatorOnSetActiveInput;
		}

		private void OriginatorOnSetPower(object sender, SetPowerApiEventArgs args)
		{
			if (args.Data)
			{
				// Power On
				SPlusDisplayShimPowerOnCallback callback = PowerOnCallback;
				if (callback != null)
					callback();
			}
			else
			{
				// Power Off
				SPlusDisplayShimPowerOffCallback callback = PowerOffCallback;
				if (callback != null)
					callback();
			}
		}

		private void OriginatorOnSetActiveInput(object sender, SetActiveInputApiEventArgs args)
		{
			SPlusDisplayShimSetActiveInputCallback callback = SetActiveInputCallback;
			if (callback != null)
				callback((ushort)args.Data);
		}

		#endregion
	}
}
