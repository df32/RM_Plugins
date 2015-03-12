using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainmeter;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace MediaElementNs
{
	internal partial class Measure
    {
		//Rainmeter.API rm;
		MediaWindow _MediaWindow;
		bool hasInitialized = false;

		internal Measure(Rainmeter.API rm)
		{
			//Debug.Listeners.Add(
			//	new TextWriterTraceListener("debug.log"));
			//Debug.AutoFlush = true;

			//this.rm = rm;
			_MediaWindow = new MediaWindow(rm);
			//_MediaWindow.Show();
		}

		internal void Reload(Rainmeter.API rm, ref double maxValue)
		{
			if (!hasInitialized)
			{
				//_MediaWindow.Show();
				//_MediaWindow.Visibility = Visibility.Hidden;
				_MediaWindow.LoadOptions(rm);
				hasInitialized = true;
			}

			maxValue = 1;
		}

		internal double Update()
		{
			return _MediaWindow.GetPosition();
		}

		internal string GetString()
		{
			return _MediaWindow.GetSourceUrl();
		}

		internal void ExecuteBang(string args)
		{
			var arglist = args.Split('|');

			foreach (var arg in arglist)
			{
				var _a = arg.Trim().ToLower();

				if (_a == "reload")
				{
					_MediaWindow.LoadOptions();
					hasInitialized = true;
					continue;
				}

				_MediaWindow.Execute(arg);
			}
		}

		internal void Dispose()
		{
			_MediaWindow.Close();
			_MediaWindow = null;
		}

    }


	#region Plugin

	public static class Plugin
	{
		static IntPtr StringBuffer = IntPtr.Zero;

		[DllExport]
		public static void Initialize(ref IntPtr data, IntPtr rm)
		{
			data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure(new Rainmeter.API(rm))));
		}

		[DllExport]
		public static void Finalize(IntPtr data)
		{
			Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
			measure.Dispose();

			GCHandle.FromIntPtr(data).Free();

			if (StringBuffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(StringBuffer);
				StringBuffer = IntPtr.Zero;
			}
		}

		[DllExport]
		public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
		{
			Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
			measure.Reload(new Rainmeter.API(rm), ref maxValue);
		}

		[DllExport]
		public static double Update(IntPtr data)
		{
			Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
			return measure.Update();
		}

		[DllExport]
		public static IntPtr GetString(IntPtr data)
		{
			Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
			if (StringBuffer != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(StringBuffer);
				StringBuffer = IntPtr.Zero;
			}

			string stringValue = measure.GetString();
			if (stringValue != null)
			{
				StringBuffer = Marshal.StringToHGlobalUni(stringValue);
			}

			return StringBuffer;
		}

		[DllExport]
		public static void ExecuteBang(IntPtr data, IntPtr args)
		{
			Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
			measure.ExecuteBang(Marshal.PtrToStringUni(args));
		}
	}

	#endregion
}
