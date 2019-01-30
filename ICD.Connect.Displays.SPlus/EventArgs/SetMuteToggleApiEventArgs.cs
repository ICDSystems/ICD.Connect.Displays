using ICD.Connect.API.EventArguments;
using ICD.Connect.API.Info;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetMuteToggleApiEventArgs : AbstractApiEventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public SetMuteToggleApiEventArgs() : base(SPlusDisplayApi.EVENT_SET_MUTE_TOGGLE)
		{
		}

		/// <summary>
		/// Builds an API result for the args.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override void BuildResult(object sender, ApiResult result)
		{
		}
	}
}