using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetVolumeIncrementApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data">true for increment/up, false for decrement/down</param>
		public SetVolumeIncrementApiEventArgs(bool data) : base(SPlusDisplayApi.EVENT_SET_VOLUME_INCREMENT, data)
		{
		}
	}
}