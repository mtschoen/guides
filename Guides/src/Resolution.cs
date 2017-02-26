using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Guides
{
	public class Resolution {
		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
		struct DEVMODE {
			public const int CCHDEVICENAME = 32;
			public const int CCHFORMNAME = 32;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
			[FieldOffset(0)]
			public string dmDeviceName;
			[FieldOffset(32)]
			public short dmSpecVersion;
			[FieldOffset(34)]
			public short dmDriverVersion;
			[FieldOffset(36)]
			public short dmSize;
			[FieldOffset(38)]
			public short dmDriverExtra;
			[FieldOffset(40)]
			public DM dmFields;

			[FieldOffset(44)]
			short dmOrientation;
			[FieldOffset(46)]
			short dmPaperSize;
			[FieldOffset(48)]
			short dmPaperLength;
			[FieldOffset(50)]
			short dmPaperWidth;
			[FieldOffset(52)]
			short dmScale;
			[FieldOffset(54)]
			short dmCopies;
			[FieldOffset(56)]
			short dmDefaultSource;
			[FieldOffset(58)]
			short dmPrintQuality;

			[FieldOffset(44)]
			public POINTL dmPosition;
			[FieldOffset(52)]
			public int dmDisplayOrientation;
			[FieldOffset(56)]
			public int dmDisplayFixedOutput;

			[FieldOffset(60)]
			public short dmColor; // See note below!
			[FieldOffset(62)]
			public short dmDuplex; // See note below!
			[FieldOffset(64)]
			public short dmYResolution;
			[FieldOffset(66)]
			public short dmTTOption;
			[FieldOffset(68)]
			public short dmCollate; // See note below!
			[FieldOffset(72)]
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
			public string dmFormName;
			[FieldOffset(102)]
			public short dmLogPixels;
			[FieldOffset(104)]
			public int dmBitsPerPel;
			[FieldOffset(108)]
			public int dmPelsWidth;
			[FieldOffset(112)]
			public int dmPelsHeight;
			[FieldOffset(116)]
			public int dmDisplayFlags;
			[FieldOffset(116)]
			public int dmNup;
			[FieldOffset(120)]
			public int dmDisplayFrequency;
		}
		struct POINTL {
			public int x;
			public int y;
		}
		[Flags]
		enum DM {
			Orientation = 0x1,
			PaperSize = 0x2,
			PaperLength = 0x4,
			PaperWidth = 0x8,
			Scale = 0x10,
			Position = 0x20,
			NUP = 0x40,
			DisplayOrientation = 0x80,
			Copies = 0x100,
			DefaultSource = 0x200,
			PrintQuality = 0x400,
			Color = 0x800,
			Duplex = 0x1000,
			YResolution = 0x2000,
			TTOption = 0x4000,
			Collate = 0x8000,
			FormName = 0x10000,
			LogPixels = 0x20000,
			BitsPerPixel = 0x40000,
			PelsWidth = 0x80000,
			PelsHeight = 0x100000,
			DisplayFlags = 0x200000,
			DisplayFrequency = 0x400000,
			ICMMethod = 0x800000,
			ICMIntent = 0x1000000,
			MediaType = 0x2000000,
			DitherType = 0x4000000,
			PanningWidth = 0x8000000,
			PanningHeight = 0x10000000,
			DisplayFixedOutput = 0x20000000
		}
		public const int ENUM_CURRENT_SETTINGS = -1;
		[DllImport("user32.dll")]
		static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

		//http://stackoverflow.com/questions/18832991/enumdisplaydevices-not-returning-anything
		[Flags]
		public enum DisplayDeviceStateFlags {
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

		public int x, y, offsetX, offsetY;

		public static Dictionary<string, Resolution> GetResolutions() {

			Dictionary<string, Resolution> resolutions = new Dictionary<string, Resolution>();

			DISPLAY_DEVICE dd = new DISPLAY_DEVICE();

			dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));

			uint deviceNum = 0;
			while (EnumDisplayDevices(null, deviceNum, ref dd, 0)) {
				//DumpDevice(dd);
				DEVMODE dm = new DEVMODE();
				dm.dmDeviceName = new string(new char[32]);
				dm.dmFormName = new string(new char[32]);
				dm.dmSize = (short)Marshal.SizeOf(dm);
				if (0 != EnumDisplaySettings(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm)) {
					//We have a monitor, and here's the resolution.
					//Debug.WriteLine(dd.DeviceName + ", " + dm.dmPelsWidth);
					resolutions[dd.DeviceName] = new Resolution { x = dm.dmPelsWidth, y = dm.dmPelsHeight, offsetX = dm.dmPosition.x, offsetY = dm.dmPosition.y };
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
