// Copyright (c) Kodi Studios 2021.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MidiCSharpConsole
{
    static class NativeMethods
    {
        [DllImport("winmm.dll")]
        public static extern uint midiOutOpen(out IntPtr lphMidiOut, uint uDeviceID, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll")]
        public static extern uint midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);

        [DllImport("winmm.dll")]
        public static extern uint midiOutClose(IntPtr hMidiOut);
    }

    class Program
    {
        // Plays Midi Note
        // To Stop playing, set velocity parameter to 0
        static void SendMidiNote(
            IntPtr hMidiOut,
            byte channel,  // 4 bits, 0 to 15
            byte pitch,    // 7 bits, 0 to 127
            byte velocity  // 7 bits, 0 to 127
        )
        {
            // "Note On" Protocol:
            // [0] Status byte     : 0b 1001 CCCC
            //     Note On Signature   : 0b 1001
            //     Channel 4-bits      : 0b CCCC
            // [1] Pitch 7-bits    : 0b 0PPP PPPP
            // [2] Velocity 7-bits : 0b 0VVV VVVV
            // [3] Unused          : 0b 0000 0000
            // Reference: https://www.cs.cmu.edu/~music/cmsip/readings/MIDI%20tutorial%20for%20programmers.html

            // To Turn "Note Off", simply pass 0 as Velocity (Volume)

            const byte NoteOnSignature = 0b1001;
            byte statusByte = NoteOnSignature;         // 0b 0000 1001
            statusByte = (byte)(statusByte << 4);      // 0b 1001 0000
            statusByte = (byte)(statusByte | channel); // 0b 1001 CCCC

            byte[] bData = new byte[4];
            bData[0] = statusByte;  // MIDI Status byte
            bData[1] = pitch;       // First MIDI data byte
            bData[2] = velocity;    // Second MIDI data byte
            // Byte [3] is unused

            // Midi message is 4 bytes
            // Windows Midi midiOutShortMsg Api passes
            // those 4 bytes as DWORD type.
            uint dwData = BitConverter.ToUInt32(bData, /*startIndex*/ 0);

            NativeMethods.midiOutShortMsg(hMidiOut, dwData);
        }

        static void SelectMidiInstrument(
            IntPtr hMidiOut,
            byte channel,       // 4 bits, 0 to 15
            byte instrument     // 7 bits, 0 to 127
        )
        {
            // Set Midi Instrument Protocol:
            // [0] Status byte          : 0b 1100 CCCC
            //     Set Instrument Signature      : 0b 1100
            //     Channel 4-bits                : 0b CCCC
            // [1] Instrument 7-bits    : 0b 0III IIII
            // [2] Unused               : 0b 0000 0000
            // [3] Unused               : 0b 0000 0000

            const byte SetInstrumentSignature = 0b1100;
            byte statusByte = SetInstrumentSignature;  // 0b 0000 1100
            statusByte = (byte)(statusByte << 4);      // 0b 1100 0000
            statusByte = (byte)(statusByte | channel); // 0b 1100 CCCC

            byte[] bData = new byte[4];

            bData[0] = statusByte;       // MIDI Status byte
            bData[1] = instrument;       // First MIDI data byte
            // Bytes [2] and [3] are unused

            // Midi message is 4 bytes
            // Windows Midi midiOutShortMsg Api passes
            // those 4 bytes as DWORD type.
            uint dwData = BitConverter.ToUInt32(bData, /*startIndex*/ 0);

            NativeMethods.midiOutShortMsg(hMidiOut, dwData);
        }

        static void Main()
        {
            // Open Midi Handle
            NativeMethods.midiOutOpen(
                out IntPtr hMidiOut,
                /*uDeviceID*/ 0, // System's Midi device is at index 0
                /*dwCallback*/ IntPtr.Zero,
                /*dwInstance*/ IntPtr.Zero,
                /*fdwOpen*/ 0);

            Console.Write("Select Instrument\n");
            // Set Instruments for Channels 0 and 1
            SelectMidiInstrument(hMidiOut, /*channel*/ 0, /*instrument: Guitar*/ 24);

            Console.Write("Start Playing Note\n");
            SendMidiNote(
                hMidiOut,
                /*channel*/ 0,
                /*pitch (Note): Middle C*/ 60,
                /*velocity (Volume)*/ 90);

            Console.Write("Continue Playing for 2 Seconds\n");
            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Console.Write("Stop Playing Note\n");
            SendMidiNote(
                hMidiOut,
                /*channel*/ 0,
                /*pitch*/ 60,
                /*velocity (Volume): Min*/ 0);

            NativeMethods.midiOutClose(hMidiOut);
        }
    }
}
