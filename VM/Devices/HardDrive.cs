using System;
using System.IO;
using System.Xml.Linq;
using SFML.Graphics;

namespace VM.Devices
{
    class HardDrive : Device
    {
        public const int BytesPerSector = 512;
        public const ushort Version = 1;

        enum DeviceState : short
        {
            None,
            Identify,
            Read,
            Write,
            Error
        }

        enum ErrorCode : short
        {
            BadSector
        }

        private VirtualMachine vm;
        private FileStream diskImage;
        private ushort sectorCount;
        private short devPort;

        private ushort[] packet;
        private int packetOffset;

        private DeviceState state;
        private ErrorCode errorCode;

        public HardDrive(RenderWindow window, VirtualMachine virtualMachine, XElement config)
        {
            vm = virtualMachine;
            state = DeviceState.None;

            var errorMsg = "";

            try
            {
                errorMsg = "Bad Port";
                devPort = short.Parse(Util.ElementValue(config, "Port", null));

                errorMsg = "Bad FileName";
                var fileName = Util.ElementValue(config, "FileName", null);
                if (fileName == null)
                    throw new Exception();

                errorMsg = string.Format("Failed to open '{0}'", fileName);
                diskImage = new FileStream(fileName, FileMode.Open);
                sectorCount = (ushort)(new FileInfo(fileName).Length / BytesPerSector);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("HardDrive: {0}", errorMsg), e);
            }
        }

        public override void Attach(VirtualMachine machine)
        {
            machine.RegisterPortInHandler(devPort, () =>
            {
                switch (state)
                {
                    case DeviceState.Identify:
                        return IdentifyDevice();

                    case DeviceState.Error:
                        state = DeviceState.None;
                        return (short)errorCode;
                }

                return 0;
            });

            machine.RegisterPortOutHandler(devPort, data =>
            {
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
            });
        }

        public override void Reset()
        {
            state = DeviceState.None;
            packetOffset = 0;
        }

        private short IdentifyDevice()
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
                return 0;

            return (short)packet[packetOffset++];
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
                var address = packet[0];
                var sector = packet[1];

                if (sector >= sectorCount)
                {
                    errorCode = ErrorCode.BadSector;
                    state = DeviceState.Error;
                    return;
                }

                var buffer = new byte[BytesPerSector];
                diskImage.Seek(sector * BytesPerSector, SeekOrigin.Begin);
                diskImage.Read(buffer, 0, BytesPerSector);

                for (var i = 0; i < buffer.Length; i++)
                {
                    vm.Memory[address + i] = buffer[i];
                }

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
                var address = packet[0];
                var sector = packet[1];

                if (sector >= sectorCount)
                {
                    errorCode = ErrorCode.BadSector;
                    state = DeviceState.Error;
                    return;
                }

                var buffer = new byte[BytesPerSector];
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = vm.Memory[address + i];
                }

                diskImage.Seek(sector * BytesPerSector, SeekOrigin.Begin);
                diskImage.Write(buffer, 0, BytesPerSector);

                state = DeviceState.None;
            }
        }
    }
}
