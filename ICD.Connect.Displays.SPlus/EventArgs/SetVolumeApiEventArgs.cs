using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.SPlus.Proxy;

namespace ICD.Connect.Displays.SPlus.EventArgs
{
	public sealed class SetVolumeApiEventArgs : AbstractGenericApiEventArgs<float>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SetVolumeApiEventArgs(float data) : base(SPlusDisplayApi.EVENT_SET_VOLUME, data)
		{
		}
	}
}