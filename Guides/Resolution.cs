using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Guides {
	public class Resolution {
		[StructLayout(LayoutKind.Sequential)]
		public struct DEVMODE {
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmDeviceName;
			public short dmSpecVersion;
			public short dmDriverVersion;
			public short dmSize;
			public short dmDriverExtra;
			public int dmFields;

			public short dmOrientation;
			public short dmPaperSize;
			public short dmPaperLength;
			public short dmPaperWidth;

			public short dmScale;
			public short dmCopies;
			public short dmDefaultSource;
			public short dmPrintQuality;
			public short dmColor;
			public short dmDuplex;
			public short dmYResolution;
			public short dmTTOption;
			public short dmCollate;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmFormName;
			public short dmLogPixels;
			public short dmBitsPerPel;
			public int dmPelsWidth;
			public int dmPelsHeight;

			public int dmDisplayFlags;
			public int dmDisplayFrequency;

			public int dmICMMethod;
			public int dmICMIntent;
			public int dmMediaType;
			public int dmDitherType;
			public int dmReserved1;
			public int dmReserved2;

			public int dmPanningWidth;
			public int dmPanningHeight;
		};
		public const int ENUM_CURRENT_SETTINGS = -1;
		[DllImport("user32.dll")]
		public static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

		//http://stackoverflow.com/questions/18832991/enumdisplaydevices-not-returning-anything
		[Flags()]
		public enum DisplayDeviceStateFlags : int {
			/// <summary>The device is part of the desktop.</summary>
			AttachedToDesktop = 0x1,
			MultiDriver = 0x2,
			/// <summary>The device is part of the desktop.</summary>
			PrimaryDevice = 0x4,
			/// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
			MirroringDriver = 0x8,
			/// <summary>The device is VGA compatible.</summary>
			VGACompatible = 0x10,
			/// <summary>The device is removable; it cannot be the primary display.</summary>
			Removable = 0x20,
			/// <summary>The device has more display modes than its output devices support.</summary>
			ModesPruned = 0x8000000,
			Remote = 0x4000000,
			Disconnect = 0x2000000
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct DISPLAY_DEVICE {
			[MarshalAs(UnmanagedType.U4)]
			public int cb;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceString;
			[MarshalAs(UnmanagedType.U4)]
			public DisplayDeviceStateFlags StateFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceID;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceKey;
		}
		[DllImport("user32.dll")]
		static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

		public int x, y;

		public static Dictionary<string, Resolution> GetResolutions() {

			Dictionary<string, Resolution> resolutions = new Dictionary<string, Resolution>();

			DISPLAY_DEVICE dd = new DISPLAY_DEVICE();

			dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));

			uint deviceNum = 0;
			while (EnumDisplayDevices(null, deviceNum, ref dd, 0)) {
				//DumpDevice(dd);
				DEVMODE dm = new DEVMODE();
				dm.dmDeviceName = new String(new char[32]);
				dm.dmFormName = new String(new char[32]);
				dm.dmSize = (short)Marshal.SizeOf(dm);
				if (0 != EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm)) {
					//We have a monitor, and here's the resolution.
					//Debug.WriteLine(dd.DeviceName + ", " + dm.dmPelsWidth);
					resolutions[dd.DeviceName] = new Resolution { x = dm.dmPelsWidth, y = dm.dmPelsHeight };
				}

				DISPLAY_DEVICE newdd = new DISPLAY_DEVICE();
				newdd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
				uint monitorNum = 0;
				while (EnumDisplayDevices(dd.DeviceName, monitorNum, ref newdd, 0)) {
					//DumpDevice(newdd);
					
					if (0 != EnumDisplaySettings(newdd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm)) {
						//Usually don't find monitors here
						Debug.WriteLine("found a monitor here?");
					}
					monitorNum++;
				}
				deviceNum++;
			}

			return resolutions;
		}
		public static void DumpDevice(DISPLAY_DEVICE dd) {
			Debug.WriteLine(dd.DeviceName);
			Debug.WriteLine(dd.DeviceString);
			Debug.WriteLine(dd.StateFlags);
			Debug.WriteLine(dd.DeviceID);
			Debug.WriteLine(dd.DeviceKey + 42);
		}
	}
}
