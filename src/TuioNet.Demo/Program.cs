﻿using TuioNet.Common;
using TuioNet.Tuio11;

namespace TuioNet.Demo;

class Program
{
    private static void Main(string[] args)
    {
        var tuioClient = new Tuio11Client(TuioConnectionType.UDP);
        tuioClient.AddTuioListener(new TuioListener());
        Console.WriteLine("Connect...");
        tuioClient.Connect();
        while (true)
        {
            if (!Console.KeyAvailable) continue;
            var pressedKey = Console.ReadKey().Key;
            if (pressedKey == ConsoleKey.Q) break;
        }
        Console.WriteLine("Disconnect...");
        tuioClient.Disconnect();
    }
}

