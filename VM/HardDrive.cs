using System;
using System.IO;

namespace VM
{
	class HardDrive : IDevice
	{
		public const int BytesPerSector = 512;
		public const int Port = 200;
		public const ushort Version = 1;

		enum DeviceState : short
		{
			None,
			Identify,
			Read,
			Write
		}

		private readonly VirtualMachine vm;
		private readonly FileStream diskImage;
		private ushort[] packet;
		private int packetOffset;

		private DeviceState state;
		private readonly ushort sectorCount;

		public HardDrive(VirtualMachine vm, string filename)
		{
			this.vm = vm;
			state = DeviceState.None;

			try
			{
				diskImage = new FileStream(filename, FileMode.Open);
				sectorCount = (ushort)(new FileInfo(filename).Length / BytesPerSector);
			}
			catch (Exception e)
			{
				throw new VmException("Cannot initialize device HardDrive", e);
			}
		}

		public void DataReceived(short port, short data)
		{
			if (port != Port)
				return;

			switch (state)
			{
				case DeviceState.Read:
					ReadDevice((ushort)data);
					break;

				case DeviceState.Write:
					WriteDevice((ushort)data);
					break;

				default:
					state = (DeviceState)data;
					packetOffset = 0;
					packet = null;
					break;
			}
		}

		public short? DataRequested(short port)
		{
			if (port != Port || state == DeviceState.None)
				return null;

			switch (state)
			{
				case DeviceState.Identify:
					return IdentifyDevice();
			}

			return null;
		}

		private short? IdentifyDevice()
		{
			const int packetSize = 4;

			/*
			 *	Identify Packet
			 *		WORD			DESCRIPTION
			 *		0000h			Status Word
			 *		0000h			Disk size in sectors
			 *		0001h			Size of sector in bytes
			 *		0002h			Device index
			 *		
			 *	Status Word
			 *		HI BYTE			Version
			 *		LO BYTE			Bit 7		Signifies device present
			 *						Bit 0:6		Reserved (Flags in the future?)
			 */

			if (packet == null)
			{
				packet = new ushort[packetSize];
				packet[0] = (Version << 8) | 0x80;
				packet[1] = sectorCount;
				packet[2] = BytesPerSector;
				packet[3] = 0;

				packetOffset = 0;
			}

			if (packetOffset > packetSize)
				return null;

			return (short?)packet[packetOffset++];
		}

		private void ReadDevice(ushort data)
		{
			const int packetSize = 2;

			/*
			 *	Read Packet
			 *		WORD			DESCRIPTION
			 *		0000h			Address
			 *		0001h			LBA
			 */

			if (packet == null)
				packet = new ushort[packetSize];

			packet[packetOffset++] = data;

			if (packetOffset >= packetSize)
			{
				ushort address = packet[0];
				ushort sector = packet[1];

				// TODO Error codes?
				if (sector > sectorCount)
					return;

				byte[] buffer = new byte[BytesPerSector];
				diskImage.Seek(sector * BytesPerSector, SeekOrigin.Begin);
				diskImage.Read(buffer, 0, BytesPerSector);
				for (int i = 0; i < buffer.Length; ++i)
					vm.Memory[address + i] = buffer[i];

				state = DeviceState.None;
			}
		}

		private void WriteDevice(ushort data)
		{
			const int packetSize = 2;

			/*
			 *	Read Packet
			 *		WORD			DESCRIPTION
			 *		0000h			Address
			 *		0001h			LBA
			 */

			if (packet == null)
				packet = new ushort[packetSize];

			packet[packetOffset++] = data;

			if (packetOffset >= packetSize)
			{
				ushort address = packet[0];
				ushort sector = packet[1];

				// TODO Error codes?
				if (sector > sectorCount)
					return;

				byte[] buffer = new byte[BytesPerSector];
				for (int i = 0; i < buffer.Length; ++i)
					buffer[i] = vm.Memory[address + i];

				diskImage.Seek(sector * BytesPerSector, SeekOrigin.Begin);
				diskImage.Write(buffer, 0, BytesPerSector);

				state = DeviceState.None;
			}
		}
	}
}
