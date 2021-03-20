// Copyright (c) Kodi Studios 2021.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MidiCSharpConsole
{
    enum MMRESULT : uint
    {
        MMSYSERR_NOERROR = 0,
    }

    static class NativeMethods
    {
        [DllImport("winmm.dll")]
        public static extern MMRESULT midiOutOpen(out IntPtr lphMidiOut, uint uDeviceID, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll")]
        public static extern MMRESULT midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);

        [DllImport("winmm.dll")]
        public static extern MMRESULT midiOutClose(IntPtr hMidiOut);
    }

    class Program
    {
        class MidiException : Exception
        {
            public MidiException(string message) : base(message) { }
        }

        delegate MMRESULT VoidDelegate();

        // Helper Method
        // If Midi Api returns error, throws exception
        static void VerifyMidi(VoidDelegate midiFuncAndParams)
        {
            MMRESULT midiFuncResult = midiFuncAndParams();
            if (midiFuncResult != MMRESULT.MMSYSERR_NOERROR)
            {
                throw new MidiException("Midi Error: " + midiFuncResult);
            }
        }

        // Helper Function
        // If value falls out of limit, throws exception
        static void VerifyLimit(uint currentValue, uint maxValue, string valueName)
        {
            if (currentValue > maxValue)
            {
                throw new IndexOutOfRangeException(valueName + " Current: " + currentValue + " Max: " + maxValue);
            }
        }

        static void SetMidiInstrument(
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

            VerifyLimit(channel, 15, "Channel");
            VerifyLimit(instrument, 127, "Instrument");

            const byte SetInstrumentSignature = 0b1100;
            byte statusByte = SetInstrumentSignature << 4; // 0b 1100 0000
            statusByte |= channel;                         // 0b 1100 CCCC

            byte[] bData = new byte[4];

            bData[0] = statusByte;       // MIDI Status byte
            bData[1] = instrument;       // First MIDI data byte
                                         // Bytes [2] and [3] are unused

            // Midi message is 4 bytes
            // Windows Midi midiOutShortMsg Api passes
            // those 4 bytes as DWORD type.
            uint dwData = BitConverter.ToUInt32(bData, /*startIndex*/ 0);

            VerifyMidi(() => { return NativeMethods.midiOutShortMsg(hMidiOut, dwData); });
        }

        // Plays Midi Note
        // To Stop playing, set velocity parameter to 0
        static void SendMidiNote(
            IntPtr hMidiOut,
            byte channel,  // 4 bits, 0 to 15
            byte pitch,    // 7 bits, 0 to 127
            byte velocity  // 7 bits, 0 to 127
        )
        {
            // Note On Protocol:
            // [0] Status byte     : 0b 1001 CCCC
            //     Note On Signature   : 0b 1001
            //     Channel 4-bits      : 0b CCCC
            // [1] Pitch 7-bits    : 0b 0PPP PPPP
            // [2] Velocity 7-bits : 0b 0VVV VVVV
            // [3] Unused          : 0b 0000 0000
            // Reference: https://www.cs.cmu.edu/~music/cmsip/readings/MIDI%20tutorial%20for%20programmers.html

            // To Turn Note Off, simply pass 0 as Velocity (Volume)
            VerifyLimit(channel, 15, "Channel");
            VerifyLimit(pitch, 127, "Pitch");
            VerifyLimit(velocity, 127, "Velocity");

            const byte NoteOnSignature = 0b1001;
            byte statusByte = NoteOnSignature << 4; // 0b 1001 0000
            statusByte |= channel; // 0b 1001 CCCC

            byte[] bData = new byte[4];
            bData[0] = statusByte;  // MIDI Status byte
            bData[1] = pitch;       // First MIDI data byte
            bData[2] = velocity;    // Second MIDI data byte
                                    // Byte [3] is unused

            // Midi message is 4 bytes
            // Windows Midi midiOutShortMsg Api passes
            // those 4 bytes as DWORD type.
            uint dwData = BitConverter.ToUInt32(bData, /*startIndex*/ 0);

            VerifyMidi(() => { return NativeMethods.midiOutShortMsg(hMidiOut, dwData); });
        }

        static void Main(string[] _)
        {
            IntPtr hMidiOut = new IntPtr();
            VerifyMidi(() =>
            {
                return NativeMethods.midiOutOpen(
                    out hMidiOut,
                    /*uDeviceID*/ 0, // System's Midi device is at index 0
                    /*dwCallback*/ IntPtr.Zero,
                    /*dwInstance*/ IntPtr.Zero,
                    /*fdwOpen*/ 0
                );
            });

            // Set Instruments for Channels 0 and 1
            SetMidiInstrument(hMidiOut, /*channel*/ 0, /*Grand Piano*/ 0);
            SetMidiInstrument(hMidiOut, /*channel*/ 1, /*Guitar*/ 24);

            Console.WriteLine("Play Piano C Note");
            SendMidiNote(hMidiOut, /*channel*/ 0, /*note*/ 60, /*velocity*/ 127);
            Thread.Sleep(TimeSpan.FromSeconds(3));
            SendMidiNote(hMidiOut, /*channel*/ 0, /*note*/ 60, /*velocity*/ 0); // Stop

            Console.WriteLine("Play Guitar C Note");
            SendMidiNote(hMidiOut, /*channel*/ 1, /*note*/ 60, /*velocity*/ 127);
            Thread.Sleep(TimeSpan.FromSeconds(3));
            SendMidiNote(hMidiOut, /*channel*/ 1, /*note*/ 60, /*velocity*/ 0); // Stop

            VerifyMidi(() => { return NativeMethods.midiOutClose(hMidiOut); });
        }
    }
}
