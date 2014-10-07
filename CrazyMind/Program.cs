using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CrazyflieDotNet.Crazyradio.Driver;
using ThinkGearNET;

namespace CrazyMind
{
	class Program
	{
		/**
		 * Read more at http://bazookian.net/2014/10/07/control-the-crazyflie-quadcopter-with-your-mind.
		 */

		const ushort MIN_THRUST = 10001;
		const ushort MAX_THRUST = 60000;

		private static ushort Thrust = 0;

		private static void Main(string[] args)
		{
			ICrazyradioDriver crazyDriver = null;
			ThinkGearWrapper brain = null;

			try
			{
				brain = OpenMindWave();
				brain.ThinkGearChanged += BrainChanged;

				crazyDriver = GetCrazyDriver();
				OpenCrazyDriver(crazyDriver);

				MainLoop(crazyDriver);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				CloseCrazyDriver(crazyDriver);
			}

			Console.WriteLine("Press any key to exit...");
			Console.ReadLine();
		}

		private static void MainLoop(ICrazyradioDriver driver)
		{
			while (true)
			{
				var packet = GetCommandPacket(Thrust, 0, 0, 0);
				driver.SendData(packet);

				// as per recommendation
				Thread.Sleep(10);
			}
		}

		private static void BrainChanged(object sender, ThinkGearChangedEventArgs e)
		{
			// get attention level as a percentage
			float attentionPercentage = e.ThinkGearState.Attention / 100.0f;
			
			// calculate the thrust of the crazyflie (10001 - 60000)
			Thrust = (ushort)(MIN_THRUST + (ushort)((MAX_THRUST - MIN_THRUST) * attentionPercentage));

			Console.WriteLine(attentionPercentage);
		}

		private static ThinkGearWrapper OpenMindWave()
		{
			var brain = new ThinkGearWrapper();

			// hard-coded COM port for the mindwave
			if (!brain.Connect("COM6", 57600, true))
				throw new Exception("Failed to connect to mindwave");

			//brain.EnableBlinkDetection(true);

			return brain;
		}

		private static ICrazyradioDriver GetCrazyDriver()
		{
			IEnumerable<ICrazyradioDriver> crazyradioDrivers = null;

			crazyradioDrivers = CrazyradioDriver.GetCrazyradios();

			if (crazyradioDrivers != null && crazyradioDrivers.Any())
				return crazyradioDrivers.First();

			throw new Exception("No Crazyradio USB dongles found!");
		}

		private static void OpenCrazyDriver(ICrazyradioDriver driver)
		{
			driver.Open();

			// hard-code the channel and data rate (didn't work to scan all the time)
			driver.DataRate = RadioDataRate.DataRate250Kps;
			driver.Channel = RadioChannel.Channel10;
		}

		private static void CloseCrazyDriver(ICrazyradioDriver driver)
		{
			if (driver != null)
				driver.Close();
		}

		private static byte[] GetCommandPacket(ushort thrust, float roll, float pitch, float yaw)
		{
			// command packet protocol shamelessly stolen from https://github.com/Zagitta/CrazySharp
			var rollBytes = BitConverter.GetBytes(roll);
			var pitchBytes = BitConverter.GetBytes(pitch);
			var yawBytes = BitConverter.GetBytes(yaw);
			var thrustBytes = BitConverter.GetBytes(thrust);

			return new byte[]
            {
                60, // hard-coded command packet header
                rollBytes[0],
                rollBytes[1],
                rollBytes[2],
                rollBytes[3],
                pitchBytes[0],
                pitchBytes[1],
                pitchBytes[2],
                pitchBytes[3],
                yawBytes[0],
                yawBytes[1],
                yawBytes[2],
                yawBytes[3],
                thrustBytes[0],
                thrustBytes[1]
            };
		}
	}
}
